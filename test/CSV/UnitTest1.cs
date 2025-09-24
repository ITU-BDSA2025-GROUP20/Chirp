namespace UnitTests;
using Xunit;
using static UserInterface;

public class UnitTest1
{


    [Fact]
    public void StoreAndRead()
    {
        var entry = new Cheep("testuser", "Hello test user", 16084696);
        
    
        db.Store(entry);

        var records = db.Read().ToList();
        var record = records.FirstOrDefault();

        Assert.NotNull(record);
        Assert.Equal(entry.Username, record.Username);
        Assert.Equal(entry.Usermessage, record.Usermessage);
        Assert.Equal(entry.Timestamp, record.Timestamp);
    }
}