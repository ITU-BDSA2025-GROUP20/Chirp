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
                List<Cheep> cheeps = database.Read().ToList();



                foreach (Cheep cheep in cheeps)
                {
                    DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                    dateTime = dateTime.AddSeconds(Convert.ToDouble(cheep.Timestamp)).ToLocalTime();
                    string[] switchedDayMonth = dateTime.ToString().Split('/');
                    switchedDayMonth[2] = switchedDayMonth[2].TrimStart('2');
                    switchedDayMonth[2] = switchedDayMonth[2].TrimStart('0');
                    var time = switchedDayMonth[1] + "/" + switchedDayMonth[0] + "/" + switchedDayMonth[2];
                    Console.WriteLine(cheep.Author + " @ " + time + ": " + cheep.Message);
                }
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
