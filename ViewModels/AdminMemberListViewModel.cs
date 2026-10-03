using KPGJA.Web.Models;

namespace KPGJA.Web.ViewModels;

public class AdminMemberListViewModel
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public List<MembershipApplication> Members { get; set; } = new();
}
