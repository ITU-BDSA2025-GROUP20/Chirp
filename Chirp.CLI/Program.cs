
using Microsoft.VisualBasic.FileIO;
using System;
var path = "chirp_cli_db.csv";
List<string> Authors = new List<string>();
List<string> Messages = new List<string>();
List<string> Timestamps = new List<string>();

using (TextFieldParser csvParser = new TextFieldParser(path))
{
    csvParser.CommentTokens = new string[] { "#" };
    csvParser.SetDelimiters(new string[] { "," });
    csvParser.HasFieldsEnclosedInQuotes = true;

    // Skip the row with the column names
    csvParser.ReadLine();
    while (!csvParser.EndOfData)
    {
        // Read current line fields, pointer moves to the next line.
        string[] fields = csvParser.ReadFields();
        Authors.Add(fields[0]);
        Messages.Add(fields[1]);
        Timestamps.Add(fields[2]);
    }
}

if (args[0].ToLower() == "read")
{


    for (int i = 0; i < Authors.Count; i++)
    {
        DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        dateTime = dateTime.AddSeconds(Convert.ToDouble(Timestamps[i])).ToLocalTime();
        string[] switchedDayMonth = dateTime.ToString().Split('/');
        switchedDayMonth[2] = switchedDayMonth[2].TrimStart('2');
        switchedDayMonth[2] = switchedDayMonth[2].TrimStart('0');
        Console.WriteLine("" + Authors[i] + " @ " + switchedDayMonth[1] + "/" + switchedDayMonth[0] + "/" + switchedDayMonth[2] + ": " + Messages[i]);
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
    
    csv += Environment.UserName + ",\"" + line + "\"," + DateTimeOffset.UtcNow.ToUnixTimeSeconds() + Environment.NewLine;
    File.AppendAllText(path, csv);
}