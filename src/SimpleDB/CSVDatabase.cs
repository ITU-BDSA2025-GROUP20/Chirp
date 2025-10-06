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
            // Ensure CSV exists with header
            if (!File.Exists(csvPath))
            {
                using var writer = new StreamWriter(csvPath);
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true
                };
                using var csv = new CsvWriter(writer, config);
                csv.WriteHeader<T>();
                csv.NextRecord();
                return Enumerable.Empty<T>();
            }

            using var reader = new StreamReader(csvPath);
            var configReader = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true
            };
            using var csvReader = new CsvReader(reader, configReader);

            var records = csvReader.GetRecords<T>().ToList();
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
            using var writer = new StreamWriter(csvPath, append: true);
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = !fileExists // write header if file didn't exist
            };
            using var csv = new CsvWriter(writer, config);

            if (!fileExists)
            {
                csv.WriteHeader<T>();
                csv.NextRecord();
            }

            csv.WriteRecord(record);
            csv.NextRecord();
        }
    }
}
