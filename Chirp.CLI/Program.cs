
using Microsoft.VisualBasic.FileIO;
using System;


    var path = "chirp_cli_db.csv";
    List<string> Authors = new List<string>();
    List<string> Messages = new List<string>();
    List<string> Timestamps = new List<string>();
    List<Cheep> cheeps = new List<Cheep>();

    
    
    for (int i = 0; i < Authors.Count; i++)
    {
        Cheep cheep = new(Authors[i], Messages[i], long.Parse(Timestamps[i]));
        cheeps.Add(cheep);
    }
    
    

    if (args[0].ToLower() == "read")
    {


        foreach(Cheep cheep in cheeps) {
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(Convert.ToDouble(cheep.TimeStamp)).ToLocalTime();
            string[] switchedDayMonth = dateTime.ToString().Split('/');
            switchedDayMonth[2] = switchedDayMonth[2].TrimStart('2');
            switchedDayMonth[2] = switchedDayMonth[2].TrimStart('0');
            var time = switchedDayMonth[1] + "/" + switchedDayMonth[0] + "/" + switchedDayMonth[2];
            Console.WriteLine(cheep.Author + " @ " + time + ": " + cheep.Message);
        }
    }
    else if (args[0].ToLower() == "cheep")
    {
        string csv = "";
        string line = "";
        for (int i = 1; i < args.Length; i++)
        {
            line += args[i];
        }
        Cheep cheep = new(Environment.UserName, line, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        csv += cheep.Author + ",\"" + cheep.Message + "\"," + cheep.TimeStamp + Environment.NewLine;
        File.AppendAllText(path, csv);
    }


public record Cheep(string Author, string Message, long TimeStamp);