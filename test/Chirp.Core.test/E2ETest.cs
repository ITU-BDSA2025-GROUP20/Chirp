// test/Chirp.Core.test/E2ETest.cs
using System;
using System.Linq;
using System.Threading.Tasks;
using Core;
using Infrastructure;
using Infrastructure.Data;
using Infrastructure.Models;
using Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Tests.Core;

public class E2ETest : IDisposable
{
    private readonly CheepDbContext _context;
    private readonly CheepRepository _repository;
    private readonly CheepService _service;

    public E2ETest()
    {
        // Use a unique in-memory database for each test run to ensure isolation
        var options = new DbContextOptionsBuilder<CheepDbContext>()
            .UseInMemoryDatabase(databaseName: $"ChirpTest_{Guid.NewGuid()}")
            .Options;

        _context = new CheepDbContext(options);
        _context.Database.EnsureCreated(); // Initialize schema including Identity tables

        _repository = new CheepRepository(_context);
        _service = new CheepService(_repository);
    }

    [Fact]
    public async Task UserPostsCheep_CheepAppearsInPublicTimeline()
    {
        // Post a cheep as "alice"
        await _repository.StoreCheepAsync(new MessageDTO
        {
            AuthorName = "alice",
            Text = "Hello world! This is my first cheep :)"
        });

        // Verify it appears on the public timeline (default view)
        var cheeps = await _service.GetCheeps(page: 1);

        Assert.Single(cheeps);
        Assert.Equal("alice", cheeps[0].AuthorName);
        Assert.Equal("Hello world! This is my first cheep :)", cheeps[0].Text);
        // Check that timestamp is formatted as expected (dd/MM/yy HH:mm:ss)
        Assert.Matches(@"\d{2}/\d{2}/\d{2} \d{2}:\d{2}:\d{2}", cheeps[0].TimeStamp);
    }

    [Fact]
    public async Task UserPostsCheep_CheepAppearsOnTheirOwnAuthorPage()
    {
        // Post a cheep as "bob"
        await _repository.StoreCheepAsync(new MessageDTO
        {
            AuthorName = "bob",
            Text = "Just setting up my chrp"
        });

        // Verify it appears on the author's personal page
        var authorCheeps = await _service.GetCheepsFromAuthor("bob");

        Assert.Single(authorCheeps);
        Assert.Equal("bob", authorCheeps[0].AuthorName);
        Assert.Contains("setting up my chrp", authorCheeps[0].Text);
    }

    [Fact]
    public async Task UserPostsCheep_AppearsInTheirPrivateTimeline()
    {
        var username = "carl";
        // Post a cheep as "carl"
        await _repository.StoreCheepAsync(new MessageDTO
        {
            AuthorName = username,
            Text = "This should only be visible to me and my followers"
        });

        // Verify it appears in the user's private timeline (own + followers)
        var timeline = await _service.GetPrivateTimeline(username);

        Assert.Single(timeline);
        Assert.Equal(username, timeline[0].AuthorName);
    }

    [Fact]
    public async Task WhenUserFollowsAnother_TheirCheepsAppearInTimeline()
    {
        // Arrange: Create cheeps from two users
        await _repository.StoreCheepAsync(new MessageDTO { AuthorName = "alice", Text = "Good morning!" });
        await Task.Delay(10); // Ensure stable timestamp ordering
        await _repository.StoreCheepAsync(new MessageDTO { AuthorName = "bob", Text = "Hey, I'm bob!" });

        // Bob follows Alice
        await _repository.FollowUserAsync("bob", "alice");

        // Verify Alice's cheep appears in Bob's private timeline
        var bobsTimeline = await _service.GetPrivateTimeline("bob");

        Assert.Equal(2, bobsTimeline.Count);
        Assert.Equal("bob", bobsTimeline[0].AuthorName);   // newest first
        Assert.Equal("alice", bobsTimeline[1].AuthorName);
    }

    [Fact]
    public async Task AfterUnfollow_CheepsNoLongerAppearInTimeline()
    {
        // Arrange: Create cheeps and follow/unfollow
        await _repository.StoreCheepAsync(new MessageDTO { AuthorName = "x", Text = "x post" });
        await _repository.StoreCheepAsync(new MessageDTO { AuthorName = "y", Text = "y post" });

        await _repository.FollowUserAsync("y", "x");
        await _repository.UnfollowUserAsync("y", "x");

        // Verify only "y"'s own cheep remains in their private timeline
        var timeline = await _service.GetPrivateTimeline("y");

        Assert.Single(timeline);
        Assert.Equal("y", timeline[0].AuthorName);
    }

    [Fact]
    public async Task Pagination_WorksCorrectly_OnPublicTimeline()
    {
        // Create 50 cheeps to test pagination (32 per page by default)
        for (int i = 1; i <= 50; i++)
        {
            await _repository.StoreCheepAsync(new MessageDTO
            {
                AuthorName = $"user{i % 8}",
                Text = $"Cheep #{i}"
            });
            await Task.Delay(1); // Ensure stable ordering by timestamp
        }

        var page1 = await _service.GetCheeps(1);
        var page2 = await _service.GetCheeps(2);

        Assert.Equal(32, page1.Count);
        Assert.Equal(18, page2.Count);
        Assert.NotEqual(page1[0].Text, page2[0].Text); // Different content
    }

    [Fact]
    public async Task Timestamp_IsCorrectlyFormatted_InAllViews()
    {
        // Arrange: Manually create author and cheep with fixed UTC timestamp
        var author = new Author { Name = "timeguy", Email = "time@example.com" };
        _context.Authors.Add(author);
        await _context.SaveChangesAsync();

        var fixedTime = new DateTime(2025, 12, 25, 15, 45, 30, DateTimeKind.Utc);
        _context.Cheeps.Add(new Cheep
        {
            AuthorId = author.AuthorId,
            Text = "Merry Christmas!",
            TimeStamp = fixedTime
        });
        await _context.SaveChangesAsync();

        // Verify timestamp is formatted correctly in DTO (dd/MM/yy HH:mm:ss)
        var cheeps = await _service.GetCheeps(1);
        Assert.Equal("12/25/25 15:45:30", cheeps[0].TimeStamp);
    }

    [Fact]
    public async Task Author_IsCreatedAutomatically_OnFirstCheep()
    {
        var newUser = "brandnewuser";

        // Ensure author doesn't exist yet
        Assert.False(_context.Authors.Any(a => a.Name == newUser));

        // Post first cheep as new user
        await _repository.StoreCheepAsync(new MessageDTO
        {
            AuthorName = newUser,
            Text = "I'm new here!"
        });

        // Verify author was auto-created with default email
        var author = await _context.Authors.FirstOrDefaultAsync(a => a.Name == newUser);
        Assert.NotNull(author);
        Assert.Equal($"{newUser}@example.com", author.Email);
    }

    public void Dispose()
    {
        // Clean up the in-memory database after each test
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}