namespace StackOverflowLite.Application.Features.Questions.DTOs;

public record UpdateQuestionDto(string Title, string Description, List<string> Tags);
