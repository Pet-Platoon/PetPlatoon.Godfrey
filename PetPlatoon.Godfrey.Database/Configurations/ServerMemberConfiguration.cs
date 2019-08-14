using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetPlatoon.Godfrey.Database.Configurations.Common;
using PetPlatoon.Godfrey.Database.Servers;

namespace PetPlatoon.Godfrey.Database.Configurations
{
    public class ServerMemberConfiguration : BaseEntityTypeConfiguration<ServerMember>
    {
        public override void Configure(EntityTypeBuilder<ServerMember> builder)
        {
            builder.HasOne(e => e.User)
                   .WithMany(e => e.Servers)
                   .HasForeignKey(e => e.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(e => e.Server)
                   .WithMany(e => e.Members)
                   .HasForeignKey(e => e.ServerId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
