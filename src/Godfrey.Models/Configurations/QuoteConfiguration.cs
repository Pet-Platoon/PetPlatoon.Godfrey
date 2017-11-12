using Godfrey.Models.Quotes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Godfrey.Models.Configurations
{
    public class QuoteConfiguration : IEntityTypeConfiguration<Quote>
    {
        public void Configure(EntityTypeBuilder<Quote> builder)
        {
            builder.HasOne(q => q.Author)
                   .WithMany(a => a.AuthoredQuotes)
                   .HasForeignKey(q => q.AuthorId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(q => q.Quoter)
                   .WithMany(q => q.QuotedMessages)
                   .HasForeignKey(q => q.QuoterId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
