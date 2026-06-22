using MediatR;
using Microsoft.EntityFrameworkCore;
using StackOverflowLite.Application.Exceptions;
using StackOverflowLite.Application.Features.Questions.DTOs;
using StackOverflowLite.Application.Interfaces;

namespace StackOverflowLite.Application.Features.Questions.Queries.GetQuestionById;

public record GetQuestionByIdQuery(int Id) : IRequest<QuestionDto>;

public class GetQuestionByIdQueryHandler : IRequestHandler<GetQuestionByIdQuery, QuestionDto>
{
    private readonly IApplicationDbContext _context;

    public GetQuestionByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<QuestionDto> Handle(GetQuestionByIdQuery request, CancellationToken cancellationToken)
    {
        var question = await _context.Questions
            .AsNoTracking()
            .Include(q => q.Author)
            .Include(q => q.Answers)
            .Include(q => q.Votes)
            .FirstOrDefaultAsync(q => q.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Question", request.Id);

        // Increment view count (separate tracked query for the update)
        var tracked = await _context.Questions.FindAsync(new object[] { request.Id }, cancellationToken);
        if (tracked != null)
        {
            tracked.ViewCount++;
            await _context.SaveChangesAsync(cancellationToken);
        }

        return new QuestionDto(
            question.Id,
            question.Title,
            question.Description,
            question.AuthorId,
            question.Author.UserName ?? string.Empty,
            question.ViewCount + 1, // reflect the increment
            question.AcceptedAnswerId,
            question.Votes.Sum(v => (int)v.Type),
            question.Answers.Count,
            question.CreatedAt,
            question.UpdatedAt
        );
    }
}
