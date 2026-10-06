namespace KPGJA.Web.Models;

// The single intro + highlight block shown at top of Women page
public class WomenEmpowermentContent
{
    public int Id { get; set; }
    public string IntroTitle { get; set; } = "";
    public string IntroBody { get; set; } = "";
    public string HighlightTitle { get; set; } = "";
    public string HighlightBody { get; set; } = "";
    public string CoverImagePath { get; set; } = "";
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

// Each repeatable feature card on the Women page (list of these)
public class WomenFeature
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public string IconClass { get; set; } = "bi-star";
    public int DisplayOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}