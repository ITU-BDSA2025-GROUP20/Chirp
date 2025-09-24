namespace UnitTests
{
    using Xunit;
    using SimpleDB;
    using System.Linq;
    // Using the global namespace for Program and UserInterface
    using Cheep = global::Cheep;
    using UserInterface = global::UserInterface;

    public class UnitTest1
    {
        [Fact]
        public void StoreAndRead()
        {
            var db = new CSVDatabase<Cheep>("test.csv");
            var entry = new Cheep("testuser", "Hello test user", (long)16084696);

            db.Store(entry);

            var records = db.Read().ToList();
            var record = records.FirstOrDefault();

            Assert.NotNull(record);
            Assert.Equal(entry.Author, record.Author);
            Assert.Equal(entry.Message, record.Message);
            Assert.Equal(entry.Timestamp, record.Timestamp);
        }
    }
}