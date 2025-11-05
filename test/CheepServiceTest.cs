using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Chirp.Infrastructure.Services;
using Chirp.Core;
using Chirp.Infrastructure.Models;

public class CheepServiceTests
{
    [Fact]
    public async Task GetCheeps_ShouldReturnMappedViewModels()
    {
        var mockRepo = new Mock<ICheepRepository>();
        mockRepo.Setup(r => r.GetAllCheepsAsync()).ReturnsAsync(new List<Cheep>
        {
            new Cheep { Author = new Author { Name = "Alice" }, Text = "Hello", TimeStamp = new DateTime(2024, 1, 1) }
        });

        var service = new CheepService(mockRepo.Object);
        var result = await service.GetCheeps();

        Assert.Single(result);
        Assert.Equal("Alice", result[0].AuthorName);
        Assert.Equal("Hello", result[0].Text);
        Assert.Equal("01/01/24 0.00.00", result[0].TimeStamp);
    }

    [Fact]
    public async Task GetCheepsFromAuthor_ShouldCallRepositoryAndMapResults()
    {
        var mockRepo = new Mock<ICheepRepository>();
        mockRepo.Setup(r => r.GetAllCheepsFromAuthorAsync("Bob")).ReturnsAsync(new List<Cheep>
        {
            new Cheep { Author = new Author { Name = "Bob" }, Text = "Hey there", TimeStamp = DateTime.UtcNow }
        });

        var service = new CheepService(mockRepo.Object);
        var result = await service.GetCheepsFromAuthor("Bob");

        Assert.Single(result);
        Assert.Equal("Bob", result[0].AuthorName);
        Assert.Equal("Hey there", result[0].Text);
    }
}
