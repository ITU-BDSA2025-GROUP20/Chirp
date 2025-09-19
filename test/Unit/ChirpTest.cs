using Xunit;
using Userinterface;
namespace Chirp.Tests;


public class ChirpTest
{
    [Fact]
    public void TimeTest()
    {
         Cheep cheep = new Cheep(ropf,"Hello, BDSA students!",1690891760);

        var vtime = TimeStamp(cheep);

        var rtime = "14.09.20";

            Assert.Equals(vtime = rtime);

    }
}