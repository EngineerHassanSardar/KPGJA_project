using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace KPGJA.Web.ViewModels;

public class RegisterViewModel
{
    [Required, MaxLength(200)] public string BusinessName { get; set; } = "";
    [Required, MaxLength(200)] public string FullName { get; set; } = "";
    [Required, MaxLength(200)] public string FatherHusbandName { get; set; } = "";
    [Required, RegularExpression(@"^\d{5}-\d{7}-\d{1}$")] public string Cnic { get; set; } = "";
    public string? Designation { get; set; }
    public int? YearsInBusiness { get; set; }
    [Required] public string NatureOfBusiness { get; set; } = "";
    [Required] public string MobileNo { get; set; } = "";
    public string? LandlineNo { get; set; }
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Url] public string? Website { get; set; }
    [Required] public string BusinessAddress { get; set; } = "";
    [Required] public string CityDistrict { get; set; } = "";
    public string? PostalCode { get; set; }

    [Required] public string MembershipCategory { get; set; } = "";
    public List<string> BusinessType { get; set; } = new();

    public string? NtnStrn { get; set; }
    public string? LicenceNo { get; set; }
    public string? AssociationMembership { get; set; }
    public string? KpRegistrationNo { get; set; }

    [Required, MinLength(6)] public string Password { get; set; } = "";
    [Required, Compare(nameof(Password))] public string ConfirmPassword { get; set; } = "";

    [Required] public IFormFile? CnicDocument { get; set; }
    [Required] public IFormFile? PassportPhoto { get; set; }
    public IFormFile? BusinessRegistrationDocument { get; set; }
    public IFormFile? FeeReceipt { get; set; }
    public IFormFile? BankDepositSlip { get; set; }
    public IFormFile? RecommendationDocument { get; set; }

    [Range(typeof(bool), "true", "true")] public bool DeclarationAgreement { get; set; }
    [Range(typeof(bool), "true", "true")] public bool FeeAgreement { get; set; }
    [Range(typeof(bool), "true", "true")] public bool UndertakingAgreement { get; set; }
    [Range(typeof(bool), "true", "true")] public bool AuthenticityAgreement { get; set; }

    [Required] public string ConfirmationFullName { get; set; } = "";
    [Required, RegularExpression(@"^\d{5}-\d{7}-\d{1}$")] public string ConfirmationCnic { get; set; } = "";
    [Required] public string Signature { get; set; } = "";
    [Range(typeof(bool), "true", "true")] public bool FinalConfirmation { get; set; }
}
