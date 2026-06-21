namespace StackOverflowLite.Domain.Entities;

public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // unique
    
    public ICollection<QuestionTag> QuestionTags { get; set; } = new List<QuestionTag>();
}
