using System.Linq;
using System;
using Microsoft.EntityFrameworkCore;
using Chirp.Razor.Data;
using Chirp.Razor.Models;


namespace Chirp.Razor;

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
            .OrderByDescending(c => c.Timestamp)
            .ToListAsync();
    }

    public async Task StoreCheepAsync(Cheep cheep)
    {
        await _dbcontext.Cheeps.AddAsync(cheep);
        await _dbcontext.SaveChangesAsync();
    }
    
    public async Task<Cheep> GetCheepByIdAsync(int id)
    {
        return await _dbcontext.Cheeps
            .Include(c => c.Author)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

}
