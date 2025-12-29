// test/Chirp.Infrastructure.test/CheepServiceTests.cs
using System;
using System.Linq;
using System.Threading.Tasks;
using Core;
using Infrastructure;
using Infrastructure.Data;
using Infrastructure.Models;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Tests.Infrastructure;

public class CheepServiceTests : IDisposable
{
    private readonly CheepDbContext _context;
    private readonly ICheepRepository _repository;
    private readonly CheepService _service;

    public CheepServiceTests()
    {
        // Use a unique in-memory database for each test run to ensure isolation
        var options = new DbContextOptionsBuilder<CheepDbContext>()
            .UseInMemoryDatabase($"CheepService_{Guid.NewGuid()}")
            .Options;

        _context = new CheepDbContext(options);
        _repository = new CheepRepository(_context);
        _service = new CheepService(_repository);

        Seed40Cheeps(); // Seeds 40 cheeps from one author for consistent pagination testing
    }
    private void Seed40Cheeps()
    {
        var author = new Author { Name = "tester", Email = "t@example.com" };
        _context.Authors.Add(author);
        _context.SaveChanges();

        var cheeps = Enumerable.Range(1, 40).Select(i => new Cheep
        {
            Text = $"Cheep #{i}",
            AuthorId = author.AuthorId,
            TimeStamp = DateTime.UtcNow.AddHours(-i) // Newest cheep has smallest offset
        });

        _context.Cheeps.AddRange(cheeps);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetCheeps_Page1_Returns32Cheeps()
    {
        // First page should return the default page size of 32 cheeps, ordered newest first
        var result = await _service.GetCheeps(1);

        Assert.Equal(32, result.Count);
        Assert.Equal("Cheep #1", result[0].Text); // Most recent cheep
    }

    [Fact]
    public async Task GetCheeps_Page2_ReturnsRemaining8Cheeps()
    {
        // Second page should return the remaining 8 cheeps (40 total - 32 on page 1)
        var result = await _service.GetCheeps(2);

        Assert.Equal(8, result.Count);
        Assert.Equal("Cheep #33", result[0].Text); // Oldest cheeps appear last
    }

    [Fact]
    public async Task TestSeedAsync_PrintsCheeps_WhenDataExists()
    {
        // Captures console output to verify that TestSeedAsync writes expected summary info
        var output = new StringWriter();
        Console.SetOut(output);

        await _service.TestSeedAsync();

        var consoleOutput = output.ToString();
        Assert.Contains("Total cheeps:", consoleOutput);
    }

    [Fact]
    public async Task GetCheeps_InvalidPage_ReturnsFirstPage()
    {
        // Invalid page numbers (null or <= 0) should fall back to page 1
        var page1 = await _service.GetCheeps(1);
        var pageNull = await _service.GetCheeps(null);
        var pageZero = await _service.GetCheeps(0);

        Assert.Equal(page1.Select(c => c.Text), pageNull.Select(c => c.Text));
        Assert.Equal(page1.Select(c => c.Text), pageZero.Select(c => c.Text));
    }

    [Fact]
    public async Task GetCheepsFromAuthor_FormatsTimestamp_Correctly()
    {
        // Verifies that timestamps are formatted as "MM/dd/yy HH:mm:ss" in the DTO
        var author = new Author { Name = "alice", Email = "a@example.com" };
        _context.Authors.Add(author);

        _context.Cheeps.Add(new Cheep
        {
            AuthorId = author.AuthorId,
            Text = "Time test",
            TimeStamp = new DateTime(2025, 11, 28, 14, 3, 9)
        });
        await _context.SaveChangesAsync();

        var result = await _service.GetCheepsFromAuthor("alice", 1);

        Assert.Equal("11/28/25 14:03:09", result[0].TimeStamp);
    }

    [Fact]
    public async Task GetPrivateTimeline_IncludesFollowedUsersCheeps()
    {
        // Private timeline should include cheeps from users that the requested user follows
        var author2 = new Author { Name = "tester2", Email = "t2@example.com" };
        _context.Authors.Add(author2);
        await _context.SaveChangesAsync();

        _context.Cheeps.Add(new Cheep
        {
            AuthorId = author2.AuthorId,
            Text = "Hello from tester2",
            TimeStamp = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        await _repository.FollowUserAsync("tester", "tester2");

        var result = await _service.GetPrivateTimeline("tester");

        Assert.Contains(result, c => c.Text == "Hello from tester2");
        Assert.Contains(result, c => c.AuthorName == "tester2");
    }

    [Fact]
    public async Task GetPrivateTimeline_PaginatesCorrectly()
    {
        // Private timeline for "tester" should paginate the same way as public (32 + 8)
        var page1 = await _service.GetPrivateTimeline("tester", 1);
        var page2 = await _service.GetPrivateTimeline("tester", 2);

        Assert.Equal(32, page1.Count);
        Assert.Equal(8, page2.Count);
    }

    public void Dispose()
    {
        // Clean up the in-memory database
        _context.Dispose();
    }
}