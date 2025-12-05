using System;
using Core;
using Infrastructure.Services;
using Infrastructure.Data;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;


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

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false).AddEntityFrameworkStores<CheepDbContext>();

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

builder.Services.AddAuthentication()
    .AddGitHub(o =>
    {
        o.ClientId = builder.Configuration["authentication:github:clientId"];
        o.ClientSecret = builder.Configuration["authentication:github:clientSecret"];
        o.CallbackPath = "/signin-github";
        o.AuthorizationEndpoint += "?prompt=login"; 
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

var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
var helgePassword = config["SeedUsers:HelgePassword"];
var adrianPassword = config["SeedUsers:AdrianPassword"];

try
{
    await db.Database.MigrateAsync();
    await DbInitializer.SeedDatabaseAsync(db);
    Console.WriteLine("Database connection successful.");

    // Set passwords for Helge
    var helge = await userManager.FindByEmailAsync("ropf@itu.dk");
    if (helge == null)
    {
        helge = new IdentityUser
        {
        UserName = "ropf@itu.dk",
        Email = "ropf@itu.dk",
        EmailConfirmed = true
        };
        await userManager.CreateAsync(helge, helgePassword);
    }

    var adrian = await userManager.FindByEmailAsync("adho@itu.dk");
    if (adrian == null)
    {
        adrian = new IdentityUser
        {
        UserName = "adho@itu.dk",
        Email = "adho@itu.dk",
        EmailConfirmed = true
        };
        await userManager.CreateAsync(adrian, adrianPassword);
    }
}
catch (Exception ex)
{
    Console.WriteLine("Database setup failed: " + ex.Message);
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
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();
