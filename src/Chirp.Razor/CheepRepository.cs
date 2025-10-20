using System.Linq;
using System;
using Chirp.Razor.Data;


namespace Chirp.Razor;

public class CheepRepository : ICheepRepository
{
    private readonly CheepDbContext _dbcontext;

    public CheepRepository(CheepDbContext dbcontext) {

        _dbcontext = dbcontext;
    }

    public IEnumerable<CheepViewModel> GetAll()
    {
        return _dbcontext.Cheeps;
    }

    public StoreCheep(CheepViewModel cheep) {
        _dbcontext.Cheeps.Add(cheep);
        
    }

}
