using MediatR;
using Microsoft.EntityFrameworkCore;
using StackOverflowLite.Application.Features.Questions.DTOs;
using StackOverflowLite.Application.Interfaces;
using StackOverflowLite.Domain.Entities;

namespace StackOverflowLite.Application.Features.Questions.Commands.CreateQuestion;

public record CreateQuestionCommand(string Title, string Description) : IRequest<QuestionDto>;

public class CreateQuestionCommandHandler : IRequestHandler<CreateQuestionCommand, QuestionDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateQuestionCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
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

        _context.Questions.Add(question);
        await _context.SaveChangesAsync(cancellationToken);

        // Reload with author navigation
        var created = await _context.Questions
            .Include(q => q.Author)
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
            created.CreatedAt,
            created.UpdatedAt
        );
    }
}
