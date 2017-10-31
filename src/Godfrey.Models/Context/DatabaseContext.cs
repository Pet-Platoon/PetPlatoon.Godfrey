using System.Threading.Tasks;
using Godfrey.Models.Configs;
using Godfrey.Models.Quotes;
using Godfrey.Models.Servers;
using Godfrey.Models.Users;
using Microsoft.EntityFrameworkCore;

namespace Godfrey.Models.Context
{
    public class DatabaseContext : DbContext
    {
        public virtual DbSet<Server> Servers { get; set; }
        public virtual DbSet<Config> Configs { get; set; }
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<Quote> Quotes { get; set; }

        public DatabaseContext(DbContextOptions options) : base(options)
        {

        }
    }

    public static class DatabaseContextFactory
    {

        public static DatabaseContext Create(string connectionString)
        {
            return CreateAsync(connectionString).GetAwaiter().GetResult();
        }

        public static async Task<DatabaseContext> CreateAsync(string connectionString)
        {
            var optionsBuilder = new DbContextOptionsBuilder()
                .UseMySql(connectionString);

            var context = new DatabaseContext(optionsBuilder.Options);
            await context.Database.EnsureCreatedAsync();

            return context;
        }
    }
}
