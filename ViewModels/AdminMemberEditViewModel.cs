using System.ComponentModel.DataAnnotations;

namespace KPGJA.Web.ViewModels;

public class AdminMemberEditViewModel
{
    public int Id { get; set; }

    [Required] public string Status { get; set; } = "Pending Review";
    public string? MembershipNo { get; set; }
    public decimal? FeeReceived { get; set; }
    public string? PaymentReference { get; set; }
    public string? AdminRemarks { get; set; }
}
