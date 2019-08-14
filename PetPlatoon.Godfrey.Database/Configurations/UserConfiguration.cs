using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetPlatoon.Godfrey.Database.Configurations.Common;
using PetPlatoon.Godfrey.Database.Users;

namespace PetPlatoon.Godfrey.Database.Configurations
{
    public class UserConfiguration : BaseEntityTypeConfiguration<User>
    {
        public override void Configure(EntityTypeBuilder<User> builder)
        {
            builder.Property(e => e.Name)
                   .IsRequired();

            builder.HasMany(e => e.AuthoredQuotes)
                   .WithOne(e => e.Author)
                   .HasForeignKey(e => e.AuthorId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(e => e.QuotedMessages)
                   .WithOne(e => e.Quoter)
                   .HasForeignKey(e => e.QuoterId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(e => e.OwnedServers)
                   .WithOne(e => e.Owner)
                   .HasForeignKey(e => e.OwnerId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(e => e.Servers)
                   .WithOne(e => e.User)
                   .HasForeignKey(e => e.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
