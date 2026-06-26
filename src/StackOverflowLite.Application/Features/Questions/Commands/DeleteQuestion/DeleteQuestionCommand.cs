using MediatR;
using Microsoft.EntityFrameworkCore;
using StackOverflowLite.Application.Exceptions;
using StackOverflowLite.Application.Interfaces;

namespace StackOverflowLite.Application.Features.Questions.Commands.DeleteQuestion;

public record DeleteQuestionCommand(int Id) : IRequest<Unit>;

public class DeleteQuestionCommandHandler : IRequestHandler<DeleteQuestionCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICacheService _cacheService;

    public DeleteQuestionCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService, ICacheService cacheService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _cacheService = cacheService;
    }

    public async Task<Unit> Handle(DeleteQuestionCommand request, CancellationToken cancellationToken)
    {
        var question = await _context.Questions
            .FirstOrDefaultAsync(q => q.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Question", request.Id);

        var userId = _currentUserService.UserId
            ?? throw new ForbiddenException("User is not authenticated.");

        if (question.AuthorId != userId)
        {
            throw new ForbiddenException("You can only delete your own questions.");
        }

        _context.Questions.Remove(question);
        await _context.SaveChangesAsync(cancellationToken);

        await _cacheService.RemoveAsync($"question_{request.Id}", cancellationToken);
        await _cacheService.RemoveByPrefixAsync("questions_page_", cancellationToken);

        return Unit.Value;
    }
}
