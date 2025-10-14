using Microsoft.EntityFrameworkCore;
using OpenProductivity.Web.Data;
using OpenProductivity.Web.Interfaces;
using OpenProductivity.Web.Services;
using OpenProjectProductivity.Web.Interfaces;
using OpenProjectProductivity.Web.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add EF DbContext
builder.Services.AddDbContext<OpenProjectContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("OpenProjectDb")));

// Add statistics service
builder.Services.AddScoped<IProductivityStatisticService, ProductivityStatisticService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
// Optional: still keep HttpClient for future API integrations
builder.Services.AddHttpClient("OpenProjectClient", client =>
{
    client.BaseAddress = new Uri("https://openproject.formulatrix.com/api/v3/");

    // Basic Auth header (can be moved to appsettings.json later)
    var username = "apikey";
    var accessToken = "70e94dd4c1998dbaa0f918df8c2ab150e2ee71629c8fc3b2284329e2df030731";
    var authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{accessToken}"));
    client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authValue);
});
builder.Services.AddHttpClient("OpenProjectApi", client =>
{
    client.BaseAddress = new Uri("http://localhost:5066/"); // your API base url
});
var app = builder.Build();

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
    name: "productivity",
    pattern: "productivity/{action=Index}/{projectId?}",
    defaults: new { controller = "Productivity" });

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
