using MediatR;
using Microsoft.EntityFrameworkCore;
using StackOverflowLite.Application.Exceptions;
using StackOverflowLite.Application.Interfaces;

namespace StackOverflowLite.Application.Features.Answers.Commands.UnacceptAnswer;

public record UnacceptAnswerCommand(int QuestionId) : IRequest<Unit>;

public class UnacceptAnswerCommandHandler : IRequestHandler<UnacceptAnswerCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICacheService _cacheService;

    public UnacceptAnswerCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService, ICacheService cacheService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _cacheService = cacheService;
    }

    public async Task<Unit> Handle(UnacceptAnswerCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new ForbiddenException("User is not authenticated.");

        // 1. Verify question exists
        var question = await _context.Questions
            .FirstOrDefaultAsync(q => q.Id == request.QuestionId, cancellationToken)
            ?? throw new NotFoundException("Question", request.QuestionId);

        // 2. Only the question author can unaccept
        if (question.AuthorId != userId)
        {
            throw new ForbiddenException("Only the question author can unaccept an answer.");
        }

        // 3. Check if there is an accepted answer
        if (!question.AcceptedAnswerId.HasValue)
        {
            throw new BadRequestException("This question does not have an accepted answer.");
        }

        // 4. Unaccept the answer
        var answer = await _context.Answers
            .FirstOrDefaultAsync(a => a.Id == question.AcceptedAnswerId, cancellationToken);

        if (answer != null)
        {
            answer.IsAccepted = false;

            // 5. Deduct 15 rep from the answer author (floor at 0)
            var answerAuthor = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == answer.AuthorId, cancellationToken);

            if (answerAuthor != null)
            {
                answerAuthor.Reputation = Math.Max(0, answerAuthor.Reputation - 15);
            }
        }

        // 6. Clear the accepted answer reference
        question.AcceptedAnswerId = null;

        await _context.SaveChangesAsync(cancellationToken);

        await _cacheService.RemoveAsync($"question_{request.QuestionId}", cancellationToken);
        await _cacheService.RemoveByPrefixAsync("questions_page_", cancellationToken);

        return Unit.Value;
    }
}
