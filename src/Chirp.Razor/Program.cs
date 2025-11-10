using System;
using Chirp.Infrastructure.Services;
using Chirp.Infrastructure.Data;
using Chirp.Core;
using Chirp.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages(options =>
{
    options.RootDirectory = "/Chirp.Web/Pages";
});

builder.Services.AddScoped<CheepService>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<ICheepRepository, CheepRepository>();

string? connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<CheepDbContext>(options => options.UseSqlite(connectionString));

var app = builder.Build();

// Seed database
using var scope = app.Services.CreateScope();
var db = scope.ServiceProvider.GetRequiredService<CheepDbContext>();
await DbInitializer.SeedDatabaseAsync(db);

var service = scope.ServiceProvider.GetRequiredService<CheepService>();
await service.TestSeedAsync();

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

app.MapRazorPages();  // ← this is correct

app.Run();