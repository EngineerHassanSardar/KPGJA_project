namespace KPGJA.Web.Models;

public class HeroSlide
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Subtitle { get; set; } = "";
    public string CtaText { get; set; } = "";
    public string CtaLink { get; set; } = "";
    public string ImagePath { get; set; } = "";
    public int DisplayOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}