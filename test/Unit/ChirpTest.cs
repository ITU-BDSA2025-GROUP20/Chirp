namespace ChirpTests;
using Xunit;
using static UserInterface;

public class ChirpTest
{
    [Fact]
    public void TimeTest()
    {
        Cheep cheep = new Cheep("ropf","Hello, BDSA students!",1690891760);

        string vtime = Timestamp(cheep);

        string rtime = "08/01/23 14.09.20";

        Assert.Equal(vtime, rtime);

    }
}