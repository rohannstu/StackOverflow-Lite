using MediatR;
using Microsoft.EntityFrameworkCore;
using StackOverflowLite.Application.Features.Questions.DTOs;
using StackOverflowLite.Application.Interfaces;

namespace StackOverflowLite.Application.Features.Questions.Queries.GetQuestions;

public record GetQuestionsQuery(int Page = 1, int PageSize = 20) : IRequest<PagedResult<QuestionDto>>;

public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNext => Page < TotalPages;
    public bool HasPrevious => Page > 1;
}

public class GetQuestionsQueryHandler : IRequestHandler<GetQuestionsQuery, PagedResult<QuestionDto>>
{
    private readonly IApplicationDbContext _context;

    public GetQuestionsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<QuestionDto>> Handle(GetQuestionsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 50);

        var query = _context.Questions
            .AsNoTracking()
            .Include(q => q.Author)
            .Include(q => q.Answers)
            .Include(q => q.Votes)
            .OrderByDescending(q => q.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(q => new QuestionDto(
                q.Id,
                q.Title,
                q.Description,
                q.AuthorId,
                q.Author.UserName ?? string.Empty,
                q.ViewCount,
                q.AcceptedAnswerId,
                q.Votes.Sum(v => (int)v.Type),
                q.Answers.Count,
                q.CreatedAt,
                q.UpdatedAt
            ))
            .ToListAsync(cancellationToken);

        return new PagedResult<QuestionDto>(items, totalCount, page, pageSize);
    }
}
