namespace StackOverflowLite.Application.Features.Questions.DTOs;

public record QuestionDto(
    int Id,
    string Title,
    string Description,
    string AuthorId,
    string AuthorUsername,
    int ViewCount,
    int? AcceptedAnswerId,
    int VoteScore,
    int AnswerCount,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
