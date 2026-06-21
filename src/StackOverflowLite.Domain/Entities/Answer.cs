namespace StackOverflowLite.Domain.Entities;

public class Answer
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string AuthorId { get; set; } = string.Empty;
    public ApplicationUser Author { get; set; } = null!;
    public int QuestionId { get; set; }
    public Question Question { get; set; } = null!;
    public bool IsAccepted { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    public ICollection<Vote> Votes { get; set; } = new List<Vote>();
}
