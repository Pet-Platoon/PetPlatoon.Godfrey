using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetPlatoon.Godfrey.Database.Common;
using PetPlatoon.Godfrey.Database.Quotes;

namespace PetPlatoon.Godfrey.Database.Configurations.Common
{
    public abstract class BaseEntityTypeConfiguration<T> : IEntityTypeConfiguration<T> where T : BaseEntity
    {
        public abstract void Configure(EntityTypeBuilder<T> builder);

        protected void ConfigureVersionedEntity<TVersioned>(EntityTypeBuilder<TVersioned> builder)
                where TVersioned : class, IVersionedEntity
        {
            builder.HasIndex(e => e.Version)
                   .IsUnique();

            builder.Property(e => e.Version)
                   .IsRequired()
                   .IsConcurrencyToken()
                   .ValueGeneratedNever();
        }
    }
}
