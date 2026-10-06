namespace KPGJA.Web.Models;

public class ContactInquiry
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; } = false;
    public bool IsReplied { get; set; } = false;
}