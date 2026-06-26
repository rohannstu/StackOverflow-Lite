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
    private readonly ICacheService _cacheService;

    public GetQuestionByIdQueryHandler(IApplicationDbContext context, ICacheService cacheService)
    {
        _context = context;
        _cacheService = cacheService;
    }

    public async Task<QuestionDto> Handle(GetQuestionByIdQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"question_{request.Id}";
        var cachedDto = await _cacheService.GetAsync<QuestionDto>(cacheKey, cancellationToken);
        
        // Increment view count in background/fire-and-forget style to avoid blocking the fast cache response,
        // but since we don't have a background queue, we'll just increment it directly.
        var tracked = await _context.Questions.FindAsync(new object[] { request.Id }, cancellationToken);
        if (tracked != null)
        {
            tracked.ViewCount++;
            await _context.SaveChangesAsync(cancellationToken);
        }

        if (cachedDto != null)
        {
            // Update the view count on the cached DTO to reflect the new state, but don't re-cache just for view count
            return cachedDto with { ViewCount = tracked?.ViewCount ?? cachedDto.ViewCount };
        }

        var question = await _context.Questions
            .AsNoTracking()
            .Include(q => q.Author)
            .Include(q => q.Answers)
            .Include(q => q.Votes)
            .FirstOrDefaultAsync(q => q.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException("Question", request.Id);

        var dto = new QuestionDto(
            question.Id,
            question.Title,
            question.Description,
            question.AuthorId,
            question.Author.UserName ?? string.Empty,
            question.ViewCount,
            question.AcceptedAnswerId,
            question.Votes.Sum(v => (int)v.Type),
            question.Answers.Count,
            question.CreatedAt,
            question.UpdatedAt
        );

        // Cache for 10 minutes
        await _cacheService.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(10), cancellationToken);

        return dto;
    }
}
