using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Chirp.Infrastructure.Models;

namespace Chirp.Infrastructure.Data
{
    public class CheepDbContext : DbContext
    {
        public CheepDbContext(DbContextOptions<CheepDbContext> options) 
            : base(options)
        {
        }

        public DbSet<Cheep> Cheeps { get; set; } = null!;
        public DbSet<Author> Authors { get; set; } = null!;
    }
}