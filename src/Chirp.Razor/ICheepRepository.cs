using System.Collections.Generic;
using System.Threading.Tasks;
using Chirp.Razor.Models;


public interface ICheepRepository
{
    Task<IEnumerable<Cheep>> GetAllCheepsAsync();
    Task StoreCheepAsync(Cheep cheep);
    Task<Cheep> GetCheepByIdAsync(int id);

}