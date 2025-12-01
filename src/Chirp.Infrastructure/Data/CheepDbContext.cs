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

        public DbSet<Follow> Follows { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<Cheep>(entity =>
            {
            entity.Property(e => e.TimeStamp)
            .HasColumnType("TEXT");
            });

            builder.Entity<Follow>()
                .HasKey(f => new { f.FollowerId, f.FolloweeId });

            builder.Entity<Follow>()
                .HasOne(f => f.Follower)
                .WithMany(a => a.Following)
                .HasForeignKey(f => f.FollowerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Follow>()
                .HasOne(f => f.Followee)
                .WithMany(a => a.Followers)
                .HasForeignKey(f => f.FolloweeId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    
}