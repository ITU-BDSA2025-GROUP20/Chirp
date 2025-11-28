using System.Linq;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;
using Infrastructure.Models;
using Core;

namespace Infrastructure;

public class CheepRepository : ICheepRepository
{
    private readonly CheepDbContext _dbcontext;

    public CheepRepository(CheepDbContext dbcontext)
    {
        _dbcontext = dbcontext;
    }

    public async Task<IEnumerable<MessageDTO>> GetAllCheepsAsync()
    {
        var cheeps = await _dbcontext.Cheeps
            .Include(c => c.Author)
            .OrderByDescending(c => c.TimeStamp)
            .ToListAsync();

        // Map entity → DTO
        return cheeps.Select(c => new MessageDTO
        {
            Id = c.CheepId,
            Text = c.Text,
            AuthorName = c.Author.Name,
            TimeStamp = c.TimeStamp
        }).ToList();
    }

    public async Task<MessageDTO?> GetCheepByIdAsync(int id)
    {
        var cheep = await _dbcontext.Cheeps
            .Include(c => c.Author)
            .FirstOrDefaultAsync(c => c.CheepId == id);

        if (cheep == null) return null;

        return new MessageDTO
        {
            Id = cheep.CheepId,
            Text = cheep.Text,
            AuthorName = cheep.Author.Name
        };
    }

    public async Task<IEnumerable<MessageDTO>> GetAllCheepsFromAuthorAsync(string authorName)
    {
        var cheeps = await _dbcontext.Cheeps
            .Include(c => c.Author)
            .Where(c => c.Author.Name == authorName)
            .OrderByDescending(c => c.TimeStamp)
            .ToListAsync();

        return cheeps.Select(c => new MessageDTO
        {
            Id = c.CheepId,
            Text = c.Text,
            AuthorName = c.Author.Name,
            TimeStamp = c.TimeStamp
        }).ToList();
    }

    public async Task StoreCheepAsync(MessageDTO message)
    {
        // Map DTO → entity
        var cheepEntity = new Cheep
        {
            Text = message.Text,
            AuthorId = 0, // or map properly based on your Author logic
            TimeStamp = DateTime.UtcNow
        };

        _dbcontext.Cheeps.Add(cheepEntity);
        await _dbcontext.SaveChangesAsync();
    }
}
