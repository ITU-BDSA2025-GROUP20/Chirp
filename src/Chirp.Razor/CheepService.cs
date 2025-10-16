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

        public CheepService(CheepDbContext context)
        {
            _context = context;
        }

        public List<CheepViewModel> GetCheeps()
        {
            var cheeps = _context.Cheeps
                .OrderByDescending(c => c.Timestamp)
                .ToList();
                
            return cheeps.Select(c => new CheepViewModel(
                c.Author, 
                c.Message, 
                c.Timestamp.ToString("MM/dd/yy H:mm:ss")
            )).ToList();
        }

        public List<CheepViewModel> GetCheepsFromAuthor(string author)
        {
            var cheeps = _context.Cheeps
                .Where(c => c.Author == author)
                .OrderByDescending(c => c.Timestamp)
                .ToList();
                
            return cheeps.Select(c => new CheepViewModel(
                c.Author, 
                c.Message, 
                c.Timestamp.ToString("MM/dd/yy H:mm:ss")
            )).ToList();
        }
    }
    
    public record CheepViewModel(string Author, string Message, string Timestamp);
}