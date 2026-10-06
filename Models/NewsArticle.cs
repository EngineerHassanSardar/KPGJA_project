namespace KPGJA.Web.Models;

public class NewsArticle
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Category { get; set; } = "";
    public string Summary { get; set; } = "";
    public string Content { get; set; } = "";
    public string Author { get; set; } = "Admin";
    public bool IsPublished { get; set; } = false;
    public DateTime PublishedDate { get; set; } = DateTime.UtcNow;
}