using MediatR;
using Microsoft.EntityFrameworkCore;
using StackOverflowLite.Application.Exceptions;
using StackOverflowLite.Application.Features.Answers.DTOs;
using StackOverflowLite.Application.Interfaces;

namespace StackOverflowLite.Application.Features.Answers.Queries.GetAnswersByQuestion;

public record GetAnswersByQuestionQuery(int QuestionId) : IRequest<IReadOnlyList<AnswerDto>>;

public class GetAnswersByQuestionQueryHandler : IRequestHandler<GetAnswersByQuestionQuery, IReadOnlyList<AnswerDto>>
{
    private readonly IApplicationDbContext _context;

    public GetAnswersByQuestionQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<AnswerDto>> Handle(GetAnswersByQuestionQuery request, CancellationToken cancellationToken)
    {
        var questionExists = await _context.Questions
            .AnyAsync(q => q.Id == request.QuestionId, cancellationToken);

        if (!questionExists)
        {
            throw new NotFoundException("Question", request.QuestionId);
        }

        var answers = await _context.Answers
            .AsNoTracking()
            .Include(a => a.Author)
            .Include(a => a.Votes)
            .Where(a => a.QuestionId == request.QuestionId)
            .ToListAsync(cancellationToken);

        var dtos = answers
            .Select(a => new AnswerDto(
                a.Id,
                a.Content,
                a.AuthorId,
                a.Author.UserName ?? string.Empty,
                a.QuestionId,
                a.IsAccepted,
                a.Votes.Sum(v => (int)v.Type),
                a.CreatedAt,
                a.UpdatedAt
            ))
            .OrderByDescending(a => a.IsAccepted)
            .ThenByDescending(a => a.VoteScore)
            .ThenBy(a => a.CreatedAt)
            .ToList();

        return dtos;
    }
}
