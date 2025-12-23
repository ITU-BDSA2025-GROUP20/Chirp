// test/Chirp.Infrastuckture.test/CheepServiceTests.cs
using System;
using System.Linq;
using System.Threading.Tasks;
using Core;
using Infrastructure;
using Infrastructure.Data;
using Infrastructure.Models;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.RazorPages;
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
        var options = new DbContextOptionsBuilder<CheepDbContext>()
            .UseInMemoryDatabase($"CheepService_{Guid.NewGuid()}")
            .Options;

        _context = new CheepDbContext(options);
        _repository = new CheepRepository(_context);
        _service = new CheepService(_repository);

        Seed40Cheeps(); // Enough for pagination testing
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
            TimeStamp = DateTime.UtcNow.AddHours(-i) // newest first
        });

        _context.Cheeps.AddRange(cheeps);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetCheeps_Page1_Returns32Cheeps()
    {
        var result = await _service.GetCheeps(1);
        Assert.Equal(32, result.Count);
        Assert.Equal("Cheep #1", result[0].Text);
    }

    [Fact]
    public async Task GetCheeps_Page2_ReturnsRemaining8Cheeps()
    {
        var result = await _service.GetCheeps(2);
        Assert.Equal(8, result.Count);
        Assert.Equal("Cheep #33", result[0].Text);
    }

    [Fact]
    public async Task GetCheeps_InvalidPage_ReturnsFirstPage()
    {
        var page1 = await _service.GetCheeps(1);
        var pageNull = await _service.GetCheeps(null);
        var pageZero = await _service.GetCheeps(0);

        Assert.Equal(page1.Select(c => c.Text), pageNull.Select(c => c.Text));
        Assert.Equal(page1.Select(c => c.Text), pageZero.Select(c => c.Text));
    }

    [Fact]
    public async Task GetCheepsFromAuthor_FormatsTimestamp_Correctly()
    {
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

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task GetPrivateTimeline_IncludesFollowedUsersCheeps()
    {
        // Arrange - Create tester2 FIRST
        var author2 = new Author { Name = "tester2", Email = "t2@example.com" };
        _context.Authors.Add(author2);
        await _context.SaveChangesAsync();

        // Now create a cheep from tester2
        _context.Cheeps.Add(new Cheep
        {
            AuthorId = author2.AuthorId,
            Text = "Hello from tester2",
            TimeStamp = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        // NOW make tester follow tester2
        await _repository.FollowUserAsync("tester", "tester2");

        // Act
        var result = await _service.GetPrivateTimeline("tester");

        // Assert
        Assert.Contains(result, c => c.Text == "Hello from tester2");
        Assert.Contains(result, c => c.AuthorName == "tester2");
    }

    [Fact]
    public async Task GetPrivateTimeline_PaginatesCorrectly()
    {
        // tester has 40 cheeps from seed
        var page1 = await _service.GetPrivateTimeline("tester", 1);
        var page2 = await _service.GetPrivateTimeline("tester", 2);

        Assert.Equal(32, page1.Count);
        Assert.Equal(8, page2.Count);
    }
}