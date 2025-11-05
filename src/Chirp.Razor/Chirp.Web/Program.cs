using System;
using Chirp.Core;
using Chirp.Infrastructure.Services;
using Chirp.Infrastructure.Data;
using Chirp.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();
builder.Services.AddScoped<CheepService>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<ICheepRepository, CheepRepository>();

var passwordBuilder = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables();
var configuration = passwordBuilder.Build();

string? connectionString = configuration.GetConnectionString("DefaultConnection")
    .Replace("{DB_PASSWORD}", Environment.GetEnvironmentVariable("DB_PASSWORD"));

    Console.WriteLine(connectionString);

builder.Services.AddDbContext<CheepDbContext>(options =>
    options.UseSqlServer(connectionString));

// Add services to the container.
builder.Services.AddRazorPages();


var app = builder.Build();
using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<CheepDbContext>();
try
{
    await DbInitializer.SeedDatabaseAsync(db);
    Console.WriteLine("Database connection successful.");
}
catch (Exception ex)
{
    Console.WriteLine("Database connection failed: " + ex.Message);
    Console.WriteLine(ex.StackTrace);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.Run();