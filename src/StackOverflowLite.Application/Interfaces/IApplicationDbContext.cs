using Microsoft.EntityFrameworkCore;
using StackOverflowLite.Domain.Entities;

namespace StackOverflowLite.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<ApplicationUser> Users { get; }
    DbSet<Question> Questions { get; }
    DbSet<Answer> Answers { get; }
    DbSet<Tag> Tags { get; }
    DbSet<QuestionTag> QuestionTags { get; }
    DbSet<Vote> Votes { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
