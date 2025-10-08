using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using CsvHelper;
using CsvHelper.Configuration;

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
                throw new InvalidOperationException("CSVDatabase already created");

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
            // If file does not exist or is empty, return empty list
            if (!File.Exists(csvPath) || new FileInfo(csvPath).Length == 0)
                return Enumerable.Empty<T>();

            try
            {
                using var reader = new StreamReader(csvPath);
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = false, // no headers required
                    MissingFieldFound = null,
                    BadDataFound = null
                };

                using var csv = new CsvReader(reader, config);
                var records = csv.GetRecords<T>().ToList();

                return limit.HasValue ? records.Take(limit.Value) : records;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading CSV: {ex.Message}");
                return Enumerable.Empty<T>();
            }
        }

        public void Store(T record)
        {
            try
            {
                bool fileExists = File.Exists(csvPath);

                using var writer = new StreamWriter(csvPath, append: true);
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = false // always write data only
                };
                using var csv = new CsvWriter(writer, config);

                csv.WriteRecord(record);
                csv.NextRecord();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing CSV: {ex.Message}");
            }
        }
    }
}
