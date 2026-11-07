namespace project.Models;

public class RequestHistory
{
    public int Id { get; set; }
    public int RequestId { get; set; }
    public string? OldStatus { get; set; }
    public string? NewStatus { get; set; }
    public int ChangedBy { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? Comment { get; set; }
    public int? AssigneeId { get; set; }
}
public class ChangeStatusDto
{
    public string NewStatus { get; set; } = string.Empty;
    public int ChangedBy { get; set; }
    public string? Comment { get; set; }
}
public class AssignUserDto
{
    public int UserId { get; set; }
    public string? Comment { get; set; }
}
