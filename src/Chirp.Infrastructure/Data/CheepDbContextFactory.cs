using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Data
{
    public class CheepDbContextFactory : IDesignTimeDbContextFactory<CheepDbContext>
    {
        public CheepDbContext CreateDbContext(string[] args)
        {
            // Load configuration (so we can use the same connection string as in appsettings.json)
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var connectionString = config.GetConnectionString("DefaultConnection");

            var optionsBuilder = new DbContextOptionsBuilder<CheepDbContext>();
            optionsBuilder.UseSqlite(connectionString);

            return new CheepDbContext(optionsBuilder.Options);
        }
    }
}
