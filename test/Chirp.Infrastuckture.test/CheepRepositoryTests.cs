// test/Chirp.Infrastuckture.test/CheepRepositoryTests.cs
using System;
using System.Linq;
using System.Threading.Tasks;
using Core;
using Infrastructure;
using Infrastructure.Data;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Tests.Infrastructure;

public class CheepRepositoryTests : IDisposable
{
    private readonly CheepDbContext _context;
    private readonly CheepRepository _repository;

    public CheepRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<CheepDbContext>()
            .UseInMemoryDatabase($"CheepRepo_{Guid.NewGuid()}")
            .Options;

        _context = new CheepDbContext(options);
        _repository = new CheepRepository(_context);
        SeedDatabase();
    }

    private void SeedDatabase()
    {
        var authors = new[]
        {
            new Author { AuthorId = 1, Name = "alice", Email = "a@example.com" },
            new Author { AuthorId = 2, Name = "bob",   Email = "b@example.com" }
        };
        _context.Authors.AddRange(authors);

        var cheeps = new[]
        {
            new Cheep { CheepId = 1, Text = "First!", TimeStamp = new DateTime(2025, 1, 1), AuthorId = 1 },
            new Cheep { CheepId = 2, Text = "Second", TimeStamp = new DateTime(2025, 1, 2), AuthorId = 2 },
            new Cheep { CheepId = 3, Text = "Third!", TimeStamp = new DateTime(2025, 1, 3), AuthorId = 1 }
        };
        _context.Cheeps.AddRange(cheeps);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetAllCheepsAsync_ReturnsAll_InCorrectOrder()
    {
        // Cheeps should be returned newest first
        var cheeps = await _repository.GetAllCheepsAsync();
        Assert.Equal("Third!", cheeps.First().Text);
    }

    [Fact]
    public async Task GetCheepByIdAsync_Existing_ReturnsCheep()
    {
        var cheep = await _repository.GetCheepByIdAsync(1);
        Assert.Equal("First!", cheep!.Text);
    }

    [Fact]
    public async Task GetCheepByIdAsync_NonExisting_ReturnsNull()
    {
        Assert.Null(await _repository.GetCheepByIdAsync(999));
    }

    [Fact]
    public async Task GetAllCheepsFromAuthorAsync_ReturnsOnlyThatAuthorsCheeps()
    {
        var result = await _repository.GetAllCheepsFromAuthorAsync("alice");
        Assert.Equal(2, result.Count());
        Assert.All(result, c => Assert.Equal("alice", c.AuthorName));
    }

    [Fact]
    public async Task StoreCheepAsync_NewAuthor_CreatesAuthorAndCheep()
    {
        var dto = new MessageDTO { AuthorName = "carl", Text = "Hey!" };
        await _repository.StoreCheepAsync(dto);

        var author = await _context.Authors.SingleAsync(a => a.Name == "carl");
        Assert.Equal("carl@example.com", author.Email); // Email auto-generated as {name}@example.com

        var cheep = await _context.Cheeps.Include(c => c.Author)
            .SingleAsync(c => c.Author.Name == "carl");
        Assert.Equal("Hey!", cheep.Text);
    }

    [Fact]
    public async Task StoreCheepAsync_ExistingAuthor_OnlyCreatesCheep()
    {
        var dto = new MessageDTO { AuthorName = "alice", Text = "Again!" };
        await _repository.StoreCheepAsync(dto);

        var aliceCheeps = await _context.Cheeps.CountAsync(c => c.Author.Name == "alice");
        Assert.Equal(3, aliceCheeps); // Was 2 from seed data, now 3
    }

    [Fact]
    public async Task GetFollowingNamesAsync_NoFollows_ReturnsEmpty()
    {
        var result = await _repository.GetFollowingNamesAsync("nonexistent");
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetFollowingNamesAsync_WithFollows_ReturnsSortedNames()
    {
        // Ensure carl exists before following
        var carl = new Author { Name = "carl", Email = "carl@example.com" };
        _context.Authors.Add(carl);
        await _context.SaveChangesAsync();

        await _repository.FollowUserAsync("alice", "bob");
        await _repository.FollowUserAsync("alice", "carl");

        var result = await _repository.GetFollowingNamesAsync("alice");
        Assert.Equal(new[] { "bob", "carl" }, result.ToArray()); // Alphabetically sorted
    }

    [Fact]
    public async Task FollowUserAsync_AddsFollowRelation()
    {
        await _repository.FollowUserAsync("alice", "bob");

        var follow = await _context.Follows
            .SingleOrDefaultAsync(f => f.FollowerId == 1 && f.FolloweeId == 2);

        Assert.NotNull(follow);
    }

    [Fact]
    public async Task FollowUserAsync_DoesNotDuplicateFollow()
    {
        _context.Follows.Add(new Follow { FollowerId = 1, FolloweeId = 2 });
        await _context.SaveChangesAsync();

        await _repository.FollowUserAsync("alice", "bob");

        var count = await _context.Follows
            .CountAsync(f => f.FollowerId == 1 && f.FolloweeId == 2);

        Assert.Equal(1, count); // Still only one follow relationship
    }

    [Fact]
    public async Task UnfollowUserAsync_RemovesFollowRelation()
    {
        _context.Follows.Add(new Follow { FollowerId = 1, FolloweeId = 2 });
        await _context.SaveChangesAsync();

        await _repository.UnfollowUserAsync("alice", "bob");

        var follow = await _context.Follows
            .SingleOrDefaultAsync(f => f.FollowerId == 1 && f.FolloweeId == 2);

        Assert.Null(follow);
    }

    [Fact]
    public async Task IsFollowingAsync_ReturnsCorrectValues()
    {
        _context.Follows.Add(new Follow { FollowerId = 1, FolloweeId = 2 });
        await _context.SaveChangesAsync();

        Assert.True(await _repository.IsFollowingAsync("alice", "bob"));
        Assert.False(await _repository.IsFollowingAsync("bob", "alice"));
    }

    [Fact]
    public async Task GetTimelineForUserAsync_ReturnsOwnAndFollowedCheeps()
    {
        await _repository.FollowUserAsync("alice", "bob");

        var timeline = (await _repository.GetTimelineForUserAsync("alice")).ToList();

        Assert.Equal(3, timeline.Count); // Alice: 2 cheeps, Bob: 1 cheep
        Assert.Equal("Third!", timeline[0].Text); // Newest first
    }

    [Fact]
    public async Task FollowUserAsync_SelfFollow_DoesNothing()
    {
        await _repository.FollowUserAsync("alice", "alice");
        Assert.False(await _repository.IsFollowingAsync("alice", "alice"));
    }

    [Fact]
    public async Task FollowUserAsync_NonExistentFollowee_DoesNothing()
    {
        await _repository.FollowUserAsync("alice", "nonexistent");
        Assert.Empty(await _context.Follows.ToListAsync());
    }

    [Fact]
    public async Task GetTimelineForUserAsync_NonExistentUser_ReturnsEmpty()
    {
        var result = await _repository.GetTimelineForUserAsync("nonexistent");
        Assert.Empty(result);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}