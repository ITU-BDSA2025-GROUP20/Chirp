
using Microsoft.EntityFrameworkCore;
using Chirp.Razor.Models;

namespace CheepService.Data
{
    public class CheepDbContext : DbContext
    {
        public ChirpDbContext(DbContextOptions<CheepDbContext> options) 
            : base(options)
        {
            // No implementation needed here - configuration happens externally
        }

        public DbSet Cheeps { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Optional: Configure entity mappings here if needed
            modelBuilder.Entity<Cheep>(entity =>
            {
                entity.HasKey(c => c.Id);
                entity.Property(c => c.Author).IsRequired().HasMaxLength(50);
                entity.Property(c => c.Message).IsRequired().HasMaxLength(160);
                entity.Property(c => c.Timestamp).HasDefaultValueSql("GETUTCDATE()");
            });
            
            // Seed initial data
            modelBuilder.Entity<Cheep>().HasData(
                new Cheep 
                { 
                    Id = 1, 
                    Author = "Helge", 
                    Message = "Hello, BDSA students!", 
                    Timestamp = new DateTime(2023, 8, 1, 10, 16, 48, DateTimeKind.Utc) 
                },
                new Cheep 
                { 
                    Id = 2, 
                    Author = "Rasmus", 
                    Message = "Hej, velkommen til kurset.", 
                    Timestamp = new DateTime(2023, 8, 1, 11, 8, 28, DateTimeKind.Utc) 
                }
            );
        }
    }
}