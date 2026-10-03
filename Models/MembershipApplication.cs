using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KPGJA.Web.Models;

public class MembershipApplication
{
    public int Id { get; set; }

    [Required, MaxLength(450)]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    [Required, MaxLength(200)]
    public string BusinessName { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string FullName { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string FatherHusbandName { get; set; } = string.Empty;

    [Required, MaxLength(15)]
    public string Cnic { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? Designation { get; set; }

    public int? YearsInBusiness { get; set; }

    [Required, MaxLength(2000)]
    public string NatureOfBusiness { get; set; } = string.Empty;

    [Required, MaxLength(30)]
    public string MobileNo { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? LandlineNo { get; set; }

    [Required, MaxLength(256)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Website { get; set; }

    [Required, MaxLength(1000)]
    public string BusinessAddress { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string CityDistrict { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? PostalCode { get; set; }

    [Required, MaxLength(50)]
    public string MembershipCategory { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string BusinessTypes { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? NtnStrn { get; set; }

    [MaxLength(100)]
    public string? LicenceNo { get; set; }

    [MaxLength(300)]
    public string? AssociationMembership { get; set; }

    [MaxLength(100)]
    public string? KpRegistrationNo { get; set; }

    public decimal MembershipFee { get; set; }

    [MaxLength(100)]
    public string Status { get; set; } = "Pending Review";

    [MaxLength(100)]
    public string? MembershipNo { get; set; }

    public decimal? FeeReceived { get; set; }

    [MaxLength(200)]
    public string? PaymentReference { get; set; }

    [MaxLength(450)]
    public string? CnicDocumentPath { get; set; }

    [MaxLength(450)]
    public string? PassportPhotoPath { get; set; }

    [MaxLength(450)]
    public string? BusinessRegistrationDocumentPath { get; set; }

    [MaxLength(450)]
    public string? FeeReceiptPath { get; set; }

    [MaxLength(450)]
    public string? BankDepositSlipPath { get; set; }

    [MaxLength(450)]
    public string? RecommendationDocumentPath { get; set; }

    public bool DeclarationAccepted { get; set; }
    public bool FeeAgreementAccepted { get; set; }
    public bool UndertakingAccepted { get; set; }
    public bool AuthenticityAccepted { get; set; }
    public bool FinalConfirmation { get; set; }

    [Required, MaxLength(200)]
    public string ConfirmationFullName { get; set; } = string.Empty;

    [Required, MaxLength(15)]
    public string ConfirmationCnic { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Signature { get; set; } = string.Empty;

    public DateTime ApplicationDate { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public string? AdminRemarks { get; set; }
}
