namespace EventManageApp.Models;

public class TaskSubmission
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public int UserId { get; set; }
    public string? Answer { get; set; }
    public byte[]? SubmissionFile { get; set; }
    public string? FileName { get; set; }
    public string? FileContentType { get; set; }
    public string? CloudinaryUrl { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.Now;
    public bool IsApproved { get; set; } = false;
    public bool IsRejected { get; set; } = false;
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovalNotes { get; set; }
    public string? RejectionReason { get; set; }

    // Navigation properties
    public User? User { get; set; }
    public Task? Task { get; set; }
}
