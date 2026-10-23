namespace project.Models;

public class CommentDto
{
    public int Id { get; set; }
    public int RequestId { get; set; }
    public int UserId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateCommentDto
{
    public string Text { get; set; } = string.Empty;
}

public class UpdateCommentDto
{
    public string Text { get; set; } = string.Empty;
}