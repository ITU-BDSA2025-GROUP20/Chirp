namespace UnitTests
{
    using Xunit;
    using System.IO;

    using SimpleDB;
    using System.Linq;
    // Using the global namespace for Program and UserInterface
    using Cheep = global::Cheep;
    using UserInterface = global::UserInterface;
    public class CSVDatabaseTest
    {
        public const string testFile = "test.csv";
        public CSVDatabaseTest()
        {
            CSVDatabase<Cheep>.Initialize(testFile);
        }
    }
    public class UnitTest1
    {
        [Fact]
        public void StoreAndRead()
        {
            const string testFile = "test.csv";
            File.WriteAllText(testFile, "Author,Message,Timestamp\n");

            CSVDatabase<Cheep>.Initialize(testFile);
            var db = CSVDatabase<Cheep>.Instance;
            var entry = new Cheep("testuser", "Hello test user", (long)16084696);


            db.Store(entry);
            var record = db.Read().First();

            Assert.NotNull(record);
            Assert.Equal("testuser", record.Author);
            Assert.Equal("Hello test user", record.Message);
            Assert.Equal(16084696, record.Timestamp);
        }
    }
}