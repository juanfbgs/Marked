namespace Marked.Domain;

public class Bookmark : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;

    public string? ImageUrl { get; set; } 

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

}