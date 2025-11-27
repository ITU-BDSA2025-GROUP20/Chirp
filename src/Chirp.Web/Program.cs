using System;
using Core;
using Infrastructure.Services;
using Infrastructure.Data;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);
//var connectionStringdb = builder.Configuration.GetConnectionString("CheepDbContextConnection") ?? throw new InvalidOperationException("Connection string 'CheepDbContextConnection' not found.");

// Add services to the container.
builder.Services.AddRazorPages();

builder.Services.AddScoped<CheepService>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<ICheepRepository, CheepRepository>();

var passwordBuilder = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables();
var configuration = passwordBuilder.Build();

string? connectionString = configuration.GetConnectionString("DefaultConnection");

Console.WriteLine(connectionString);


builder.Services.AddDbContext<CheepDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true).AddEntityFrameworkStores<CheepDbContext>();

builder.Services.Configure<IdentityOptions>(options =>
{
    // Password settings.
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;

    // Lockout settings.
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings.
    options.User.AllowedUserNameCharacters =
    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = false;
});

builder.Services.ConfigureApplicationCookie(options =>
{
    // Cookie settings
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(5);

    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.SlidingExpiration = true;
});

var app = builder.Build();

// Seed database
using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<CheepDbContext>();
try
{
    await db.Database.EnsureCreatedAsync();
    await DbInitializer.SeedDatabaseAsync(db);
    Console.WriteLine("Database connection successful.");
}
catch (Exception ex)
{
    Console.WriteLine("Database connection failed: " + ex.Message);
    Console.WriteLine(ex.StackTrace);
}

// Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapRazorPages();

app.MapGet("/test-db", async (CheepDbContext db) =>
{
    try
    {
        // Try to read 5 authors from the database
        var authors = await db.Authors.Take(5).ToListAsync();
        return Results.Ok(new { success = true, count = authors.Count, authors });
    }
    catch (Exception ex)
    {
        // Return the exception message if something fails
        return Results.Problem(ex.Message);
    }
});
app.Run();
