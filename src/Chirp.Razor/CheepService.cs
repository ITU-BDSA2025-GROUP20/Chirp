using Chirp.Razor.Data;
using Chirp.Razor.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace Chirp.Razor.Services
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
            AuthorName=c.Author.Name,
            Message=c.Message,
            Timestamp=c.Timestamp.ToString("MM/dd/yy H:mm:ss")
            ))
            .ToList();
    }

    public async Task<List<CheepViewModel>> GetCheepsFromAuthor(string authorName)
    {
        var cheeps = await _repository.GetCheepsFromAuthorAsync(authorName);
        return cheeps.Select(c => new CheepViewModel(
            AuthorName=c.Author.Name,
            Message=c.Message,
            Timestamp=c.Timestamp.ToString("MM/dd/yy H:mm:ss")
        ))
            .ToList();
    }

}

public record CheepViewModel(string AuthorName, string Message, string Timestamp);
}
