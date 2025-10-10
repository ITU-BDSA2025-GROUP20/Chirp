using SimpleDB;
using System;



var builder = WebApplication.CreateBuilder(args);
// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

var csvFile = Path.Combine(AppContext.BaseDirectory, "chirp_cli_db.csv");
CSVDatabase<Cheep>.Initialize(csvFile);
var database = CSVDatabase<Cheep>.Instance;



app.MapPost("/cheep", (Cheep cheep) =>
{
    database.Store(cheep);
    return Results.Created($"/cheep/{cheep.Timestamp}", cheep);
});

app.MapGet("/cheeps", () =>
{
    try
    {
        var records = database.Read().ToList();
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