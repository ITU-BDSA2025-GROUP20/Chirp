using System;
using System.Globalization;
using Microsoft.VisualBasic.FileIO;
using CsvHelper;
using CsvHelper.Configuration;
using SimpleDB;

namespace SimpleDB
{



    public sealed class CSVDatabase<T> : IDatabaseRepository<T>
    {
        private static Lazy<CSVDatabase<T>>? Data;

        private readonly string csvPath;
        

        private CSVDatabase(string path)
        {
        
            csvPath = path;
        }

        public static void Initialize(string path)
        {
            if (Data != null)
            {
                throw new InvalidOperationException("CSVDatabase already created");
            }
            Data = new Lazy<CSVDatabase<T>>(() => new CSVDatabase<T>(path));
        }
        public static CSVDatabase<T> Instance
        {
            get
            {
                if (Data == null)
                throw new InvalidOperationException("CSVDatabase not initialized.");
                return Data.Value;
            }
        }


        public IEnumerable<T> Read(int? limit = null)
        {
            if (!File.Exists(csvPath))
            {
                //If the list is empty return empty string
                Console.WriteLine("File not found, check the path");
                return Enumerable.Empty<T>();

            }
            using var reader = new StreamReader(csvPath);

            var config = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true
            };

            using var csv = new CsvHelper.CsvReader(reader, config);


            var records = csv.GetRecords<T>().ToList();
            if (limit.HasValue)
            {
                return records.Take(limit.Value);
            }
            else
            {
                return records;
            }

        }
        public void Store(T record)
        {
            bool fileExists = File.Exists(csvPath);
            using var writer = new StreamWriter(csvPath, true);
            var config = new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)
        {
        HasHeaderRecord = !fileExists
         };

        using var csv = new CsvHelper.CsvWriter(writer, config);

            csv.WriteRecord(record);
            csv.NextRecord();

        }
    }
}
