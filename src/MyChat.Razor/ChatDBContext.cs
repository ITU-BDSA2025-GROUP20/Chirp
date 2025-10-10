using Microsoft.EntityFrameworkCore;

public class ChatDBContext : DbContext
{
    public ChatDBContext(DbContextOptions<ChatDBContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Message> Messages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // Optional: add extra configuration if needed
    }
}
