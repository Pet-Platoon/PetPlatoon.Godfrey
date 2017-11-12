using Godfrey.Models.Servers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Godfrey.Models.Configurations
{
    public class ServerConfiguration : IEntityTypeConfiguration<Server>
    {
        public void Configure(EntityTypeBuilder<Server> builder)
        {
            builder.Property(s => s.Id)
                   .IsRequired();

            builder.Property(s => s.Name)
                   .HasMaxLength(255)
                   .IsRequired();

            builder.HasOne(s => s.Owner)
                   .WithMany(u => u.OwnedServers)
                   .HasForeignKey(s => s.OwnerId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(s => s.Quotes)
                   .WithOne(q => q.Server)
                   .HasForeignKey(q => q.ServerId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(s => s.Members)
                   .WithOne(m => m.Server)
                   .HasForeignKey(m => m.ServerId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
