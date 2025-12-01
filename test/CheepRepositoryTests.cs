using System;
using System.Linq;
using System.Threading.Tasks;
using Core;
using Infrastructure;
using Infrastructure.Data;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Tests;

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

    [Fact] public async Task GetAllCheepsAsync_ReturnsAll_InCorrectOrder() =>
        Assert.Equal("Third!", (await _repository.GetAllCheepsAsync()).First().Text);

    [Fact] public async Task GetCheepByIdAsync_Existing_ReturnsCheep() =>
        Assert.Equal("First!", (await _repository.GetCheepByIdAsync(1))!.Text);

    [Fact] public async Task GetCheepByIdAsync_NonExisting_ReturnsNull() =>
        Assert.Null(await _repository.GetCheepByIdAsync(999));

    [Fact] public async Task GetAllCheepsFromAuthorAsync_ReturnsOnlyThatAuthorsCheeps()
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
        Assert.Equal("carl@example.com", author.Email);

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
        Assert.Equal(3, aliceCheeps);
    }

    public void Dispose() => _context.Dispose();
}