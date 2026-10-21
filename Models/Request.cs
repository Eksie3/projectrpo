namespace project.Models;

public class Request
{
    public int Id { get; set; }
    public int AuthorId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime Deadline { get; set; }
}




