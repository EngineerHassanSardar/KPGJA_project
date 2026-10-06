namespace KPGJA.Web.Models;

// The single intro block shown at top of About Us page
public class AboutContent
{
    public int Id { get; set; }
    public string SectionTitle { get; set; } = "";
    public string MissionStatement { get; set; } = "";
    public string VisionStatement { get; set; } = "";
    public string CoverImagePath { get; set; } = "";
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

// Each repeatable section on the About page (list of these)
public class AboutSection
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public string IconClass { get; set; } = "bi-info-circle";
    public int DisplayOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}