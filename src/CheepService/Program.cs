using CheepService.Models;
using CheepService.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddScoped<ICheepRepository, CheepRepository>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<CheepDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<CheepDbContext>();
    context.Database.EnsureCreated();
    SeedDatabase.Initialize(context);
}

app.MapPost("/cheep", async (Cheep cheep, CheepDbContext context) =>
{
    cheep.Timestamp = DateTime.UtcNow;
    context.Cheeps.Add(cheep);
    await context.SaveChangesAsync();
    return Results.Created($"/cheep/{cheep.Id}", cheep);
});

app.MapGet("/cheeps", (CheepDbContext context) =>
{
    try
    {
        var records = context.Cheeps
            .OrderByDescending(c => c.Timestamp)
            .ToList()
            .Select(c => new 
            {
                c.Author, 
                c.Message, 
                Timestamp = ((DateTimeOffset)c.Timestamp.ToUniversalTime()).ToUnixTimeSeconds()
            })
            .ToList();
            
        return Results.Ok(records);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error reading cheeps: {ex}");
        return Results.Problem(detail: ex.ToString());
    }
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Run();