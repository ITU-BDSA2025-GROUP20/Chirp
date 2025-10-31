using Chirp.Razor.Data;
using Chirp.Razor.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Chirp.Razor.Services
{
    public class CheepService
    {
        private readonly ICheepRepository _repository;
        private const int PageSize = 32;

        public CheepService(ICheepRepository repository)
        {
            _repository = repository;
        }

        /// Gets all cheeps, paginated. Page 1 is returned if page is null or <= 0.
        public async Task<List<CheepViewModel>> GetCheeps(int? page = 1)
        {
            int pageNumber = page ?? 1;
            if (pageNumber < 1) pageNumber = 1;

            var cheeps = await _repository.GetAllCheepsAsync();

            var pagedCheeps = cheeps
                .OrderByDescending(c => c.TimeStamp)
                .Skip((pageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            return pagedCheeps.Select(c => new CheepViewModel(
                    c.Author.Name,
                    c.Text,
                    c.TimeStamp.ToString("MM/dd/yy H:mm:ss")
                ))
                .ToList();
        }

        
        /// Gets cheeps from a specific author, paginated. Page 1 is returned if page is null or <= 0.
        public async Task<List<CheepViewModel>> GetCheepsFromAuthor(string authorName, int? page = 1)
        {
            int pageNumber = page ?? 1;
            if (pageNumber < 1) pageNumber = 1;

            var cheeps = await _repository.GetAllCheepsFromAuthorAsync(authorName);

            var pagedCheeps = cheeps
                .OrderByDescending(c => c.TimeStamp)
                .Skip((pageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            return pagedCheeps.Select(c => new CheepViewModel(
                    c.Author.Name,
                    c.Text,
                    c.TimeStamp.ToString("MM/dd/yy H:mm:ss")
                ))
                .ToList();
        }

        public async Task TestSeedAsync()
        {
            var cheeps = await _repository.GetAllCheepsAsync();
            var cheepList = cheeps.ToList();

            Console.WriteLine($"Total cheeps: {cheepList.Count}");

            foreach (var cheep in cheepList)
            {
                Console.WriteLine($"{cheep.Author.Name}: {cheep.Text} ({cheep.TimeStamp})");
            }
        }
    }

    public record CheepViewModel(string AuthorName, string Text, string TimeStamp);
}