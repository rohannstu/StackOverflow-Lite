using MediatR;
using Microsoft.EntityFrameworkCore;
using StackOverflowLite.Application.Features.Questions.DTOs;
using StackOverflowLite.Application.Interfaces;
using StackOverflowLite.Domain.Entities;

namespace StackOverflowLite.Application.Features.Questions.Commands.CreateQuestion;

public record CreateQuestionCommand(string Title, string Description, List<string> Tags) : IRequest<QuestionDto>;

public class CreateQuestionCommandHandler : IRequestHandler<CreateQuestionCommand, QuestionDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICacheService _cacheService;

    public CreateQuestionCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService, ICacheService cacheService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _cacheService = cacheService;
    }

    public async Task<QuestionDto> Handle(CreateQuestionCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new Exceptions.ForbiddenException("User is not authenticated.");

        var question = new Question
        {
            Title = request.Title,
            Description = request.Description,
            AuthorId = userId,
            CreatedAt = DateTime.UtcNow
        };

        // Handle Tags
        var uniqueTags = request.Tags
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim().ToLower())
            .Distinct()
            .ToList();

        if (uniqueTags.Any())
        {
            var existingTags = await _context.Tags
                .Where(t => uniqueTags.Contains(t.Name))
                .ToListAsync(cancellationToken);

            var existingTagNames = existingTags.Select(t => t.Name).ToHashSet();

            var newTags = uniqueTags
                .Where(t => !existingTagNames.Contains(t))
                .Select(t => new Tag { Name = t })
                .ToList();

            if (newTags.Any())
            {
                _context.Tags.AddRange(newTags);
            }

            var allTags = existingTags.Concat(newTags).ToList();

            foreach (var tag in allTags)
            {
                question.QuestionTags.Add(new QuestionTag
                {
                    Question = question,
                    Tag = tag
                });
            }
        }

        _context.Questions.Add(question);
        await _context.SaveChangesAsync(cancellationToken);

        // Invalidate paginated queries
        await _cacheService.RemoveByPrefixAsync("questions_page_", cancellationToken);

        // Reload with author navigation
        var created = await _context.Questions
            .Include(q => q.Author)
            .Include(q => q.QuestionTags)
            .ThenInclude(qt => qt.Tag)
            .FirstAsync(q => q.Id == question.Id, cancellationToken);

        return new QuestionDto(
            created.Id,
            created.Title,
            created.Description,
            created.AuthorId,
            created.Author.UserName ?? string.Empty,
            created.ViewCount,
            created.AcceptedAnswerId,
            0, // VoteScore — fresh question
            0, // AnswerCount
            created.QuestionTags.Select(qt => qt.Tag.Name).ToList(),
            created.CreatedAt,
            created.UpdatedAt
        );
    }
}
