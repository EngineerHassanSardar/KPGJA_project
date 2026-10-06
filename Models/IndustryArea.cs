namespace KPGJA.Web.Models;

public class IndustryArea
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string IconClass { get; set; } = "";    // e.g. "bi-gem"
    public string ImagePath { get; set; } = "";
    public int DisplayOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}