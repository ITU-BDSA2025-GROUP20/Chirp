using System;
using Microsoft.VisualBasic.FileIO;
using CSVHelper;
using IDatabaseRepository;
using SimpleDB;
using TextFieldParser;

namespace SimpleDB
{


class CSVDatabase{
        var path = "chirp_cli_db.csv";
        TextFieldParser csvParser = new TextFieldParser(path);
        csvParser.CommentTokens

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

}
}