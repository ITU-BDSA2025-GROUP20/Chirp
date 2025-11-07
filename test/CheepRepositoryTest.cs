using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;
using Chirp.Infrastructure;
using Chirp.Infrastructure.Data;
using Chirp.Infrastructure.Models;

public class CheepRepositoryTests
{
    private CheepDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<CheepDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new CheepDbContext(options);
    }

    [Fact]
    public async Task StoreCheepAsync_ShouldAddCheepToDatabase()
    {
        var db = GetInMemoryDbContext();
        var repo = new CheepRepository(db);

        var author = new Author { AuthorId = 1, Name = "Alice" };
        db.Authors.Add(author);
        await db.SaveChangesAsync();

        var cheep = new Cheep { Text = "Hello World", TimeStamp = DateTime.UtcNow, Author = author };
        await repo.StoreCheepAsync(cheep);

        Assert.Single(db.Cheeps);
        Assert.Equal("Hello World", db.Cheeps.First().Text);
    }

    [Fact]
    public async Task GetAllCheepsAsync_ShouldReturnCheepsOrderedByTimestampDesc()
    {
        var db = GetInMemoryDbContext();
        var repo = new CheepRepository(db);

        var author = new Author { Name = "Bob" };
        db.Authors.Add(author);
        db.Cheeps.AddRange(
            new Cheep { Text = "Old", TimeStamp = DateTime.UtcNow.AddHours(-2), Author = author },
            new Cheep { Text = "New", TimeStamp = DateTime.UtcNow, Author = author }
        );
        await db.SaveChangesAsync();

        var result = await repo.GetAllCheepsAsync();

        Assert.Equal(2, result.Count());
        Assert.Equal("New", result.First().Text); // most recent first
    }

    [Fact]
    public async Task GetCheepByIdAsync_ShouldReturnCorrectCheep()
    {
        var db = GetInMemoryDbContext();
        var repo = new CheepRepository(db);

        var author = new Author { Name = "Carl" };
        var cheep = new Cheep { Text = "Testing", TimeStamp = DateTime.UtcNow, Author = author };
        db.Cheeps.Add(cheep);
        await db.SaveChangesAsync();

        var result = await repo.GetCheepByIdAsync(cheep.CheepId);

        Assert.NotNull(result);
        Assert.Equal("Testing", result!.Text);
    }

    [Fact]
    public async Task GetAllCheepsFromAuthorAsync_ShouldReturnOnlyAuthorsCheeps()
    {
        var db = GetInMemoryDbContext();
        var repo = new CheepRepository(db);

        var a1 = new Author { Name = "Alice" };
        var a2 = new Author { Name = "Bob" };
        db.Cheeps.AddRange(
            new Cheep { Text = "A1", Author = a1, TimeStamp = DateTime.UtcNow },
            new Cheep { Text = "B1", Author = a2, TimeStamp = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        var result = await repo.GetAllCheepsFromAuthorAsync("Alice");

        Assert.Single(result);
        Assert.Equal("A1", result.First().Text);
    }
}
