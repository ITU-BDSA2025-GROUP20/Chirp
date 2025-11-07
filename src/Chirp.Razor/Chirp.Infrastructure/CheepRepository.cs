using System.Linq;
using System;
using Microsoft.EntityFrameworkCore;
using Chirp.Infrastructure.Data;
using Chirp.Infrastructure.Models;
using Chirp.Core;


namespace Chirp.Infrastructure;

public class CheepRepository : ICheepRepository
{
    private readonly CheepDbContext _dbcontext;

    public CheepRepository(CheepDbContext dbcontext) {

        _dbcontext = dbcontext;
    }

    public async Task<IEnumerable<Cheep>> GetAllCheepsAsync()
    {
        return await _dbcontext.Cheeps
            .Include(c => c.Author)
            .OrderByDescending(c => c.TimeStamp)
            .ToListAsync();
    }

    public async Task StoreCheepAsync(Cheep cheep)
    {
        await _dbcontext.Cheeps.AddAsync(cheep);
        await _dbcontext.SaveChangesAsync();
    }

    public async Task<Cheep?> GetCheepByIdAsync(int id)
    {
        return await _dbcontext.Cheeps
            .Include(c => c.Author)
            .FirstOrDefaultAsync(c => c.CheepId == id);
    }
    public async Task<IEnumerable<Cheep>> GetAllCheepsFromAuthorAsync(string authorName)
    {
        return await _dbcontext.Cheeps
            .Include(c => c.Author)
            .Where(c => c.Author.Name == authorName)
            .OrderByDescending(c => c.TimeStamp)
            .ToListAsync();
    }

}
