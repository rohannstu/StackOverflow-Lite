using FluentValidation;

namespace StackOverflowLite.Application.Features.Questions.Commands.CreateQuestion;

public class CreateQuestionCommandValidator : AbstractValidator<CreateQuestionCommand>
{
    public CreateQuestionCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(300).WithMessage("Title must not exceed 300 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MinimumLength(20).WithMessage("Description must be at least 20 characters.");

        RuleFor(x => x.Tags)
            .NotNull()
            .Must(x => x != null && x.Count <= 5).WithMessage("A maximum of 5 tags is allowed.");

        RuleForEach(x => x.Tags)
            .NotEmpty().WithMessage("Tag name cannot be empty.")
            .MaximumLength(30).WithMessage("Tag name must not exceed 30 characters.");
    }
}
