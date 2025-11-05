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