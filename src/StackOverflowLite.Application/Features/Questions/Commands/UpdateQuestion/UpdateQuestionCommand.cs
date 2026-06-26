using MediatR;
using Microsoft.EntityFrameworkCore;
using StackOverflowLite.Application.Exceptions;
using StackOverflowLite.Application.Features.Questions.DTOs;
using StackOverflowLite.Application.Interfaces;
using StackOverflowLite.Domain.Entities;

namespace StackOverflowLite.Application.Features.Questions.Commands.UpdateQuestion;

public record UpdateQuestionCommand(int Id, string Title, string Description, List<string> Tags) : IRequest<QuestionDto>;

public class UpdateQuestionCommandHandler : IRequestHandler<UpdateQuestionCommand, QuestionDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICacheService _cacheService;

    public UpdateQuestionCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService, ICacheService cacheService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _cacheService = cacheService;
    }

    public async Task<QuestionDto> Handle(UpdateQuestionCommand request, CancellationToken cancellationToken)
    {
        var question = await _context.Questions
            .Include(q => q.Author)
            .Include(q => q.Answers)
            .Include(q => q.Votes)
            .Include(q => q.QuestionTags)
            .ThenInclude(qt => qt.Tag)
            .FirstOrDefaultAsync(q => q.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Question", request.Id);

        var userId = _currentUserService.UserId
            ?? throw new ForbiddenException("User is not authenticated.");

        if (question.AuthorId != userId)
        {
            throw new ForbiddenException("You can only edit your own questions.");
        }

        question.Title = request.Title;
        question.Description = request.Description;
        question.UpdatedAt = DateTime.UtcNow;

        // Handle Tags
        var uniqueTags = request.Tags
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim().ToLower())
            .Distinct()
            .ToList();

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

        // Remove tags that are no longer present
        var tagsToRemove = question.QuestionTags
            .Where(qt => !uniqueTags.Contains(qt.Tag.Name))
            .ToList();

        foreach (var toRemove in tagsToRemove)
        {
            question.QuestionTags.Remove(toRemove);
        }

        // Add new tags
        var existingQuestionTagNames = question.QuestionTags.Select(qt => qt.Tag.Name).ToHashSet();
        foreach (var tag in allTags)
        {
            if (!existingQuestionTagNames.Contains(tag.Name))
            {
                question.QuestionTags.Add(new QuestionTag
                {
                    Question = question,
                    Tag = tag
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _cacheService.RemoveAsync($"question_{question.Id}", cancellationToken);
        await _cacheService.RemoveByPrefixAsync("questions_page_", cancellationToken);

        return new QuestionDto(
            question.Id,
            question.Title,
            question.Description,
            question.AuthorId,
            question.Author.UserName ?? string.Empty,
            question.ViewCount,
            question.AcceptedAnswerId,
            question.Votes.Sum(v => (int)v.Type),
            question.Answers.Count,
            question.QuestionTags.Select(qt => qt.Tag.Name).ToList(),
            question.CreatedAt,
            question.UpdatedAt
        );
    }
}
