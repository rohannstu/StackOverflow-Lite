using FluentValidation;

namespace StackOverflowLite.Application.Features.Answers.Commands.UpdateAnswer;

public class UpdateAnswerCommandValidator : AbstractValidator<UpdateAnswerCommand>
{
    public UpdateAnswerCommandValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required.")
            .MinimumLength(15).WithMessage("Content must be at least 15 characters.");
    }
}
