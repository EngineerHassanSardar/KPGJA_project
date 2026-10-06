namespace KPGJA.Web.Models;

public class EventItem
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Location { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime StartDate { get; set; } = DateTime.Today;
    public DateTime? EndDate { get; set; }
    public bool IsPublished { get; set; } = true;
    public string CoverImagePath { get; set; } = "";
}