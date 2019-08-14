using Microsoft.EntityFrameworkCore;
using PetPlatoon.Godfrey.Database.Configs;
using PetPlatoon.Godfrey.Database.Configurations;
using PetPlatoon.Godfrey.Database.Quotes;
using PetPlatoon.Godfrey.Database.Servers;
using PetPlatoon.Godfrey.Database.Users;

namespace PetPlatoon.Godfrey.Database
{
    public class DatabaseContext : DbContext
    {
        #region Constructors

        public DatabaseContext()
                : this(new DbContextOptionsBuilder<DatabaseContext>()
                       .UseMySql("server=127.0.0.1;port=3306;uid=root;password=root;database=byzebot").Options)
        {
        }

        public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options)
        {
        }

        #endregion Constructors

        #region Properties
        
        public virtual DbSet<Config> Configs { get; set; }
        public virtual DbSet<Quote> Quotes { get; set; }
        public virtual DbSet<Server> Servers { get; set; }
        public virtual DbSet<ServerMember> ServerMembers { get; set; }
        public virtual DbSet<User> Users { get; set; }

        #endregion Properties

        #region Methods

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new ConfigConfiguration());
            modelBuilder.ApplyConfiguration(new QuoteConfiguration());
            modelBuilder.ApplyConfiguration(new ServerConfiguration());
            modelBuilder.ApplyConfiguration(new ServerMemberConfiguration());
            modelBuilder.ApplyConfiguration(new UserConfiguration());
        }

        #endregion Methods
    }
}
