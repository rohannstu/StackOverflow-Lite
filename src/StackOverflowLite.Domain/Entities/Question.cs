namespace StackOverflowLite.Domain.Entities;

public class Question
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AuthorId { get; set; } = string.Empty;
    public ApplicationUser Author { get; set; } = null!;
    public int ViewCount { get; set; } = 0;
    public int? AcceptedAnswerId { get; set; }
    public Answer? AcceptedAnswer { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    public ICollection<Answer> Answers { get; set; } = new List<Answer>();
    public ICollection<QuestionTag> QuestionTags { get; set; } = new List<QuestionTag>();
    public ICollection<Vote> Votes { get; set; } = new List<Vote>();
}
