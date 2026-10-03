using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace KPGJA.Web.Models;

public class ApplicationUser : IdentityUser
{
    [MaxLength(200)]
    public string? FullName { get; set; }
}
