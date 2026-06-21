using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StackOverflowLite.Domain.Entities;

namespace StackOverflowLite.Infrastructure.Persistence.Configurations;

public class QuestionTagConfiguration : IEntityTypeConfiguration<QuestionTag>
{
    public void Configure(EntityTypeBuilder<QuestionTag> builder)
    {
        builder.HasKey(qt => new { qt.QuestionId, qt.TagId });
        
        builder.HasOne(qt => qt.Question)
            .WithMany(q => q.QuestionTags)
            .HasForeignKey(qt => qt.QuestionId);
            
        builder.HasOne(qt => qt.Tag)
            .WithMany(t => t.QuestionTags)
            .HasForeignKey(qt => qt.TagId);
    }
}
