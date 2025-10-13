using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenProductivity.Web.Data;
using OpenProductivity.Web.Models;

namespace OpenProductivity.Web.Services;

public class WorkPackageETLService
{
    private readonly OpenProjectContext _context;
    private readonly HttpClient _httpClient;
    private readonly int _batchSize = 500;
    private readonly int _maxConcurrentRequests = 5;
    private readonly Dictionary<int, int> _userCache = new(); // API UserId -> DB UserId

    public WorkPackageETLService(OpenProjectContext context, IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _httpClient = httpClientFactory.CreateClient("OpenProjectClient");
    }

    public async Task ImportWorkPackagesAndActivitiesAsync(string jsonFilePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(jsonFilePath))
            throw new FileNotFoundException("JSON file not found", jsonFilePath);

        Console.WriteLine($"📂 Starting ETL from {jsonFilePath}...");

        // Load existing Projects and Users
        var existingProjectIds = await _context.Projects.AsNoTracking()
            .Select(p => p.Id).ToHashSetAsync(cancellationToken);

        var existingUsers = await _context.Users.AsNoTracking()
            .ToDictionaryAsync(u => u.Id, u => u.Name, cancellationToken);

        foreach (var kv in existingUsers) _userCache[kv.Key] = kv.Key;

        var missingProjects = new Dictionary<int, string>();
        var missingUsers = new Dictionary<int, string>();
        var workPackagesBuffer = new List<(int Id, WorkPackage Wp)>();

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, AllowTrailingCommas = true };
        await using var fs = new FileStream(jsonFilePath, FileMode.Open, FileAccess.Read);

        // --- Scan JSON for WorkPackages, Projects, Users ---
        await foreach (var wpJson in JsonSerializer.DeserializeAsyncEnumerable<JsonElement>(fs, options, cancellationToken))
        {
            if (wpJson.ValueKind != JsonValueKind.Object) continue;

            // Extract Project
            var (projectId, projectTitle) = ExtractProject(wpJson);
            if (!existingProjectIds.Contains(projectId) && !missingProjects.ContainsKey(projectId))
                missingProjects[projectId] = projectTitle;

            // Extract Assignee
            var (assigneeId, assigneeName) = ExtractAssignee(wpJson);
            if (assigneeId.HasValue && !_userCache.ContainsKey(assigneeId.Value))
            {
                missingUsers[assigneeId.Value] = assigneeName ?? $"User {assigneeId.Value}";
                _userCache[assigneeId.Value] = 0; // placeholder
            }

            // Map WorkPackage
            var wp = MapJsonToWorkPackage(wpJson, projectId, assigneeId);
            int wpId = wpJson.GetProperty("id").GetInt32();
            workPackagesBuffer.Add((wpId, wp));
        }

        // --- Insert missing Projects ---
        if (missingProjects.Any())
        {
            var projectsToAdd = missingProjects.Select(kv => new Project { Id = kv.Key, Name = kv.Value });
            await _context.Projects.AddRangeAsync(projectsToAdd, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            foreach (var kv in missingProjects) existingProjectIds.Add(kv.Key);
        }

        // --- Insert missing Users ---
        foreach (var kv in missingUsers)
        {
            var user = new User { Id = kv.Key, Name = kv.Value };
            _context.Users.Add(user);
            _userCache[kv.Key] = kv.Key;
        }
        if (missingUsers.Any()) await _context.SaveChangesAsync(cancellationToken);

        // --- Upsert WorkPackages ---
        var idToWpMap = new Dictionary<int, WorkPackage>();
        for (int i = 0; i < workPackagesBuffer.Count; i += _batchSize)
        {
            var batch = workPackagesBuffer.Skip(i).Take(_batchSize).ToList();
            var ids = batch.Select(b => b.Id).ToList();

            var existingBatch = await _context.WorkPackages
                .Where(wp => ids.Contains(wp.Id))
                .ToDictionaryAsync(wp => wp.Id, cancellationToken);

            foreach (var (id, wp) in batch)
            {
                if (existingBatch.TryGetValue(id, out var existingWp))
                {
                    UpdateWorkPackage(existingWp, wp);
                    idToWpMap[id] = existingWp;
                }
                else
                {
                    wp.Id = id;
                    _context.WorkPackages.Add(wp);
                    idToWpMap[id] = wp;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            _context.ChangeTracker.Clear();
        }

        // --- Fetch Activities in parallel (only status changes) ---
        var allActivities = new List<Activity>();
        using var semaphore = new SemaphoreSlim(_maxConcurrentRequests);

        var tasks = idToWpMap.Keys.Select(async wpId =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                var activities = await FetchStatusActivitiesAsync(wpId, cancellationToken);
                lock (allActivities) allActivities.AddRange(activities);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        // --- Insert Activities in batches ---
        for (int i = 0; i < allActivities.Count; i += _batchSize)
        {
            var batch = allActivities.Skip(i).Take(_batchSize).ToList();
            await _context.Activities.AddRangeAsync(batch, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            _context.ChangeTracker.Clear();
        }

        Console.WriteLine("🎉 ETL Finished!");
    }

    // --- Only update relevant WorkPackage fields ---
    private void UpdateWorkPackage(WorkPackage existing, WorkPackage incoming)
    {
        existing.ProjectId = incoming.ProjectId;
        existing.AssigneeId = incoming.AssigneeId;
        existing.Subject = incoming.Subject;
        existing.Description = incoming.Description;
        existing.Type = incoming.Type;
        existing.Status = incoming.Status;
        existing.StartDate = incoming.StartDate;
        existing.DueDate = incoming.DueDate;
        existing.CreatedAt = incoming.CreatedAt;
        existing.UpdatedAt = incoming.UpdatedAt;
        existing.PercentageDone = incoming.PercentageDone;
        existing.GoalPeriod = incoming.GoalPeriod;
    }

    // --- Fetch only status-change activities ---
    private async Task<List<Activity>> FetchStatusActivitiesAsync(int workPackageId, CancellationToken cancellationToken)
    {
        var activities = new List<Activity>();
        var response = await _httpClient.GetAsync($"work_packages/{workPackageId}/activities", cancellationToken);
        if (!response.IsSuccessStatusCode) return activities;

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);

        if (!doc.RootElement.TryGetProperty("_embedded", out var embedded) ||
            !embedded.TryGetProperty("elements", out var activityElements) ||
            activityElements.ValueKind != JsonValueKind.Array)
            return activities;

        foreach (var elem in activityElements.EnumerateArray())
        {
            if (!elem.TryGetProperty("details", out var details) || details.ValueKind != JsonValueKind.Array)
                continue;

            string timestampStr = elem.TryGetProperty("createdAt", out var tsProp) ? tsProp.GetString() : null;
            DateTime timestamp = DateTime.TryParse(timestampStr, out var dt) ? dt : DateTime.Now;

            int? userId = null;
            if (elem.TryGetProperty("_links", out var links) &&
                links.TryGetProperty("user", out var userLink) &&
                int.TryParse(userLink.GetProperty("href").GetString()?.Split('/').Last(), out var uid))
            {
                if (_userCache.ContainsKey(uid)) userId = uid;
            }

            foreach (var detail in details.EnumerateArray())
            {
                string raw = detail.TryGetProperty("raw", out var rawProp) ? rawProp.GetString() ?? "" : "";

                // Only store status changes
                if (!raw.StartsWith("Status changed")) continue;

                var activity = new Activity
                {
                    WorkPackageId = workPackageId,
                    Timestamp = timestamp,
                    UserId = userId
                };

                var parts = raw.Replace("Status changed from ", "").Split(" to ");
                if (parts.Length == 2)
                {
                    activity.FromStatus = parts[0].Trim();
                    activity.ToStatus = parts[1].Trim();
                }

                activities.Add(activity);
            }
        }

        return activities;
    }

    // --- JSON Helpers ---
    private (int Id, string Title) ExtractProject(JsonElement wpJson)
    {
        if (wpJson.TryGetProperty("_links", out var links) &&
            links.TryGetProperty("project", out var project) &&
            project.TryGetProperty("href", out var projectHref) &&
            project.TryGetProperty("title", out var projectTitle) &&
            int.TryParse(projectHref.GetString()?.Split('/').Last(), out var projectId))
        {
            return (projectId, projectTitle.GetString() ?? $"Project {projectId}");
        }
        return (0, "Unknown Project");
    }

    private (int? Id, string? Name) ExtractAssignee(JsonElement wpJson)
    {
        if (wpJson.TryGetProperty("_links", out var links) &&
            links.TryGetProperty("assignee", out var assignee) &&
            assignee.TryGetProperty("href", out var assigneeHref) &&
            assignee.TryGetProperty("title", out var assigneeName) &&
            int.TryParse(assigneeHref.GetString()?.Split('/').Last(), out var assigneeId))
        {
            return (assigneeId, assigneeName.GetString());
        }
        return (null, null);
    }

    private string? ExtractGoalPeriod(JsonElement wpJson)
    {
        if (wpJson.TryGetProperty("_links", out var links) &&
            links.TryGetProperty("customField26", out var goalField) &&
            goalField.ValueKind == JsonValueKind.Array &&
            goalField.GetArrayLength() > 0 &&
            goalField[0].TryGetProperty("title", out var titleProp) &&
            titleProp.ValueKind == JsonValueKind.String)
        {
            return titleProp.GetString();
        }
        return null;
    }

    private WorkPackage MapJsonToWorkPackage(JsonElement wpJson, int projectId, int? assigneeId)
    {
        string subject = wpJson.TryGetProperty("subject", out var sub) ? sub.GetString() ?? "" : "";
        string description = wpJson.TryGetProperty("description", out var desc) &&
                             desc.TryGetProperty("raw", out var raw) ? raw.GetString() ?? "" : "";
        int percentageDone = wpJson.TryGetProperty("percentageDone", out var pct) && pct.ValueKind == JsonValueKind.Number
                             ? pct.GetInt32() : 0;

        DateTime createdAt = wpJson.TryGetProperty("createdAt", out var cProp) && cProp.ValueKind == JsonValueKind.String
                             ? cProp.GetDateTime() : DateTime.MinValue;
        DateTime updatedAt = wpJson.TryGetProperty("updatedAt", out var uProp) && uProp.ValueKind == JsonValueKind.String
                             ? uProp.GetDateTime() : DateTime.MinValue;

        string type = wpJson.TryGetProperty("_links", out var l) &&
                      l.TryGetProperty("type", out var t) &&
                      t.TryGetProperty("title", out var tTitle) ? tTitle.GetString() ?? "" : "";

        string status = wpJson.TryGetProperty("_links", out var l2) &&
                        l2.TryGetProperty("status", out var s) &&
                        s.TryGetProperty("title", out var sTitle) ? sTitle.GetString() ?? "" : "";

        DateTime? startDate = wpJson.TryGetProperty("startDate", out var sProp) && sProp.ValueKind == JsonValueKind.String
                              ? sProp.GetDateTime() : null;

        DateTime? dueDate = wpJson.TryGetProperty("dueDate", out var dProp) && dProp.ValueKind == JsonValueKind.String
                            ? dProp.GetDateTime() : null;

        return new WorkPackage
        {
            ProjectId = projectId,
            AssigneeId = assigneeId,
            Subject = subject,
            Description = description,
            Type = type,
            Status = status,
            StartDate = startDate,
            DueDate = dueDate,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            PercentageDone = percentageDone,
            GoalPeriod = ExtractGoalPeriod(wpJson)
        };
    }
}
