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
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        // ---------- 1. Create the user account ----------
        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            FullName = model.FullName,
            PhoneNumber = model.MobileNo,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);
            return View(model);
        }

        // ---------- 2. Save uploaded files ----------
        var cnicPath = await SaveOptionalAsync(model.CnicDocument, "cnic");
        var photoPath = await SaveOptionalAsync(model.PassportPhoto, "photo");
        var businessPath = await SaveOptionalAsync(model.BusinessRegistrationDocument, "business");
        var feeReceiptPath = await SaveOptionalAsync(model.FeeReceipt, "fee");
        var bankSlipPath = await SaveOptionalAsync(model.BankDepositSlip, "bank");
        var recommendPath = await SaveOptionalAsync(model.RecommendationDocument, "recommendation");

        // ---------- 3. Create the MembershipApplication record ----------
        // NOTE: This is temporary — only saves in-memory for now.
        // Once we add DbContext + migrations in Phase 2, this will save to SQL Server.

        var application = new MembershipApplication
        {
            FullName = model.FullName,
            FatherHusbandName = model.FatherHusbandName,
            Cnic = model.Cnic,
            BusinessName = model.BusinessName,
            Designation = model.Designation,
            NatureOfBusiness = model.NatureOfBusiness,
            MobileNo = model.MobileNo,
            Email = model.Email,
            CityDistrict = model.CityDistrict,
            BusinessAddress = model.BusinessAddress,
            MembershipCategory = model.MembershipCategory,
            BusinessTypes = string.Join(", ", model.BusinessType),
            MembershipFee = Fees.TryGetValue(model.MembershipCategory, out var fee) ? fee : 0m,
            Status = "Pending Review",
            ApplicationDate = DateTime.UtcNow,

            // Document paths
            CnicDocumentPath = cnicPath,
            PassportPhotoPath = photoPath,
            BusinessRegistrationDocumentPath = businessPath,
            FeeReceiptPath = feeReceiptPath,
            BankDepositSlipPath = bankSlipPath,
            RecommendationDocumentPath = recommendPath
        };

        // ---------- 4. Sign the user in ----------
        await _signInManager.SignInAsync(user, isPersistent: false);

        // ---------- 5. Show success and redirect to Dashboard ----------
        TempData["Success"] = $"Thank you {model.FullName}. Your application has been submitted.";
        return RedirectToAction(nameof(Dashboard));
    }


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
