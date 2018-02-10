using Godfrey.Models.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Godfrey.Models.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.Property(u => u.Name)
                   .HasMaxLength(255)
                   .IsRequired();

            builder.Property(e => e.Version)
                   .IsConcurrencyToken()
                   .IsRequired();

            builder.HasMany(u => u.AuthoredQuotes)
                   .WithOne(q => q.Author)
                   .HasForeignKey(q => q.AuthorId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(u => u.QuotedMessages)
                   .WithOne(q => q.Quoter)
                   .HasForeignKey(q => q.QuoterId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(u => u.OwnedServers)
                   .WithOne(s => s.Owner)
                   .HasForeignKey(s => s.OwnerId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(u => u.Servers)
                   .WithOne(m => m.User)
                   .HasForeignKey(m => m.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
