using KPGJA.Web.Models;
using KPGJA.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KPGJA.Web.Controllers;

public class MemberController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IWebHostEnvironment _environment;

    private static readonly Dictionary<string, decimal> Fees = new()
    {
        ["Ordinary Member"] = 1000m,
        ["Associate Member"] = 2500m,
        ["Honorary Member"] = 3500m
    };

    public MemberController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IWebHostEnvironment environment)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _environment = environment;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.FindByEmailAsync(model.Username);
        if (user == null)
        {
            ModelState.AddModelError("", "Invalid email or password.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: false);

        if (result.Succeeded)
        {
            if (await _userManager.IsInRoleAsync(user, "Admin"))
                return RedirectToAction("Index", "Admin");

            return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? Redirect(returnUrl)
                : RedirectToAction(nameof(Dashboard));
        }

        ModelState.AddModelError("", "Invalid email or password.");
        return View(model);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {        
        return View();
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    private async Task<string> SaveFileAsync(IFormFile file, string category)
    {
        var relative = await SaveOptionalAsync(file, category);
        return relative!;
    }

    private async Task<string?> SaveOptionalAsync(IFormFile? file, string category)
    {
        if (file == null || file.Length == 0) return null;

        var allowed = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowed.Contains(extension))
            throw new InvalidOperationException("Invalid file type.");

        if (file.Length > 5 * 1024 * 1024)
            throw new InvalidOperationException("File size exceeds 5 MB.");

        var folder = Path.Combine(_environment.ContentRootPath, "Storage", "MemberDocuments", category);
        Directory.CreateDirectory(folder);

        var name = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(folder, name);

        await using var stream = System.IO.File.Create(fullPath);
        await file.CopyToAsync(stream);

        return Path.Combine(category, name).Replace("\\", "/");
    }
}
