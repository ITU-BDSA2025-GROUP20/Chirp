// test/Chirp.Infrastructure.Tests/MessageRepositoryTests.cs
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

public class MessageRepositoryTests : IDisposable
{
    private readonly CheepDbContext _context;
    private readonly MessageRepository _repository;

    public MessageRepositoryTests()
    {
        // Use a unique in-memory database for each test run to ensure isolation
        var options = new DbContextOptionsBuilder<CheepDbContext>()
            .UseInMemoryDatabase($"MessageRepo_{Guid.NewGuid()}")
            .Options;

        _context = new CheepDbContext(options);
        _repository = new MessageRepository(_context);
        Seed();
    }

    /// Seeds the in-memory database with one author and two cheeps for consistent test data.
    private void Seed()
    {
        var author = new Author { AuthorId = 1, Name = "bob", Email = "bob@example.com" };
        _context.Authors.Add(author);

        var cheeps = new[]
        {
            new Cheep { CheepId = 10, Text = "Old one", TimeStamp = new DateTime(2025, 1, 1), AuthorId = 1 },
            new Cheep { CheepId = 20, Text = "New one", TimeStamp = new DateTime(2025, 1, 2), AuthorId = 1 }
        };
        _context.Cheeps.AddRange(cheeps);
        _context.SaveChanges();
    }

    [Fact]
    public async Task ReadMessages_ReturnsOnlyUsersMessages()
    {
        // Act
        var messages = await _repository.ReadMessages("bob");

        // Assert
        Assert.Equal(2, messages.Count);
        Assert.All(messages, m => Assert.Equal("bob", m.AuthorName));
    }

    [Fact]
    public async Task CreateMessage_AddsNewCheep_ReturnsId()
    {
        // Arrange
        var dto = new MessageDTO
        {
            AuthorName = "bob",
            Text = "Created via MessageRepository",
            TimeStamp = DateTime.UtcNow
        };

        // Act
        var newId = await _repository.CreateMessage(dto);

        // Assert
        var cheep = await _context.Cheeps.FindAsync(newId);
        Assert.NotNull(cheep);
        Assert.Equal(dto.Text, cheep!.Text);
    }

    [Fact]
    public async Task UpdateMessage_ChangesText()
    {
        // Arrange
        var dto = new MessageDTO
        {
            Id = 10,
            Text = "Updated text!",
            AuthorName = "bob" // Included for completeness, though not used in update logic
        };

        // Act
        await _repository.UpdateMessage(dto);

        // Assert
        var updated = await _context.Cheeps.FindAsync(10);
        Assert.Equal("Updated text!", updated!.Text);
    }

    [Fact]
    public async Task UpdateMessage_NonExisting_DoesNothing()
    {
        // Arrange
        var dto = new MessageDTO { Id = 999, Text = "Ghost" };

        // Act (should not throw)
        await _repository.UpdateMessage(dto);

        // Assert - database remains unchanged
        var stillTwo = await _context.Cheeps.CountAsync();
        Assert.Equal(2, stillTwo);
    }

    public void Dispose()
    {
        // Clean up the in-memory context
        _context.Dispose();
    }
}