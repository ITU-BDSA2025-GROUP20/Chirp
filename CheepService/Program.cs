using SimpleDB;


var builder = WebApplication.CreateBuilder(args);
// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

CSVDatabase<Cheep>.Initialize("chirp_cli_db.csv");
var database = CSVDatabase<Cheep>.Instance;


app.MapPost("/cheep", (Cheep cheep) =>
{
    database.Store(cheep);
    return Results.Created($"/cheep/{cheep.Timestamp}", cheep);
});

app.MapGet("/cheeps", () =>
{
    var records = database.Read().ToList();
    return Results.Ok(records);
});


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Run();