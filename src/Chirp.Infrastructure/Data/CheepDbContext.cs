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
            }); // Upon creation of a CheepDbContext, the property TimeStamp of cheep is set as TEXT

            builder.Entity<Follow>()
                .HasKey(f => new { f.FollowerId, f.FolloweeId }); // Define the primary key of Follow as FollowId and FolloweeId such that no person can follow the same person twice.

            builder.Entity<Follow>()
                .HasOne(f => f.Follower) // Further clarification that a Follow has one Follower.
                .WithMany(a => a.Following) // With many Following, I.E. one person can follow many people.
                .HasForeignKey(f => f.FollowerId) 
                .OnDelete(DeleteBehavior.Cascade); // Should the Follower be deleted, also delete follow records.

            builder.Entity<Follow>() // Inverse of above.
                .HasOne(f => f.Followee)
                .WithMany(a => a.Followers)
                .HasForeignKey(f => f.FolloweeId)
                .OnDelete(DeleteBehavior.Cascade); 
        } 
    }

    
}
