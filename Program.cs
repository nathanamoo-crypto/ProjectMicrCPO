using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.Win32;
using MicrDbChequeProcessingSystem.Data; // your data namespace
using MicrDbChequeProcessingSystem.Services;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

var env = builder.Environment;
var config = builder.Configuration;
var defaultSqlServer = config.GetConnectionString("DefaultConnection");
var sqliteFile = Path.Combine(env.ContentRootPath, "micrdb.db");

// Determine final SQL Server connection string (try named instance -> port)
string? finalSqlServer = null;
if (!string.IsNullOrWhiteSpace(defaultSqlServer))
{
    var csb = new SqlConnectionStringBuilder(defaultSqlServer);
    if (!csb.ContainsKey("Connect Timeout")) csb["Connect Timeout"] = 3;

    // If using a named instance (e.g., KB\\SQLEXPRESS), try resolving port to avoid SQLBrowser dependency
    if (csb.DataSource.Contains("\\"))
    {
        var parts = csb.DataSource.Split('\\', 2);
        var host = string.IsNullOrWhiteSpace(parts[0]) ? "localhost" : parts[0];
        var instance = parts.Length > 1 ? parts[1] : "";

        if (!string.IsNullOrWhiteSpace(instance))
        {
            var port = TryGetSqlServerPortForInstance(instance);
            if (!string.IsNullOrWhiteSpace(port))
            {
                csb.DataSource = host + "," + port;
            }
        }
    }

    finalSqlServer = csb.ConnectionString;
}

builder.Services.AddDbContext<MicrDbContext>(options =>
{
    if (!string.IsNullOrWhiteSpace(finalSqlServer))
    {
        options.UseSqlServer(finalSqlServer, sql => sql.EnableRetryOnFailure());
        return;
    }

    // If no SQL Server configured, use SQLite
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
    }
    else if (provider.Contains("SqlServer", StringComparison.OrdinalIgnoreCase))
    {
        // Do NOT EnsureCreated on SQL Server to avoid conflicts with existing schemas
        // Just verify connectivity; schema should be managed externally or via migrations
        try { dbContext.Database.CanConnect(); } catch { /* handled by middleware later */ }
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

// Utility: best-effort read of TCP port for named SQL Server instance (local machine)
static string? TryGetSqlServerPortForInstance(string instanceName)
{
    try
    {
        // From: HKLM\SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL
        using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var namesKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL");
        var instanceId = namesKey?.GetValue(instanceName) as string;
        if (string.IsNullOrWhiteSpace(instanceId)) return null;

        var ipAllPath = $"SOFTWARE\\Microsoft\\Microsoft SQL Server\\{instanceId}\\MSSQLServer\\SuperSocketNetLib\\Tcp\\IpAll";
        using var ipAllKey = baseKey.OpenSubKey(ipAllPath);
        var explicitPort = ipAllKey?.GetValue("TcpPort") as string;
        var dynamicPort = ipAllKey?.GetValue("TcpDynamicPorts") as string;

        // Prefer explicit port, else dynamic port
        var port = !string.IsNullOrWhiteSpace(explicitPort) ? explicitPort : dynamicPort;
        if (string.IsNullOrWhiteSpace(port)) return null;
        return port.Split(';').FirstOrDefault(p => !string.IsNullOrWhiteSpace(p));
    }
    catch { return null; }
}
