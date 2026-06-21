using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StackOverflowLite.Domain.Entities;

namespace StackOverflowLite.Infrastructure.Persistence.Configurations;

public class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.HasOne(q => q.Author)
            .WithMany(u => u.Questions)
            .HasForeignKey(q => q.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasOne(q => q.AcceptedAnswer)
            .WithMany()
            .HasForeignKey(q => q.AcceptedAnswerId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
