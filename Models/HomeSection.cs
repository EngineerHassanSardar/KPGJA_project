namespace KPGJA.Web.Models;

public class HomeSection
{
    public int Id { get; set; }
    public string SectionKey { get; set; } = "";  // "objectives" or "quicklinks"
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public string IconClass { get; set; } = "";
    public int DisplayOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}