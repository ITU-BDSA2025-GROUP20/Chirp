using Microsoft.EntityFrameworkCore;
using Chirp.Razor.Models;

namespace Chirp.Razor.Data
{
    public class ChirpDbContext : DbContext
    {
        public ChirpDbContext(DbContextOptions<ChirpDbContext> options) 
            : base(options)
        {
        }

        public DbSet Cheeps { get; set; } = null!;
    }
}