namespace StackOverflowLite.Domain.Entities;

public class QuestionTag
{
    public int QuestionId { get; set; }
    public Question Question { get; set; } = null!;
    
    public int TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}
