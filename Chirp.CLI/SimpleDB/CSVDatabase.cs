using System;
using Microsoft.VisualBasic.FileIO;
using CsvHelper;
using IDatabaseRepository;
using SimpleDB;

namespace SimpleDB
{
}

public class CSVDatabase<T> : IDatabaseRepository<T>
{
        var path = "chirp_cli_db";

    private readonly string csvPath;

    public CSVDatabase(string path)
    {
        csvPath = path;
    }

    public void Save(Cheep cheep)
    {
        using var writer = new StreamWriter(csvPath, true);
        using var csv = new CsvHelper.CsvWriter(writer, CultureInfo.InvariantCulture);

        csv.WriteRecord(cheep);
        csv.NextRecord();
    }

    public List<Cheep> Load()
    {
        if (!FileExists(csvPath))
        {
            //If the list is empty return empty string
            Console.WriteLine("File not found, check the path");
                return new List<Cheep>();

        }
        using var reader = StreamReader(csvPath);

        var confiq = new CsvHelper.CsvReader(reader, new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture))
        {
            HasHeaderRecord = false
        };
        
        using var csv = new CsvHelper.CsvReader(reader, config);


        var cheeps = csv.GetRecords<T>().ToList();
        return cheeps;
    }

}

