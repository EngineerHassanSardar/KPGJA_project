using System.ComponentModel.DataAnnotations;

namespace KPGJA.Web.ViewModels;

public class LoginViewModel
{
    [Required, EmailAddress] public string Username { get; set; } = "";
    [Required] public string Password { get; set; } = "";
    public bool RememberMe { get; set; }
}
