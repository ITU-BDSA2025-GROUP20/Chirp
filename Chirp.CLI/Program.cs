using CommandLine;
using Microsoft.VisualBasic.FileIO;
using System;
using SimpleDB;

public record Cheep(string Author, string Message, long Timestamp);

class Program
{
    public class Options
    {
        [Option('r', "read", Required = false, HelpText = "Read cheeps from the database.")]
        public bool Read { get; set; }

        [Option('c', "cheep", Required = false, HelpText = "Create a new cheep.")]
        public string Cheep { get; set; }
    }

    static void Main(string[] args)
    {
        Parser.Default.ParseArguments<Options>(args)
            .WithParsed<Options>(opts =>
            {
                var database = new CSVDatabase<Cheep>("chirp_cli_db.csv");

                if (opts.Read)
                {
                    UserInterface.PrintCheeps(database.Read().ToList());
                }
                else if (opts.Cheep != null)
                {
                    Cheep cheep = new(Environment.UserName, opts.Cheep, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                    Console.WriteLine($"Storing cheep: {cheep.Author} - {cheep.Message}");
                    database.Store(cheep);
                }
                else
                {
                    Console.WriteLine("No valid command provided. Use --help for more information.");
                }
            });
    }
}