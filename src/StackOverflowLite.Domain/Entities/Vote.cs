using StackOverflowLite.Domain.Enums;

namespace StackOverflowLite.Domain.Entities;

public class Vote
{
    public int Id { get; set; }
    public string VoterId { get; set; } = string.Empty;
    public ApplicationUser Voter { get; set; } = null!;
    
    public VoteType Type { get; set; }
    public ContentType ContentType { get; set; }
    
    public int? QuestionId { get; set; }
    public Question? Question { get; set; }
    
    public int? AnswerId { get; set; }
    public Answer? Answer { get; set; }
    
    public DateTime CreatedAt { get; set; }
}
