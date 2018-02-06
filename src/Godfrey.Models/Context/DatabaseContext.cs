using System;
using System.Threading;
using System.Threading.Tasks;
using Godfrey.Models.Common;
using Godfrey.Models.Configs;
using Godfrey.Models.Configurations;
using Godfrey.Models.Quotes;
using Godfrey.Models.Servers;
using Godfrey.Models.Users;
using Microsoft.EntityFrameworkCore;

namespace Godfrey.Models.Context
{
    public class DatabaseContext : DbContext
    {
        public virtual DbSet<Server> Servers { get; set; }
        public virtual DbSet<ServerMember> ServerMembers { get; set; }
        public virtual DbSet<Config> Configs { get; set; }
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<Quote> Quotes { get; set; }

        public DatabaseContext(DbContextOptions options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new QuoteConfiguration());
            modelBuilder.ApplyConfiguration(new ServerConfiguration());
            modelBuilder.ApplyConfiguration(new UserConfiguration());
        }

        public override int SaveChanges()
        {
            var concurrencyTokenEntries = ChangeTracker.Entries<IVersionedEntity>();
            foreach (var entry in concurrencyTokenEntries)
            {
                if (entry.State == EntityState.Unchanged)
                {
                    continue;
                }

                entry.Entity.Version = Guid.NewGuid();
            }

            return base.SaveChanges();
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            var concurrencyTokenEntries = ChangeTracker.Entries<IVersionedEntity>();
            foreach (var entry in concurrencyTokenEntries)
            {
                if (entry.State == EntityState.Unchanged)
                {
                    continue;
                }

                entry.Entity.Version = Guid.NewGuid();
            }

            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            var concurrencyTokenEntries = ChangeTracker.Entries<IVersionedEntity>();
            foreach (var entry in concurrencyTokenEntries)
            {
                if (entry.State == EntityState.Unchanged)
                {
                    continue;
                }

                entry.Entity.Version = Guid.NewGuid();
            }

            return base.SaveChangesAsync(cancellationToken);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = new CancellationToken())
        {
            var concurrencyTokenEntries = ChangeTracker.Entries<IVersionedEntity>();
            foreach (var entry in concurrencyTokenEntries)
            {
                if (entry.State == EntityState.Unchanged)
                {
                    continue;
                }

                entry.Entity.Version = Guid.NewGuid();
            }

            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
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
