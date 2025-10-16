using Microsoft.EntityFrameworkCore;
using MicrDbChequeProcessingSystem.Data; // your data namespace

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");

builder.Services.AddDbContext<MicrDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddControllersWithViews();

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
