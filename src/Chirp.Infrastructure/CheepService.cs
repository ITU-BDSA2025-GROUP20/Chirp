using Infrastructure.Data;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core;
using System.Globalization;

namespace Infrastructure.Services
{
    public class CheepService
    {
        private readonly ICheepRepository _repository;
        private const int PageSize = 32;

        public CheepService(ICheepRepository repository)
        {
            _repository = repository;
        }

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
                    c.AuthorName,
                    c.Text,
                    c.TimeStamp.ToString("MM/dd/yy H:mm:ss", System.Globalization.CultureInfo.InvariantCulture),
                    c.ImageUrl
                ))
                .ToList();
        }

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
                    c.AuthorName,
                    c.Text,
                    c.TimeStamp.ToString("MM/dd/yy H:mm:ss", System.Globalization.CultureInfo.InvariantCulture),
                    c.ImageUrl
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
                Console.WriteLine($"{cheep.AuthorName}: {cheep.Text} ({cheep.TimeStamp})");
            }
        }
        public async Task<List<CheepViewModel>> GetPrivateTimeline(string username, int? page = 1)
        {
            int pageNumber = page ?? 1;
            if (pageNumber < 1) pageNumber = 1;

            var cheeps = await _repository.GetTimelineForUserAsync(username);

            var pagedCheeps = cheeps
                .Skip((pageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            return pagedCheeps.Select(c => new CheepViewModel(
                    c.AuthorName,
                    c.Text,
                    c.TimeStamp.ToString("MM/dd/yy H:mm:ss", System.Globalization.CultureInfo.InvariantCulture),
                    c.ImageUrl
                ))
                .ToList();
        }
    }

    

    public record CheepViewModel(string AuthorName, string? Text, string TimeStamp, string? ImageUrl = null);
}