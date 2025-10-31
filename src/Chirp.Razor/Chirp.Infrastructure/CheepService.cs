using Chirp.Infrastructure.Data;
using Chirp.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using Chirp.Core;

namespace Chirp.Infrastructure.Services
{
    public class CheepService
{
    private readonly ICheepRepository _repository;

    public CheepService(ICheepRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<CheepViewModel>> GetCheeps()
        {
        var cheeps = await _repository.GetAllCheepsAsync();
        return cheeps.Select(c => new CheepViewModel(
            c.Author.Name,
            c.Text,
            c.TimeStamp.ToString("MM/dd/yy H:mm:ss")
            ))
            .ToList();
    }

    public async Task<List<CheepViewModel>> GetCheepsFromAuthor(string authorName)
    {
        var cheeps = await _repository.GetAllCheepsFromAuthorAsync(authorName);
        return cheeps.Select(c => new CheepViewModel(
            c.Author.Name,
            c.Text,
            c.TimeStamp.ToString("MM/dd/yy H:mm:ss")
        ))
            .ToList();
    }
    public async Task TestSeedAsync()
{
    var cheeps = await _repository.GetAllCheepsAsync(); // returns IEnumerable<Cheep>
    var cheepList = cheeps.ToList(); // convert to list for Count property
    Console.WriteLine($"Total cheeps: {cheepList.Count}");
    
    foreach (var cheep in cheepList)
    {
        // Access the entity's Author property
        Console.WriteLine($"{cheep.Author.Name}: {cheep.Text} ({cheep.TimeStamp})");
    }
}

}

public record CheepViewModel(string AuthorName, string Text, string TimeStamp);
}
