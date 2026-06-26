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
    private readonly ICacheService _cacheService;

    public GetQuestionsQueryHandler(IApplicationDbContext context, ICacheService cacheService)
    {
        _context = context;
        _cacheService = cacheService;
    }

    public async Task<PagedResult<QuestionDto>> Handle(GetQuestionsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 50);

        var cacheKey = $"questions_page_{page}_size_{pageSize}";
        var cachedResult = await _cacheService.GetAsync<PagedResult<QuestionDto>>(cacheKey, cancellationToken);
        
        if (cachedResult != null)
        {
            return cachedResult;
        }

        var query = _context.Questions
            .AsNoTracking()
            .Include(q => q.Author)
            .Include(q => q.Answers)
            .Include(q => q.Votes)
            .Include(q => q.QuestionTags)
            .ThenInclude(qt => qt.Tag)
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
                q.QuestionTags.Select(qt => qt.Tag.Name).ToList(),
                q.CreatedAt,
                q.UpdatedAt
            ))
            .ToListAsync(cancellationToken);

        var result = new PagedResult<QuestionDto>(items, totalCount, page, pageSize);

        // Cache for 5 minutes
        await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5), cancellationToken);

        return result;
    }
}
