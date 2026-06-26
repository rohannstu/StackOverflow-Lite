using FluentValidation;

namespace StackOverflowLite.Application.Features.Votes.Commands.CastVote;

public class CastVoteCommandValidator : AbstractValidator<CastVoteCommand>
{
    public CastVoteCommandValidator()
    {
        RuleFor(v => v.Type)
            .IsInEnum().WithMessage("Invalid vote type.");

        RuleFor(x => x)
            .Must(x => (x.QuestionId.HasValue && !x.AnswerId.HasValue) || (!x.QuestionId.HasValue && x.AnswerId.HasValue))
            .WithMessage("Exactly one of QuestionId or AnswerId must be provided.");
    }
}
