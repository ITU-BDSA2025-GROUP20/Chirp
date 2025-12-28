// test/Chirp.Infrastuckture.test/DBInitializerTests.cs
using System;
using System.Threading.Tasks;
using Infrastructure.Data;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Tests.Infrastructure;

public class DBInitializerTests : IDisposable
{
    private readonly CheepDbContext _context;

    public DBInitializerTests()
    {
        var options = new DbContextOptionsBuilder<CheepDbContext>()
            .UseInMemoryDatabase(databaseName: $"DBInitializer_{Guid.NewGuid()}")
            .Options;

        _context = new CheepDbContext(options);
    }

    [Fact]
    public async Task SeedDatabaseAsync_EmptyDB_AddsAuthorsAndCheeps()
    {
        // Act
        await DbInitializer.SeedDatabaseAsync(_context);

        // Assert
        var authorCount = await _context.Authors.CountAsync();
        Assert.Equal(12, authorCount); // Exactly 12 authors: Roger Histand → Adrian

        var cheepCount = await _context.Cheeps.CountAsync();
        // The original seed adds many cheeps (at least 657 based on the list in your code)
        Assert.True(cheepCount > 600, $"Expected many cheeps, but got {cheepCount}");

        // Spot-check one known author
        var helge = await _context.Authors.FirstOrDefaultAsync(a => a.Name == "Helge");
        Assert.NotNull(helge);
        Assert.Equal("ropf@itu.dk", helge!.Email);

        // Spot-check the first cheep (from your seed data)
        var firstCheep = await _context.Cheeps
            .Include(c => c.Author)
            .OrderBy(c => c.CheepId)
            .FirstOrDefaultAsync();

        Assert.NotNull(firstCheep);
        Assert.StartsWith("They were married in Chicago", firstCheep!.Text);
        Assert.Equal("Jacqualine Gilcoine", firstCheep.Author.Name); // a10 in your seed
    }

    [Fact]
    public async Task SeedDatabaseAsync_ExistingData_SkipsSeeding()
    {
        // Arrange: Add some data so the seed condition detects the DB is not empty
        var existingAuthor = new Author { Name = "existing", Email = "existing@example.com" };
        _context.Authors.Add(existingAuthor);
        await _context.SaveChangesAsync();

        var existingCheep = new Cheep
        {
            Text = "This already exists",
            AuthorId = existingAuthor.AuthorId,
            TimeStamp = DateTime.UtcNow
        };
        _context.Cheeps.Add(existingCheep);
        await _context.SaveChangesAsync();

        // Act
        await DbInitializer.SeedDatabaseAsync(_context);

        // Assert: No additional authors or cheeps from the big seed were added
        Assert.Equal(1, await _context.Authors.CountAsync());
        Assert.Equal(1, await _context.Cheeps.CountAsync());

        // Double-check that our pre-seeded data is still there and unchanged
        var author = await _context.Authors.FirstAsync();
        Assert.Equal("existing", author.Name);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}