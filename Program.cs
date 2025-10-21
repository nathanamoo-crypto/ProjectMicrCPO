using Microsoft.EntityFrameworkCore;
using MicrDbChequeProcessingSystem.Data; // your data namespace
using MicrDbChequeProcessingSystem.Services;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

var env = builder.Environment;
var config = builder.Configuration;
var defaultSqlServer = config.GetConnectionString("DefaultConnection");
var sqliteFile = Path.Combine(env.ContentRootPath, "micrdb.db");

builder.Services.AddDbContext<MicrDbContext>(options =>
{
    // Prefer SQL Server when a connection string is provided
    if (!string.IsNullOrWhiteSpace(defaultSqlServer))
    {
        options.UseSqlServer(defaultSqlServer);
        return;
    }

    // Development fallback to SQLite when micrdb.db is present
    if (env.IsDevelopment() && File.Exists(sqliteFile))
    {
        options.UseSqlite($"Data Source={sqliteFile}");
        return;
    }

    // Last resort: SQLite in content root
    options.UseSqlite($"Data Source={sqliteFile}");
});

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<ISystemStatusService, SystemStatusService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<MicrDbContext>();

    var provider = dbContext.Database.ProviderName ?? string.Empty;
    if (provider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
    {
        dbContext.Database.EnsureCreated();

        dbContext.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS AccountTypeCustom (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            AccountTypeName TEXT NOT NULL,
            Description TEXT,
            CreatedAt TEXT NOT NULL
        );");

        dbContext.Database.ExecuteSqlRaw(@"CREATE TABLE IF NOT EXISTS RegionCustom (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            RegionName TEXT NOT NULL,
            Description TEXT,
            CreatedAt TEXT NOT NULL
        );");
    }
}

// ✅ 4. Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles(); // Important to serve static assets like CSS, JS, images

app.UseRouting();
app.UseAuthorization();

// ✅ 5. Default route configuration
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
