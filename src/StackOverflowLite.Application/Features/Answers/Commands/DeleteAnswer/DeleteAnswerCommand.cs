using MediatR;
using Microsoft.EntityFrameworkCore;
using StackOverflowLite.Application.Exceptions;
using StackOverflowLite.Application.Interfaces;

namespace StackOverflowLite.Application.Features.Answers.Commands.DeleteAnswer;

public record DeleteAnswerCommand(int Id) : IRequest<Unit>;

public class DeleteAnswerCommandHandler : IRequestHandler<DeleteAnswerCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public DeleteAnswerCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Unit> Handle(DeleteAnswerCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new ForbiddenException("User is not authenticated.");

        var answer = await _context.Answers
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Answer", request.Id);

        if (answer.AuthorId != userId)
        {
            throw new ForbiddenException("You can only delete your own answers.");
        }

        if (answer.IsAccepted)
        {
            throw new BadRequestException("Accepted answers cannot be deleted.");
        }

        _context.Answers.Remove(answer);
        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
