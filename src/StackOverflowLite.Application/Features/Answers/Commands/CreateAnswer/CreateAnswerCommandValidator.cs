using FluentValidation;

namespace StackOverflowLite.Application.Features.Answers.Commands.CreateAnswer;

public class CreateAnswerCommandValidator : AbstractValidator<CreateAnswerCommand>
{
    public CreateAnswerCommandValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required.")
            .MinimumLength(15).WithMessage("Content must be at least 15 characters.");

        RuleFor(x => x.QuestionId)
            .NotEmpty().WithMessage("Question ID is required.");
    }
}
