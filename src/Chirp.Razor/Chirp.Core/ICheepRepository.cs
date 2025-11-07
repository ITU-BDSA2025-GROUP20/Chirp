using System.Collections.Generic;
using System.Threading.Tasks;
using Chirp.Infrastructure.Models;

namespace Chirp.Core;
public interface ICheepRepository
{
    Task<IEnumerable<Cheep>> GetAllCheepsAsync();
    Task StoreCheepAsync(Cheep cheep);
    Task<Cheep?> GetCheepByIdAsync(int id);

    Task<IEnumerable<Cheep>> GetAllCheepsFromAuthorAsync(string authorName);

}