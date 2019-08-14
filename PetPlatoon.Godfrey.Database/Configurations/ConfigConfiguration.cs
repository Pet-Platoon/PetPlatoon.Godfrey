using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetPlatoon.Godfrey.Database.Configs;
using PetPlatoon.Godfrey.Database.Configurations.Common;

namespace PetPlatoon.Godfrey.Database.Configurations
{
    public class ConfigConfiguration : BaseEntityTypeConfiguration<Config>
    {
        public override void Configure(EntityTypeBuilder<Config> builder)
        {
            builder.HasOne(e => e.Server)
                   .WithMany(e => e.Configs)
                   .HasForeignKey(e => e.ServerId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.Property(e => e.Key)
                   .IsRequired();
        }
    }
}
