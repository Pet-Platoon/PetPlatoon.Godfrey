using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetPlatoon.Godfrey.Database.Configurations.Common;
using PetPlatoon.Godfrey.Database.Servers;

namespace PetPlatoon.Godfrey.Database.Configurations
{
    public class ServerConfiguration : BaseEntityTypeConfiguration<Server>
    {
        public override void Configure(EntityTypeBuilder<Server> builder)
        {
            builder.Property(e => e.Name)
                   .IsRequired()
                   .IsUnicode(false);

            builder.HasOne(e => e.Owner)
                   .WithMany(e => e.OwnedServers)
                   .HasForeignKey(e => e.OwnerId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(e => e.Configs)
                   .WithOne(e => e.Server)
                   .HasForeignKey(e => e.ServerId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(e => e.Quotes)
                   .WithOne(e => e.Server)
                   .HasForeignKey(e => e.ServerId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(e => e.Members)
                   .WithOne(e => e.Server)
                   .HasForeignKey(e => e.ServerId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
