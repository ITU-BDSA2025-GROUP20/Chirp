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

        public DbSet<CheepViewModel> Cheeps { get; set; } = null!;
        public DbSet<UserModel> Users { get; set; }
        public DbSet<MessageModel> Messages { get; set; }
    }
}