using Chirp.Razor.Data;
using Chirp.Razor.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace Chirp.Razor.Services
{
    public class CheepService
    {
        private readonly CheepDbContext _context;

        public CheepService(ICheepRepository repository)
        {
            _repository = repository;
        }

        public List<CheepViewModel> GetCheeps()
        {
            return _repository.GetAllCheepsAsync()
                .Select(c => new CheepViewModel(
                    c.Author.Name,
                    c.Message,
                    c.Timestamp.ToString("MM/dd/yy H:mm:ss")
                ))
                .ToList();
        }

        public List<CheepViewModel> GetCheepsFromAuthor(string authorName)
        {
            return _repository.GetCheepsFromAuthorAsync(authorName)
                .Select(c => new CheepViewModel(
                    c.Author.Name,
                    c.Message,
                    c.Timestamp.ToString("MM/dd/yy H:mm:ss")
                ))
                .ToList();
        }

        public List<CheepViewModel> GetCheepsFromAuthor(string authorName)
        {
            return _context.Cheeps
                .Include(c => c.Author)
                .Where(c => c.Author.Name == authorName)
                .OrderByDescending(c => c.Timestamp)
                .Select(c => new CheepViewModel(
                    c.Author.Name,
                    c.Message,
                    c.Timestamp.ToString("MM/dd/yy H:mm:ss")
                ))
                .ToList();
        }
    }

    public record CheepViewModel(string Author, string Message, string Timestamp);
}
