using Microsoft.EntityFrameworkCore;
using OpenProductivity.Web.Data;
using OpenProductivity.Web.Services;
using System.Diagnostics;
using System.Text;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add EF DbContext
builder.Services.AddDbContext<OpenProjectContext>(options =>
    options.UseSqlite("Data Source=openproject.db"));

// Add WorkPackageETLService
builder.Services.AddScoped<WorkPackageETLService>();
// Replace with your actual values
var username = "apikey";           // usually "apikey" or your username
var accessToken = "70e94dd4c1998dbaa0f918df8c2ab150e2ee71629c8fc3b2284329e2df030731";

builder.Services.AddHttpClient("OpenProjectClient", client =>
{
    client.BaseAddress = new Uri("https://openproject.formulatrix.com/api/v3/");

    // Basic Auth header
    var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{accessToken}"));
    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authValue);
});
var app = builder.Build();

// Reset database before ETL
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OpenProjectContext>();
    Console.WriteLine("🧹 Resetting database...");
    await db.Database.EnsureDeletedAsync();
    await db.Database.EnsureCreatedAsync();

    var etlService = scope.ServiceProvider.GetRequiredService<WorkPackageETLService>();
    string jsonPath = "/home/sofyan/Developments/formulatrix-openproject-miner/src/OpenProjectProductivity.Web/done_work_packages_1000_page_size.json";

    Console.WriteLine("🚀 Starting WorkPackage ETL...");
    var sw = Stopwatch.StartNew();

    try
    {
        await etlService.ImportWorkPackagesAndActivitiesAsync(jsonPath, app.Lifetime.ApplicationStopping);
        sw.Stop();
        Console.WriteLine($"🎉 ETL Complete in {sw.Elapsed.TotalMinutes:F2} minutes!");
    }
    catch (Exception ex)
    {
        sw.Stop();
        Console.WriteLine($"❌ ETL failed after {sw.Elapsed.TotalMinutes:F2} minutes: {ex.Message}");
    }
}

// Configure middleware, routing, etc.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();
