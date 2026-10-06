namespace KPGJA.Web.Models;

public class CommitteeMember
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Designation { get; set; } = "";   // e.g. President, Vice President
    public string Bio { get; set; } = "";
    public string PhotoPath { get; set; } = "";
    public int DisplayOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}