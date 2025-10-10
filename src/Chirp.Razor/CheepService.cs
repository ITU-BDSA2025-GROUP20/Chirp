using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;

public record CheepViewModel(string Author, string Message, string Timestamp);

public interface ICheepService
{
    public List<CheepViewModel> GetCheeps();
    public List<CheepViewModel> GetCheepsFromAuthor(string author);
}

public class CheepService : ICheepService
{
    private readonly string _connectionString;

    public CheepService()
    {
        // Database file stored locally
        _connectionString = "Data Source=schema.db";
    }

    public List<CheepViewModel> GetCheeps()
    {
        var cheeps = new List<CheepViewModel>();

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Author, Message, Timestamp FROM Cheeps ORDER BY Id DESC;";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            cheeps.Add(new CheepViewModel(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2)
            ));
        }

        return cheeps;
    }

    public List<CheepViewModel> GetCheepsFromAuthor(string author)
    {
        var cheeps = new List<CheepViewModel>();

        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        var command = connection.CreateCommand();
        command.CommandText = "SELECT Author, Message, Timestamp FROM Cheeps WHERE Author = @Author ORDER BY Id DESC;";
        command.Parameters.AddWithValue("@Author", author);

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            cheeps.Add(new CheepViewModel(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2)
            ));
        }

        return cheeps;
    }
}
