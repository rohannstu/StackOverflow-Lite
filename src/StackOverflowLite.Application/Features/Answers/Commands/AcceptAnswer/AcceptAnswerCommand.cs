using MediatR;
using Microsoft.EntityFrameworkCore;
using StackOverflowLite.Application.Exceptions;
using StackOverflowLite.Application.Interfaces;

namespace StackOverflowLite.Application.Features.Answers.Commands.AcceptAnswer;

public record AcceptAnswerCommand(int QuestionId, int AnswerId) : IRequest<Unit>;

public class AcceptAnswerCommandHandler : IRequestHandler<AcceptAnswerCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public AcceptAnswerCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Unit> Handle(AcceptAnswerCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new ForbiddenException("User is not authenticated.");

        // 1. Verify question exists and load it
        var question = await _context.Questions
            .FirstOrDefaultAsync(q => q.Id == request.QuestionId, cancellationToken)
            ?? throw new NotFoundException("Question", request.QuestionId);

        // 2. Only the question author can accept an answer
        if (question.AuthorId != userId)
        {
            throw new ForbiddenException("Only the question author can accept an answer.");
        }

        // 3. Verify answer exists and belongs to this question
        var answer = await _context.Answers
            .FirstOrDefaultAsync(a => a.Id == request.AnswerId, cancellationToken)
            ?? throw new NotFoundException("Answer", request.AnswerId);

        if (answer.QuestionId != request.QuestionId)
        {
            throw new BadRequestException("This answer does not belong to the specified question.");
        }

        // 4. Block self-accept: question author cannot accept their own answer
        if (answer.AuthorId == question.AuthorId)
        {
            throw new BadRequestException("You cannot accept your own answer.");
        }

        // 5. If there is already an accepted answer, unaccept it first and deduct reputation
        if (question.AcceptedAnswerId.HasValue && question.AcceptedAnswerId != request.AnswerId)
        {
            var previousAnswer = await _context.Answers
                .FirstOrDefaultAsync(a => a.Id == question.AcceptedAnswerId, cancellationToken);

            if (previousAnswer != null)
            {
                previousAnswer.IsAccepted = false;

                // Deduct 15 rep from the previous accepted answer's author (floor at 0)
                var previousAuthor = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == previousAnswer.AuthorId, cancellationToken);

                if (previousAuthor != null)
                {
                    previousAuthor.Reputation = Math.Max(0, previousAuthor.Reputation - 15);
                }
            }
        }

        // 6. If re-accepting the same answer, no-op
        if (question.AcceptedAnswerId == request.AnswerId)
        {
            return Unit.Value;
        }

        // 7. Accept the new answer
        answer.IsAccepted = true;
        question.AcceptedAnswerId = request.AnswerId;

        // 8. Award 15 rep to the accepted answer's author
        var answerAuthor = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == answer.AuthorId, cancellationToken);

        if (answerAuthor != null)
        {
            answerAuthor.Reputation += 15;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
