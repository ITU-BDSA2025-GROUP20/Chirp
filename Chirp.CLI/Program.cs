using Microsoft.VisualBasic.FileIO;
using System;
using SimpleDB;

    public record Cheep(string Author, string Message, long Timestamp);

    class Program
    {

        static void Main(string[] args)
        {
            var database = new CSVDatabase<Cheep>("chirp_cli_db.csv");

            if (args[0].ToLower() == "read")
            {
            UserInterface.PrintCheeps(database.Read().ToList());
            }
            else if (args[0].ToLower() == "cheep")
            {
                    string line = string.Join(" ", args.Skip(1));

                Cheep cheep = new(Environment.UserName, line, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                Console.WriteLine($"Storing cheep: {cheep.Author} - {cheep.Message}");
                database.Store(cheep);
            }
        }
    }
