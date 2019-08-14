using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetPlatoon.Godfrey.Database.Configurations.Common;
using PetPlatoon.Godfrey.Database.Quotes;

namespace PetPlatoon.Godfrey.Database.Configurations
{
    public class QuoteConfiguration : BaseEntityTypeConfiguration<Quote>
    {
        public override void Configure(EntityTypeBuilder<Quote> builder)
        {
            builder.Property(e => e.Message)
                   .IsRequired();

            builder.Property(e => e.CreatedAt)
                   .IsRequired();

            builder.Property(e => e.UpdatedAt)
                   .IsRequired();

            builder.HasOne(e => e.Author)
                   .WithMany(e => e.AuthoredQuotes)
                   .HasForeignKey(e => e.AuthorId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(e => e.Quoter)
                   .WithMany(e => e.QuotedMessages)
                   .HasForeignKey(e => e.QuoterId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(e => e.Server)
                   .WithMany(e => e.Quotes)
                   .HasForeignKey(e => e.ServerId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
