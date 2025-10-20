using Microsoft.EntityFrameworkCore;
using Chirp.Razor.Models;

namespace Chirp.Razor.Data
{
    public class CheepDbContext : DbContext
    {
        public CheepDbContext(DbContextOptions<CheepDbContext> options) 
            : base(options)
        {
        }

        public DbSet<Cheep> Cheeps { get; set; } = null!;
        public DbSet<Author> Users { get; set; }
    }
}