namespace StackOverflowLite.Application.Features.Answers.DTOs;

public record AnswerDto(
    int Id,
    string Content,
    string AuthorId,
    string AuthorUsername,
    int QuestionId,
    bool IsAccepted,
    int VoteScore,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
