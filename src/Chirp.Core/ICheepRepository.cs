using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core;
public interface ICheepRepository
{
    Task<IEnumerable<MessageDTO>> GetAllCheepsAsync();
    Task StoreCheepAsync(MessageDTO cheep);
    Task<MessageDTO?> GetCheepByIdAsync(int id);

    Task<IEnumerable<MessageDTO>> GetAllCheepsFromAuthorAsync(string authorName);

}