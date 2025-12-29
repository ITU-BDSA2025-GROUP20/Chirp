using Infrastructure.Data;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Infrastructure.Services
{
    public class CheepService
    {
        private readonly ICheepRepository _repository;
        private const int PageSize = 32;

        private static readonly TimeZoneInfo CetZone =
            TimeZoneInfo.FindSystemTimeZoneById(
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? "Central European Standard Time"
                    : "Europe/Paris"
            );

        public CheepService(ICheepRepository repository)
        {
            _repository = repository;
        }

        private static string FormatCet(DateTime utcTime)
        {
            var cetTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, CetZone);
            return cetTime.ToString("dd/MM/yy HH:mm:ss", CultureInfo.InvariantCulture);
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

            return pagedCheeps
                .Select(c => new CheepViewModel(
                    c.AuthorName,
                    c.Text,
                    FormatCet(c.TimeStamp),
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

            return pagedCheeps
                .Select(c => new CheepViewModel(
                    c.AuthorName,
                    c.Text,
                    FormatCet(c.TimeStamp),
                    c.ImageUrl
                ))
                .ToList();
        }

        public async Task<List<CheepViewModel>> GetPrivateTimeline(string username, int? page = 1)
        {
            int pageNumber = page ?? 1;
            if (pageNumber < 1) pageNumber = 1;

            var cheeps = await _repository.GetTimelineForUserAsync(username);

            // Repository already returns newest-first
            var pagedCheeps = cheeps
                .Skip((pageNumber - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            return pagedCheeps
                .Select(c => new CheepViewModel(
                    c.AuthorName,
                    c.Text,
                    FormatCet(c.TimeStamp),
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
                Console.WriteLine($"{cheep.AuthorName}: {cheep.Text} ({FormatCet(cheep.TimeStamp)})");
            }
        }
    }

    public record CheepViewModel(
        string AuthorName,
        string? Text,
        string TimeStamp,
        string? ImageUrl = null
    );
}