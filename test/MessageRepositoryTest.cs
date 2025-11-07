using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;
using Chirp.Infrastructure;
using Chirp.Infrastructure.Data;
using Chirp.Infrastructure.Models;
using Chirp.Core;

public class MessageRepositoryTests
{
    private CheepDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<CheepDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new CheepDbContext(options);
    }

    [Fact]
    public async Task CreateMessage_ShouldAddNewMessage()
    {
        var db = GetDbContext();
        db.Authors.Add(new Author { Name = "Alice" });
        await db.SaveChangesAsync();

        var repo = new MessageRepository(db);

        var dto = new MessageDTO
        {
            Text = "Testing create",
            AuthorName = "Alice",
            TimeStamp = DateTime.UtcNow
        };

        var id = await repo.CreateMessage(dto);

        Assert.Equal(1, db.Cheeps.Count());
        Assert.Equal("Testing create", db.Cheeps.First().Text);
    }

    [Fact]
    public async Task ReadMessages_ShouldReturnMessagesByUser()
    {
        var db = GetDbContext();
        var author = new Author { Name = "Bob" };
        db.Authors.Add(author);
        db.Cheeps.AddRange(
            new Cheep { Text = "Msg1", Author = author, TimeStamp = DateTime.UtcNow },
            new Cheep { Text = "Other", Author = new Author { Name = "Charlie" }, TimeStamp = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        var repo = new MessageRepository(db);
        var result = await repo.ReadMessages("Bob");

        Assert.Single(result);
        Assert.Equal("Msg1", result[0].Text);
    }

    [Fact]
    public async Task UpdateMessage_ShouldModifyExistingCheepText()
    {
        var db = GetDbContext();
        var author = new Author { Name = "Alice" };
        var cheep = new Cheep { Text = "Old", Author = author, TimeStamp = DateTime.UtcNow };
        db.Cheeps.Add(cheep);
        await db.SaveChangesAsync();

        var repo = new MessageRepository(db);
        var dto = new MessageDTO { Id = cheep.CheepId, Text = "Updated" };
        await repo.UpdateMessage(dto);

        var updated = await db.Cheeps.FindAsync(cheep.CheepId);
        Assert.Equal("Updated", updated!.Text);
    }
}
