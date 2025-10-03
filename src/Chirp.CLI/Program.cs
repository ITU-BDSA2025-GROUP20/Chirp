using CommandLine;
using Microsoft.VisualBasic.FileIO;
using System;
using SimpleDB;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;

public record Cheep(string Author, string Message, long Timestamp);

    public class Options
    {
        [Option('r', "read", Required = false, HelpText = "Read cheeps from the database.")]
        public bool Read { get; set; }

        [Option('c', "cheep", Required = false, HelpText = "Create a new cheep.")]
        public string Cheep { get; set; }
    }

class Program
{
    private static readonly HttpClient client = new HttpClient
    {
        BaseAddress = new Uri("http://bdsagroup20chirpremotedb.azurewebsites.net")
    };
    static async Task Main(string[] args)
    {

        var result = Parser.Default.ParseArguments<Options>(args);
        await result.MapResult(
            async opts =>
            {
                await RunTheShit(opts);
                return 0;
            },
            errs => Task.FromResult(1)
        );
    }

    private static async Task RunTheShit(Options opts)
    {
        if (opts.Read)
        {
            await ReadCheeps();
        }
        else if (opts.Cheep != null)
        {
            await Cheep(opts.Cheep);
        }
        else
        {
            Console.WriteLine("No valid command provided. Use --help for more information.");
        }
    }


    private static async Task ReadCheeps()
    {
            var response = await client.GetAsync("cheeps");
            response.EnsureSuccessStatusCode();
            Console.WriteLine(response);
            var cheeps = await response.Content.ReadFromJsonAsync<List<Cheep>>();
            UserInterface.PrintCheeps(cheeps);
    }

    private static async Task Cheep(string message)
    {
        Cheep cheep = new(Environment.UserName, message, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        var response = await client.PostAsJsonAsync("cheep", cheep);
        response.EnsureSuccessStatusCode();
        Console.WriteLine($"Storing cheep: {cheep.Author} - {cheep.Message}");
    }
}