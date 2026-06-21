using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StackOverflowLite.Domain.Entities;

namespace StackOverflowLite.Infrastructure.Persistence.Configurations;

public class VoteConfiguration : IEntityTypeConfiguration<Vote>
{
    public void Configure(EntityTypeBuilder<Vote> builder)
    {
        builder.HasIndex(v => new { v.VoterId, v.QuestionId }).IsUnique();
        builder.HasIndex(v => new { v.VoterId, v.AnswerId }).IsUnique();
        
        builder.HasOne(v => v.Voter)
            .WithMany(u => u.Votes)
            .HasForeignKey(v => v.VoterId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasOne(v => v.Question)
            .WithMany(q => q.Votes)
            .HasForeignKey(v => v.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
            
        builder.HasOne(v => v.Answer)
            .WithMany(a => a.Votes)
            .HasForeignKey(v => v.AnswerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
