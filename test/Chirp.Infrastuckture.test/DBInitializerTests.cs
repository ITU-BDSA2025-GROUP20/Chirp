// test/Infrastructure.Tests/DBInitializerTests.cs
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

    /// Sets up an in-memory database for each test.
    /// A new unique database name is generated to ensure test isolation.
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
        // Act: Run the seeding process
        await DbInitializer.SeedDatabaseAsync(_context);

        // Assert: Exactly 12 authors should be added (as defined in the seed data)
        var authorCount = await _context.Authors.CountAsync();
        Assert.Equal(12, authorCount);

        // Assert: A substantial number of cheeps should be added (> 600 is safe based on current seed data)
        var cheepCount = await _context.Cheeps.CountAsync();
        Assert.True(cheepCount > 600, $"Expected more than 600 cheeps, but found {cheepCount}");

        // Spot-check: Verify a known author exists with correct details
        var helge = await _context.Authors.FirstOrDefaultAsync(a => a.Name == "Helge");
        Assert.NotNull(helge);
        Assert.Equal("ropf@itu.dk", helge!.Email);

        // Spot-check: Verify the first cheep (by ID) matches expected content and author
        var firstCheep = await _context.Cheeps
            .Include(c => c.Author)
            .OrderBy(c => c.CheepId)
            .FirstOrDefaultAsync();

        Assert.NotNull(firstCheep);
        Assert.StartsWith("They were married in Chicago", firstCheep!.Text);
        Assert.Equal("Jacqualine Gilcoine", firstCheep.Author.Name);
    }

    /// Verifies that SeedDatabaseAsync does nothing when the database already contains data.
    /// This prevents duplicate seeding in production or repeated test runs.
    [Fact]
    public async Task SeedDatabaseAsync_ExistingData_SkipsSeeding()
    {
        // Arrange: Pre-populate the database with one author and one cheep
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

        // Act: Attempt to seed again
        await DbInitializer.SeedDatabaseAsync(_context);

        // Assert: No additional data from the seed should have been added
        Assert.Equal(1, await _context.Authors.CountAsync());
        Assert.Equal(1, await _context.Cheeps.CountAsync());

        // Verify the original pre-seeded data remains intact
        var author = await _context.Authors.FirstAsync();
        Assert.Equal("existing", author.Name);
    }

    /// Cleans up the in-memory database after each test.
    public void Dispose()
    {
        _context.Dispose();
    }
}