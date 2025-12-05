// File: CheepService_IntegrationTests.cs
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

namespace Chirp.Tests.Integration;

public class CheepService_IntegrationTests : IDisposable
{
    private readonly CheepDbContext _context;
    private readonly CheepRepository _repository;
    private readonly CheepService _service;

    public CheepService_IntegrationTests()
    {
        var options = new DbContextOptionsBuilder<CheepDbContext>()
            .UseInMemoryDatabase(databaseName: $"ChirpTest_{Guid.NewGuid()}")
            .Options;

        _context = new CheepDbContext(options);
        _context.Database.EnsureCreated(); // Creates Identity + your tables

        _repository = new CheepRepository(_context);
        _service = new CheepService(_repository);
    }

    [Fact]
    public async Task UserPostsCheep_CheepAppearsInPublicTimeline()
    {
        // Act: User posts a cheep via the real flow
        await _repository.StoreCheepAsync(new MessageDTO
        {
            AuthorName = "alice",
            Text = "Hello world! This is my first cheep :)"
        });

        // Assert: It appears on the public timeline
        var cheeps = await _service.GetCheeps(page: 1);

        Assert.Single(cheeps);
        Assert.Equal("alice", cheeps[0].AuthorName);
        Assert.Equal("Hello world! This is my first cheep :)", cheeps[0].Text);
        Assert.Matches(@"\d{2}/\d{2}/\d{2} \d{2}:\d{2}:\d{2}", cheeps[0].TimeStamp);
    }

    [Fact]
    public async Task UserPostsCheep_CheepAppearsOnTheirOwnAuthorPage()
    {
        await _repository.StoreCheepAsync(new MessageDTO
        {
            AuthorName = "bob",
            Text = "Just setting up my chrp"
        });

        var authorCheeps = await _service.GetCheepsFromAuthor("bob");

        Assert.Single(authorCheeps);
        Assert.Equal("bob", authorCheeps[0].AuthorName);
        Assert.Contains("setting up my chrp", authorCheeps[0].Text);
    }

    [Fact]
    public async Task UserPostsCheep_AppearsInTheirPrivateTimeline()
    {
        var username = "carl";
        await _repository.StoreCheepAsync(new MessageDTO
        {
            AuthorName = username,
            Text = "This should only be visible to me and my followers"
        });

        var timeline = await _service.GetPrivateTimeline(username);

        Assert.Single(timeline);
        Assert.Equal(username, timeline[0].AuthorName);
    }

    [Fact]
    public async Task WhenUserFollowsAnother_TheirCheepsAppearInTimeline()
    {
        // Arrange
        await _repository.StoreCheepAsync(new MessageDTO { AuthorName = "alice", Text = "Good morning!" });
        await Task.Delay(10); // Ensure different timestamps
        await _repository.StoreCheepAsync(new MessageDTO { AuthorName = "bob", Text = "Hey, I'm bob!" });

        await _repository.FollowUserAsync("bob", "alice");

        // Act
        var bobsTimeline = await _service.GetPrivateTimeline("bob");

        // Assert
        Assert.Equal(2, bobsTimeline.Count);
        Assert.Equal("bob", bobsTimeline[0].AuthorName);   // newest first
        Assert.Equal("alice", bobsTimeline[1].AuthorName);
    }

    [Fact]
    public async Task AfterUnfollow_CheepsNoLongerAppearInTimeline()
    {
        await _repository.StoreCheepAsync(new MessageDTO { AuthorName = "x", Text = "x post" });
        await _repository.StoreCheepAsync(new MessageDTO { AuthorName = "y", Text = "y post" });

        await _repository.FollowUserAsync("y", "x");
        await _repository.UnfollowUserAsync("y", "x");

        var timeline = await _service.GetPrivateTimeline("y");

        Assert.Single(timeline);
        Assert.Equal("y", timeline[0].AuthorName);
    }

    [Fact]
    public async Task Pagination_WorksCorrectly_OnPublicTimeline()
    {
        // Create 50 cheeps from different authors
        for (int i = 1; i <= 50; i++)
        {
            await _repository.StoreCheepAsync(new MessageDTO
            {
                AuthorName = $"user{i % 8}",
                Text = $"Cheep #{i}"
            });
            await Task.Delay(1); // ensure unique timestamps for stable ordering
        }

        var page1 = await _service.GetCheeps(1);
        var page2 = await _service.GetCheeps(2);

        Assert.Equal(32, page1.Count);
        Assert.Equal(18, page2.Count);
        Assert.NotEqual(page1[0].Text, page2[0].Text);
    }

    [Fact]
    public async Task Timestamp_IsCorrectlyFormatted_InAllViews()
    {
        // Insert with known timestamp (bypass UtcNow)
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

        var cheeps = await _service.GetCheeps(1);

        Assert.Equal("12/25/25 15:45:30", cheeps[0].TimeStamp);
    }

    [Fact]
    public async Task Author_IsCreatedAutomatically_OnFirstCheep()
    {
        var newUser = "brandnewuser";

        Assert.False(_context.Authors.Any(a => a.Name == newUser));

        await _repository.StoreCheepAsync(new MessageDTO
        {
            AuthorName = newUser,
            Text = "I'm new here!"
        });

        var author = await _context.Authors.FirstOrDefaultAsync(a => a.Name == newUser);
        Assert.NotNull(author);
        Assert.Equal($"{newUser}@example.com", author.Email);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}