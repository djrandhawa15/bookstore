using Microsoft.EntityFrameworkCore;
using Bookstore.Data;
using Bookstore.Components;
using Bookstore.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// setup Database Context
// Accept either a key-value connection string or a postgresql:// / postgres:// URL
// Render sets DATABASE_URL automatically when a PostgreSQL database is linked
var rawConnectionString = builder.Configuration.GetConnectionString("BOOKSTORE_DB")
    ?? Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? throw new InvalidOperationException("No database connection string found. Set ConnectionStrings__BOOKSTORE_DB or DATABASE_URL.");

var connectionString = ParseConnectionString(rawConnectionString);

builder.Services.AddDbContextFactory<BookstoreDb>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 10,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null)));

builder.Services.AddQuickGridEntityFrameworkAdapter();

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddScoped<OrderState>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var db = services.GetRequiredService<IDbContextFactory<BookstoreDb>>().CreateDbContext();
    db.Database.Migrate();

    SeedData.Initialize(services);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    app.UseMigrationsEndPoint();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

// Converts a postgresql:// or postgres:// URL to Npgsql key-value format.
// Returns the string unchanged if it is already in key-value format.
static string ParseConnectionString(string raw)
{
    if (!raw.StartsWith("postgres://") && !raw.StartsWith("postgresql://"))
        return raw;

    var uri = new Uri(raw);
    var userInfo = uri.UserInfo.Split(':', 2);
    var host = uri.Host;
    var port = uri.IsDefaultPort ? 5432 : uri.Port;
    var database = uri.AbsolutePath.TrimStart('/');
    var username = userInfo.Length > 0 ? userInfo[0] : "";
    var password = userInfo.Length > 1 ? userInfo[1] : "";

    return $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
}
