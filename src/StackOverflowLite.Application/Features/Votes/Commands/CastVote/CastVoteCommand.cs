using MediatR;
using Microsoft.EntityFrameworkCore;
using StackOverflowLite.Application.Exceptions;
using StackOverflowLite.Application.Interfaces;
using StackOverflowLite.Domain.Entities;
using StackOverflowLite.Domain.Enums;

namespace StackOverflowLite.Application.Features.Votes.Commands.CastVote;

public record CastVoteCommand(int? QuestionId, int? AnswerId, VoteType Type) : IRequest<Unit>;

public class CastVoteCommandHandler : IRequestHandler<CastVoteCommand, Unit>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public CastVoteCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Unit> Handle(CastVoteCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new ForbiddenException("User is not authenticated.");

        var contentType = request.QuestionId.HasValue ? ContentType.Question : ContentType.Answer;
        var targetId = request.QuestionId ?? request.AnswerId!.Value;

        // 1. Fetch target entity and author to validate and apply reputation
        string targetAuthorId;
        if (contentType == ContentType.Question)
        {
            var question = await _context.Questions
                .FirstOrDefaultAsync(q => q.Id == targetId, cancellationToken)
                ?? throw new NotFoundException("Question", targetId);
            targetAuthorId = question.AuthorId;
        }
        else
        {
            var answer = await _context.Answers
                .FirstOrDefaultAsync(a => a.Id == targetId, cancellationToken)
                ?? throw new NotFoundException("Answer", targetId);
            targetAuthorId = answer.AuthorId;
        }

        // 2. Prevent self-voting
        if (targetAuthorId == userId)
        {
            throw new BadRequestException("You cannot vote on your own post.");
        }

        var targetAuthor = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == targetAuthorId, cancellationToken);
        
        if (targetAuthor == null)
        {
            throw new NotFoundException("User", targetAuthorId);
        }

        // 3. Find existing vote by this user on this content
        var existingVote = await _context.Votes
            .FirstOrDefaultAsync(v => 
                v.VoterId == userId && 
                v.ContentType == contentType &&
                (contentType == ContentType.Question ? v.QuestionId == targetId : v.AnswerId == targetId), 
                cancellationToken);

        // 4. Handle voting logic
        if (existingVote == null)
        {
            // NEW VOTE
            var newVote = new Vote
            {
                VoterId = userId,
                Type = request.Type,
                ContentType = contentType,
                QuestionId = request.QuestionId,
                AnswerId = request.AnswerId,
                CreatedAt = DateTime.UtcNow
            };
            
            _context.Votes.Add(newVote);
            ApplyReputationChange(targetAuthor, contentType, request.Type, isAdding: true);
        }
        else if (existingVote.Type == request.Type)
        {
            // REMOVE VOTE (Toggling the same vote type removes it)
            _context.Votes.Remove(existingVote);
            ApplyReputationChange(targetAuthor, contentType, existingVote.Type, isAdding: false);
        }
        else
        {
            // CHANGE VOTE
            // Revert old vote
            ApplyReputationChange(targetAuthor, contentType, existingVote.Type, isAdding: false);
            // Apply new vote
            existingVote.Type = request.Type;
            ApplyReputationChange(targetAuthor, contentType, request.Type, isAdding: true);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }

    private void ApplyReputationChange(ApplicationUser author, ContentType contentType, VoteType voteType, bool isAdding)
    {
        int points = 0;
        
        if (contentType == ContentType.Question)
        {
            points = voteType == VoteType.Upvote ? 5 : -2;
        }
        else if (contentType == ContentType.Answer)
        {
            points = voteType == VoteType.Upvote ? 10 : -2;
        }

        // If we are removing a vote, we subtract the points
        if (!isAdding)
        {
            points = -points;
        }

        author.Reputation = Math.Max(0, author.Reputation + points);
    }
}
