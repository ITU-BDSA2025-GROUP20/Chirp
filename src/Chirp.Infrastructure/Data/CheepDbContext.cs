using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Models;

namespace Infrastructure.Data
{
    public class CheepDbContext : IdentityDbContext<IdentityUser>
    {
        public CheepDbContext(DbContextOptions<CheepDbContext> options)
            : base(options)
        {
        }

        public DbSet<Cheep> Cheeps { get; set; } = null!;
        public DbSet<Author> Authors { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<Cheep>(entity =>
            {
            entity.Property(e => e.TimeStamp)
            .HasColumnType("TEXT");
            });

        }
    }
}