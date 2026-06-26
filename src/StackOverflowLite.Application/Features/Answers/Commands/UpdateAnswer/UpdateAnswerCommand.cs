using MediatR;
using Microsoft.EntityFrameworkCore;
using StackOverflowLite.Application.Exceptions;
using StackOverflowLite.Application.Features.Answers.DTOs;
using StackOverflowLite.Application.Interfaces;

namespace StackOverflowLite.Application.Features.Answers.Commands.UpdateAnswer;

public record UpdateAnswerCommand(int Id, string Content) : IRequest<AnswerDto>;

public class UpdateAnswerCommandHandler : IRequestHandler<UpdateAnswerCommand, AnswerDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICacheService _cacheService;

    public UpdateAnswerCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService, ICacheService cacheService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _cacheService = cacheService;
    }

    public async Task<AnswerDto> Handle(UpdateAnswerCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new ForbiddenException("User is not authenticated.");

        var answer = await _context.Answers
            .Include(a => a.Author)
            .Include(a => a.Votes)
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Answer", request.Id);

        if (answer.AuthorId != userId)
        {
            throw new ForbiddenException("You can only update your own answers.");
        }

        answer.Content = request.Content;
        answer.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        await _cacheService.RemoveAsync($"question_{answer.QuestionId}", cancellationToken);
        await _cacheService.RemoveByPrefixAsync("questions_page_", cancellationToken);

        return new AnswerDto(
            answer.Id,
            answer.Content,
            answer.AuthorId,
            answer.Author.UserName ?? string.Empty,
            answer.QuestionId,
            answer.IsAccepted,
            answer.Votes.Sum(v => (int)v.Type),
            answer.CreatedAt,
            answer.UpdatedAt
        );
    }
}
