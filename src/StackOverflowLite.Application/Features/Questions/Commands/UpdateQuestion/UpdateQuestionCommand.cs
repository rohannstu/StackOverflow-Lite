using MediatR;
using Microsoft.EntityFrameworkCore;
using StackOverflowLite.Application.Exceptions;
using StackOverflowLite.Application.Features.Questions.DTOs;
using StackOverflowLite.Application.Interfaces;

namespace StackOverflowLite.Application.Features.Questions.Commands.UpdateQuestion;

public record UpdateQuestionCommand(int Id, string Title, string Description) : IRequest<QuestionDto>;

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
            question.CreatedAt,
            question.UpdatedAt
        );
    }
}
