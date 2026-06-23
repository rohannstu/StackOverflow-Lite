using MediatR;
using Microsoft.EntityFrameworkCore;
using StackOverflowLite.Application.Exceptions;
using StackOverflowLite.Application.Features.Answers.DTOs;
using StackOverflowLite.Application.Interfaces;
using StackOverflowLite.Domain.Entities;

namespace StackOverflowLite.Application.Features.Answers.Commands.CreateAnswer;

public record CreateAnswerCommand(int QuestionId, string Content) : IRequest<AnswerDto>;

public class CreateAnswerCommandHandler : IRequestHandler<CreateAnswerCommand, AnswerDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CreateAnswerCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<AnswerDto> Handle(CreateAnswerCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new ForbiddenException("User is not authenticated.");

        var questionExists = await _context.Questions
            .AnyAsync(q => q.Id == request.QuestionId, cancellationToken);

        if (!questionExists)
        {
            throw new NotFoundException("Question", request.QuestionId);
        }

        var answer = new Answer
        {
            QuestionId = request.QuestionId,
            Content = request.Content,
            AuthorId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Answers.Add(answer);
        await _context.SaveChangesAsync(cancellationToken);

        // Reload with Author navigation
        var created = await _context.Answers
            .Include(a => a.Author)
            .FirstAsync(a => a.Id == answer.Id, cancellationToken);

        return new AnswerDto(
            created.Id,
            created.Content,
            created.AuthorId,
            created.Author.UserName ?? string.Empty,
            created.QuestionId,
            created.IsAccepted,
            0,
            created.CreatedAt,
            created.UpdatedAt
        );
    }
}
