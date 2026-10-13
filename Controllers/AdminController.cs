using Entities.Entities;
using KPGJA.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KPGJA.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly Core.Data.ApplicationDbContext _db;
    private readonly UserManager<Core.Data.ApplicationUser> _userManager;
    private readonly SignInManager<Core.Data.ApplicationUser> _signInManager;
    private readonly KPGJA.Web.Services.ActivityLogger _logger;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AdminController(
        Core.Data.ApplicationDbContext db,
        UserManager<Core.Data.ApplicationUser> userManager,
        SignInManager<Core.Data.ApplicationUser> signInManager,
        KPGJA.Web.Services.ActivityLogger logger,
        RoleManager<IdentityRole> roleManager)
    {
        _db = db;
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
        _roleManager = roleManager;
    }

    // Allow login + logout without admin auth
    public override void OnActionExecuting(Microsoft.AspNetCore.Mvc.Filters.ActionExecutingContext context)
    {
        var actionName = context.RouteData.Values["action"]?.ToString();
        if (actionName == "Login" || actionName == "Logout")
        {
            base.OnActionExecuting(context);
            return;
        }

        if (User.Identity?.IsAuthenticated != true)
        {
            context.Result = RedirectToAction("Login", "Admin");
            return;
        }

        if (!User.IsInRole("Admin"))
        {
            context.Result = RedirectToAction("AccessDenied", "Member");
            return;
        }

        base.OnActionExecuting(context);
    }

    // ==================== ADMIN LOGIN ====================
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true && User.IsInRole("Admin"))
            return RedirectToAction(nameof(Index));

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [AllowAnonymous]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.FindByEmailAsync(model.Username);
        if (user == null)
        {
            ModelState.AddModelError("", "Invalid email or password.");
            return View(model);
        }

        if (!user.IsActive)
        {
            ModelState.AddModelError("", "Your account has been deactivated. Contact admin.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: false);

        if (result.Succeeded)
        {
            await _logger.LogAsync("LoggedIn", "Admin", 0, "Admin signed in");

            return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
                ? Redirect(returnUrl)
                : RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError("", "Invalid email or password.");
        return View(model);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _logger.LogAsync("LoggedOut", "Admin", 0, "Admin signed out");
        await _signInManager.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    // ==================== DASHBOARD ====================
    public IActionResult Index()
    {
        var apps = GetMockApplications();
        ViewBag.TotalApplications = apps.Count;
        ViewBag.Pending = apps.Count(a => a.Status == "Pending Review");
        ViewBag.ApprovedForPayment = apps.Count(a => a.Status == "Approved for Payment" || a.Status == "Payment Submitted");
        ViewBag.TotalMembers = apps.Count(a => a.Status == "Active Member" || a.Status == "Approved");

        var recentActivity = _db.ActivityLogEntries
            .OrderByDescending(l => l.Timestamp)
            .Take(10)
            .ToList();

        ViewBag.RecentActivity = recentActivity;

        return View();
    }

    // ==================== MEMBERSHIP ====================
    public IActionResult Members(string? search, string? status)
    {
        var apps = GetMockApplications();
        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.ToLower();
            apps = apps.Where(a =>
                a.FullName.ToLower().Contains(search) ||
                a.BusinessName.ToLower().Contains(search) ||
                a.Email.ToLower().Contains(search) ||
                a.Cnic.Contains(search)).ToList();
        }
        if (!string.IsNullOrWhiteSpace(status) && status != "All Statuses")
            apps = apps.Where(a => a.Status == status).ToList();

        return View(new AdminMemberListViewModel { Members = apps, Search = search, Status = status });
    }

    public IActionResult Details(int id)
    {
        var app = GetMockApplications().FirstOrDefault(a => a.Id == id);
        if (app == null) return RedirectToAction(nameof(Members));
        return View(app);
    }

    public IActionResult DownloadDocument(int id, string type)
    {
        TempData["Success"] = $"Download requested for '{type}' (preview only).";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult Update(int id)
    {
        TempData["Success"] = $"Application #{id} updated (preview only).";
        return RedirectToAction(nameof(Details), new { id });
    }

    public IActionResult ApprovedMembers() =>
        View(GetMockApplications().Where(a => a.Status == "Active Member" || a.Status == "Approved").ToList());

    public IActionResult Payments() =>
        View(GetMockApplications().Where(a => a.Status == "Approved for Payment"
            || a.Status == "Payment Submitted" || a.Status == "Payment Verified").ToList());

    // ==================== MEMBERSHIP FEES ====================
    public IActionResult MembershipFees(bool showForm = false, int? id = null)
    {
        ViewBag.ShowForm = showForm;
        ViewBag.CurrentFee = id.HasValue
            ? _db.MembershipFees.AsNoTracking().FirstOrDefault(f => f.Id == id.Value)
            : null;

        var fees = _db.MembershipFees.AsNoTracking()
            .OrderBy(f => f.DisplayOrder)
            .ToList();

        return View(fees);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MembershipFeeSave(MembershipFee model)
    {
        model.Code = (model.Code ?? "").Trim().ToUpperInvariant();
        model.Category = (model.Category ?? "").Trim();

        bool codeTaken = _db.MembershipFees.Any(f => f.Code == model.Code && f.Id != model.Id);
        if (codeTaken)
        {
            TempData["Error"] = $"Code \"{model.Code}\" is already in use.";
            return RedirectToAction(nameof(MembershipFees));
        }

        string action;
        if (model.Id > 0)
        {
            var existing = _db.MembershipFees.FirstOrDefault(f => f.Id == model.Id);
            if (existing == null)
            {
                TempData["Error"] = "Fee not found.";
                return RedirectToAction(nameof(MembershipFees));
            }

            existing.Code = model.Code;
            existing.Category = model.Category;
            existing.AdmissionFee = model.AdmissionFee;
            existing.Benefits = model.Benefits;
            existing.Note = model.Note;
            existing.DisplayOrder = model.DisplayOrder;
            existing.IsActive = model.IsActive;

            TempData["Success"] = $"\"{model.Category}\" updated.";
            action = "Updated";
        }
        else
        {
            _db.MembershipFees.Add(model);
            TempData["Success"] = $"\"{model.Category}\" added.";
            action = "Created";
        }

        _db.SaveChanges();

        await _logger.LogAsync(action, "MembershipFee", model.Id,
            $"Category: {model.Category}, Fee: {model.AdmissionFee}");

        return RedirectToAction(nameof(MembershipFees));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MembershipFeeDelete(int id)
    {
        var fee = _db.MembershipFees.Find(id);
        if (fee != null)
        {
            _db.MembershipFees.Remove(fee);
            _db.SaveChanges();
            TempData["Success"] = $"\"{fee.Category}\" removed.";
            await _logger.LogAsync("Deleted", "MembershipFee", id, $"Category: {fee.Category}");
        }
        return RedirectToAction(nameof(MembershipFees));
    }
    // ==================== HOME CONTENT ====================
    public IActionResult HomeContent(
        bool showSlideForm = false,
        bool showObjectiveForm = false,
        bool showQuickLinkForm = false,
        bool showCtaForm = false,
        int? id = null)
    {
        ViewBag.ShowSlideForm = showSlideForm;
        ViewBag.ShowObjectiveForm = showObjectiveForm;
        ViewBag.ShowQuickLinkForm = showQuickLinkForm;
        ViewBag.ShowCtaForm = showCtaForm;

        var slides = _db.HeroSlides.AsNoTracking().OrderBy(s => s.DisplayOrder).ToList();
        var objectives = _db.HomeSections.AsNoTracking()
            .Where(s => s.SectionKey == "objectives").OrderBy(s => s.DisplayOrder).ToList();
        var quickLinks = _db.HomeSections.AsNoTracking()
            .Where(s => s.SectionKey == "quicklinks").OrderBy(s => s.DisplayOrder).ToList();

        ViewBag.Slides = slides;
        ViewBag.Objectives = objectives;
        ViewBag.QuickLinks = quickLinks;

        ViewBag.CurrentSlide = id.HasValue && showSlideForm
            ? slides.FirstOrDefault(s => s.Id == id.Value) : null;

        ViewBag.CurrentObjective = id.HasValue && showObjectiveForm
            ? objectives.FirstOrDefault(o => o.Id == id.Value) : null;

        ViewBag.CurrentQuickLink = id.HasValue && showQuickLinkForm
            ? quickLinks.FirstOrDefault(q => q.Id == id.Value) : null;

        ViewBag.Cta = _db.HomeCtas.AsNoTracking().FirstOrDefault() ?? new HomeCta();
        ViewBag.Blocks = _db.HomeBlocks.AsNoTracking().ToList();

        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> HomeSlideSave(HeroSlide model, IFormFile? ImageFile)
    {
        if (ImageFile != null && ImageFile.Length > 0)
        {
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(ImageFile.FileName).ToLowerInvariant();
            if (allowed.Contains(ext))
            {
                var folder = Path.Combine("wwwroot", "assets", "img", "slider");
                Directory.CreateDirectory(folder);
                var fileName = $"{Guid.NewGuid():N}{ext}";
                using (var stream = System.IO.File.Create(Path.Combine(folder, fileName)))
                    ImageFile.CopyTo(stream);
                model.ImagePath = $"/assets/img/slider/{fileName}";
            }
        }
        else if (model.Id > 0)
        {
            var existing = _db.HeroSlides.AsNoTracking().FirstOrDefault(s => s.Id == model.Id);
            if (existing != null && string.IsNullOrEmpty(model.ImagePath))
                model.ImagePath = existing.ImagePath;
        }

        string action;
        if (model.Id > 0)
        {
            _db.HeroSlides.Update(model);
            TempData["Success"] = $"Slide \"{model.Title}\" updated.";
            action = "Updated";
        }
        else
        {
            _db.HeroSlides.Add(model);
            TempData["Success"] = $"Slide \"{model.Title}\" added.";
            action = "Created";
        }
        _db.SaveChanges();

        await _logger.LogAsync(action, "HeroSlide", model.Id, $"Title: {model.Title}");
        return RedirectToAction(nameof(HomeContent));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> HomeSlideDelete(int id)
    {
        var s = _db.HeroSlides.Find(id);
        if (s != null)
        {
            _db.HeroSlides.Remove(s);
            _db.SaveChanges();
            await _logger.LogAsync("Deleted", "HeroSlide", id, $"Title: {s.Title}");
        }
        TempData["Success"] = "Slide deleted.";
        return RedirectToAction(nameof(HomeContent));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> HomeObjectiveSave(HomeSection model, IFormFile? ImageFile)
    {
        model.SectionKey = "objectives";

        try
        {
            if (ImageFile != null && ImageFile.Length > 0)
            {
                var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var ext = Path.GetExtension(ImageFile.FileName).ToLowerInvariant();
                if (allowed.Contains(ext))
                {
                    var folder = Path.Combine("wwwroot", "assets", "img", "home");
                    Directory.CreateDirectory(folder);
                    var fileName = $"{Guid.NewGuid():N}{ext}";
                    using (var stream = System.IO.File.Create(Path.Combine(folder, fileName)))
                        ImageFile.CopyTo(stream);
                    model.ImagePath = $"/assets/img/home/{fileName}";
                }
            }
            else if (model.Id > 0)
            {
                var existing = _db.HomeSections.AsNoTracking().FirstOrDefault(s => s.Id == model.Id);
                if (existing != null && string.IsNullOrEmpty(model.ImagePath))
                    model.ImagePath = existing.ImagePath;
            }

            string action;
            if (model.Id > 0)
            {
                _db.HomeSections.Update(model);
                TempData["Success"] = $"Objective \"{model.Title}\" updated.";
                action = "Updated";
            }
            else
            {
                _db.HomeSections.Add(model);
                TempData["Success"] = $"Objective \"{model.Title}\" added.";
                action = "Created";
            }
            _db.SaveChanges();

            await _logger.LogAsync(action, "HomeSection", model.Id, $"Title: {model.Title}");
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Save failed: {ex.InnerException?.Message ?? ex.Message}";
        }

        return RedirectToAction(nameof(HomeContent));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> HomeObjectiveDelete(int id)
    {
        var s = _db.HomeSections.Find(id);
        if (s != null)
        {
            _db.HomeSections.Remove(s);
            _db.SaveChanges();
            await _logger.LogAsync("Deleted", "HomeSection", id, $"Title: {s.Title}");
        }
        TempData["Success"] = "Objective deleted.";
        return RedirectToAction(nameof(HomeContent));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> HomeQuickLinkSave(HomeSection model)
    {
        model.SectionKey = "quicklinks";

        string action;
        if (model.Id > 0)
        {
            _db.HomeSections.Update(model);
            TempData["Success"] = $"Quick link \"{model.Title}\" updated.";
            action = "Updated";
        }
        else
        {
            _db.HomeSections.Add(model);
            TempData["Success"] = $"Quick link \"{model.Title}\" added.";
            action = "Created";
        }
        _db.SaveChanges();

        await _logger.LogAsync(action, "QuickLink", model.Id, $"Title: {model.Title}");
        return RedirectToAction(nameof(HomeContent));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> HomeQuickLinkDelete(int id)
    {
        var s = _db.HomeSections.Find(id);
        if (s != null)
        {
            _db.HomeSections.Remove(s);
            _db.SaveChanges();
            await _logger.LogAsync("Deleted", "QuickLink", id, $"Title: {s.Title}");
        }
        TempData["Success"] = "Quick link deleted.";
        return RedirectToAction(nameof(HomeContent));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> HomeCtaSave(HomeCta model)
    {
        var existing = _db.HomeCtas.FirstOrDefault();
        if (existing == null)
        {
            model.LastUpdated = DateTime.UtcNow;
            _db.HomeCtas.Add(model);
            TempData["Success"] = "CTA banner created.";
        }
        else
        {
            existing.Heading = model.Heading;
            existing.Body = model.Body;
            existing.CtaText = model.CtaText;
            existing.CtaLink = model.CtaLink;
            existing.LastUpdated = DateTime.UtcNow;
            TempData["Success"] = "CTA banner saved.";
        }
        _db.SaveChanges();

        await _logger.LogAsync("Saved", "HomeCta", 0, $"Heading: {model.Heading}");
        return RedirectToAction(nameof(HomeContent));
    }

    // ==================== HOME BLOCKS ====================
    public IActionResult HomeBlockEdit(string key)
    {
        var block = _db.HomeBlocks.FirstOrDefault(b => b.Key == key)
                    ?? new HomeBlock { Key = key };

        ViewBag.BlockKey = key;
        ViewBag.BlockLabel = key switch
        {
            "intro" => "Introduction Section",
            "president" => "President's Message",
            "women" => "Women Empowerment Section",
            "contact-cta" => "Contact CTA Banner",
            _ => key
        };
        return View(block);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> HomeBlockSave(HomeBlock model, IFormFile? ImageFile)
    {
        if (ImageFile != null && ImageFile.Length > 0)
        {
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(ImageFile.FileName).ToLowerInvariant();
            if (allowed.Contains(ext))
            {
                var folder = Path.Combine("wwwroot", "assets", "img", "home");
                Directory.CreateDirectory(folder);
                var fileName = $"{Guid.NewGuid():N}{ext}";
                using (var stream = System.IO.File.Create(Path.Combine(folder, fileName)))
                    ImageFile.CopyTo(stream);
                model.ImagePath = $"/assets/img/home/{fileName}";
            }
        }
        else if (model.Id > 0)
        {
            var existing = _db.HomeBlocks.AsNoTracking().FirstOrDefault(b => b.Id == model.Id);
            if (existing != null && string.IsNullOrEmpty(model.ImagePath))
                model.ImagePath = existing.ImagePath;
        }

        var existingBlock = _db.HomeBlocks.FirstOrDefault(b => b.Key == model.Key);

        string action;
        if (existingBlock == null)
        {
            model.LastUpdated = DateTime.UtcNow;
            _db.HomeBlocks.Add(model);
            TempData["Success"] = $"{model.Key} block created.";
            action = "Created";
        }
        else
        {
            existingBlock.Title = model.Title;
            existingBlock.Subtitle = model.Subtitle;
            existingBlock.Body = model.Body;
            existingBlock.Body2 = model.Body2;
            existingBlock.ImagePath = model.ImagePath;
            existingBlock.PersonName = model.PersonName;
            existingBlock.PersonTitle = model.PersonTitle;
            existingBlock.CtaText = model.CtaText;
            existingBlock.CtaLink = model.CtaLink;
            existingBlock.IsActive = model.IsActive;
            existingBlock.LastUpdated = DateTime.UtcNow;
            TempData["Success"] = $"{model.Key} block saved.";
            action = "Updated";
        }
        _db.SaveChanges();

        await _logger.LogAsync(action, "HomeBlock", model.Id, $"Key: {model.Key}");
        return RedirectToAction(nameof(HomeContent));
    }

    // ==================== NEWS ====================
    public IActionResult News(bool showForm = false, int? id = null)
    {
        ViewBag.ShowForm = showForm;
        ViewBag.CurrentArticle = id.HasValue
            ? _db.NewsArticles.FirstOrDefault(n => n.Id == id.Value) : null;
        return View(_db.NewsArticles.OrderByDescending(n => n.PublishedDate).ToList());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> NewsSave(NewsArticle model, IFormFile? imageFile)
    {
        if (imageFile != null && imageFile.Length > 0)
        {
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            if (allowed.Contains(ext))
            {
                var folder = Path.Combine("wwwroot", "assets", "img", "news");
                Directory.CreateDirectory(folder);
                var fileName = $"{Guid.NewGuid():N}{ext}";
                using (var stream = System.IO.File.Create(Path.Combine(folder, fileName)))
                    imageFile.CopyTo(stream);
                model.ImagePath = $"/assets/img/news/{fileName}";
            }
        }
        else if (model.Id > 0)
        {
            var existing = _db.NewsArticles.AsNoTracking().FirstOrDefault(n => n.Id == model.Id);
            if (existing != null && string.IsNullOrEmpty(model.ImagePath))
                model.ImagePath = existing.ImagePath;
        }

        string action;
        if (model.Id > 0)
        {
            _db.NewsArticles.Update(model);
            TempData["Success"] = $"\"{model.Title}\" updated.";
            action = "Updated";
        }
        else
        {
            _db.NewsArticles.Add(model);
            TempData["Success"] = $"\"{model.Title}\" published.";
            action = "Created";
        }
        _db.SaveChanges();

        await _logger.LogAsync(action, "NewsArticle", model.Id, $"Title: {model.Title}");
        return RedirectToAction(nameof(News));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> NewsDelete(int id)
    {
        var article = _db.NewsArticles.Find(id);
        if (article != null)
        {
            _db.NewsArticles.Remove(article);
            _db.SaveChanges();
            TempData["Success"] = $"\"{article.Title}\" deleted.";
            await _logger.LogAsync("Deleted", "NewsArticle", id, $"Title: {article.Title}");
        }
        return RedirectToAction(nameof(News));
    }

    // ==================== EVENTS ====================
    public IActionResult Events(bool showForm = false, int? id = null)
    {
        ViewBag.ShowForm = showForm;
        ViewBag.CurrentEvent = id.HasValue
            ? _db.Events.FirstOrDefault(e => e.Id == id.Value) : null;
        return View(_db.Events.OrderByDescending(e => e.StartDate).ToList());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EventSave(EventItem model, IFormFile? imageFile)
    {
        if (imageFile != null && imageFile.Length > 0)
        {
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            if (allowed.Contains(ext))
            {
                var folder = Path.Combine("wwwroot", "assets", "img", "events");
                Directory.CreateDirectory(folder);
                var fileName = $"{Guid.NewGuid():N}{ext}";
                using (var stream = System.IO.File.Create(Path.Combine(folder, fileName)))
                    imageFile.CopyTo(stream);
                model.CoverImagePath = $"/assets/img/events/{fileName}";
            }
        }
        else if (model.Id > 0)
        {
            var existing = _db.Events.AsNoTracking().FirstOrDefault(e => e.Id == model.Id);
            if (existing != null && string.IsNullOrEmpty(model.CoverImagePath))
                model.CoverImagePath = existing.CoverImagePath;
        }

        string action;
        if (model.Id > 0)
        {
            _db.Events.Update(model);
            TempData["Success"] = $"\"{model.Title}\" updated.";
            action = "Updated";
        }
        else
        {
            _db.Events.Add(model);
            TempData["Success"] = $"\"{model.Title}\" created.";
            action = "Created";
        }
        _db.SaveChanges();

        await _logger.LogAsync(action, "EventItem", model.Id, $"Title: {model.Title}");
        return RedirectToAction(nameof(Events));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EventDelete(int id)
    {
        var ev = _db.Events.Find(id);
        if (ev != null)
        {
            _db.Events.Remove(ev);
            _db.SaveChanges();
            TempData["Success"] = $"\"{ev.Title}\" deleted.";
            await _logger.LogAsync("Deleted", "EventItem", id, $"Title: {ev.Title}");
        }
        return RedirectToAction(nameof(Events));
    }

    // ==================== COMMITTEE ====================
    public IActionResult Committee(bool showForm = false, int? id = null)
    {
        ViewBag.ShowForm = showForm;
        ViewBag.CurrentMember = id.HasValue
            ? _db.CommitteeMembers.FirstOrDefault(m => m.Id == id.Value) : null;
        return View(_db.CommitteeMembers.OrderBy(m => m.DisplayOrder).ToList());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CommitteeSave(CommitteeMember model, IFormFile? imageFile)
    {
        if (imageFile != null && imageFile.Length > 0)
        {
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            if (allowed.Contains(ext))
            {
                var folder = Path.Combine("wwwroot", "assets", "img", "committee");
                Directory.CreateDirectory(folder);
                var fileName = $"{Guid.NewGuid():N}{ext}";
                using (var stream = System.IO.File.Create(Path.Combine(folder, fileName)))
                    imageFile.CopyTo(stream);
                model.PhotoPath = $"/assets/img/committee/{fileName}";
            }
        }
        else if (model.Id > 0)
        {
            var existing = _db.CommitteeMembers.AsNoTracking().FirstOrDefault(m => m.Id == model.Id);
            if (existing != null && string.IsNullOrEmpty(model.PhotoPath))
                model.PhotoPath = existing.PhotoPath;
        }

        string action;
        if (model.Id > 0)
        {
            _db.CommitteeMembers.Update(model);
            TempData["Success"] = $"\"{model.FullName}\" updated.";
            action = "Updated";
        }
        else
        {
            _db.CommitteeMembers.Add(model);
            TempData["Success"] = $"\"{model.FullName}\" added.";
            action = "Created";
        }
        _db.SaveChanges();

        await _logger.LogAsync(action, "CommitteeMember", model.Id, $"Name: {model.FullName}");
        return RedirectToAction(nameof(Committee));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CommitteeDelete(int id)
    {
        var m = _db.CommitteeMembers.Find(id);
        if (m != null)
        {
            _db.CommitteeMembers.Remove(m);
            _db.SaveChanges();
            TempData["Success"] = $"\"{m.FullName}\" removed.";
            await _logger.LogAsync("Deleted", "CommitteeMember", id, $"Name: {m.FullName}");
        }
        return RedirectToAction(nameof(Committee));
    }

    // ==================== ABOUT ====================
    public IActionResult About(bool showForm = false, int? id = null)
    {
        ViewBag.ShowForm = showForm;
        ViewBag.CurrentSection = id.HasValue
            ? _db.AboutSections.AsNoTracking().FirstOrDefault(s => s.Id == id.Value) : null;
        ViewBag.Sections = _db.AboutSections.AsNoTracking().OrderBy(s => s.DisplayOrder).ToList();
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AboutSectionSave(AboutSection model, IFormFile? imageFile)
    {
        if (imageFile != null && imageFile.Length > 0)
        {
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            if (allowed.Contains(ext))
            {
                var folder = Path.Combine("wwwroot", "assets", "img", "about");
                Directory.CreateDirectory(folder);
                var fileName = $"{Guid.NewGuid():N}{ext}";
                using (var stream = System.IO.File.Create(Path.Combine(folder, fileName)))
                    imageFile.CopyTo(stream);
                model.ImagePath = $"/assets/img/about/{fileName}";
            }
        }
        else if (model.Id > 0)
        {
            var existing = _db.AboutSections.AsNoTracking().FirstOrDefault(s => s.Id == model.Id);
            if (existing != null && string.IsNullOrEmpty(model.ImagePath))
                model.ImagePath = existing.ImagePath;
        }

        string action;
        if (model.Id > 0)
        {
            _db.AboutSections.Update(model);
            TempData["Success"] = $"\"{model.Title}\" updated.";
            action = "Updated";
        }
        else
        {
            _db.AboutSections.Add(model);
            TempData["Success"] = $"\"{model.Title}\" added.";
            action = "Created";
        }
        _db.SaveChanges();

        await _logger.LogAsync(action, "AboutSection", model.Id, $"Title: {model.Title}");
        return RedirectToAction(nameof(About));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AboutSectionDelete(int id)
    {
        var s = _db.AboutSections.Find(id);
        if (s != null)
        {
            _db.AboutSections.Remove(s);
            _db.SaveChanges();
            TempData["Success"] = $"\"{s.Title}\" removed.";
            await _logger.LogAsync("Deleted", "AboutSection", id, $"Title: {s.Title}");
        }
        return RedirectToAction(nameof(About));
    }

    // ==================== WOMEN ====================
    public IActionResult Women(bool showForm = false, bool showIntroForm = false,
                              bool showMiniForm = false, int? id = null, int? miniId = null)
    {
        ViewBag.ShowForm = showForm;
        ViewBag.ShowIntroForm = showIntroForm;
        ViewBag.ShowMiniForm = showMiniForm;

        var features = _db.WomenFeatures.AsNoTracking().OrderBy(f => f.DisplayOrder).ToList();
        ViewBag.Features = features;
        ViewBag.CurrentFeature = id.HasValue
            ? features.FirstOrDefault(f => f.Id == id.Value) : null;

        var minis = _db.WomenMiniFeatures.AsNoTracking().OrderBy(m => m.DisplayOrder).ToList();
        ViewBag.MiniFeatures = minis;
        ViewBag.CurrentMini = miniId.HasValue
            ? minis.FirstOrDefault(m => m.Id == miniId.Value) : null;

        var content = _db.WomenEmpowermentContents.AsNoTracking().FirstOrDefault()
                      ?? new WomenEmpowermentContent();
        return View(content);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> WomenCombinedSave(
        string Title,
        string? Body,
        IFormFile? imageFile,
        string? MiniIconClass,
        string? MiniLabel,
        string? MiniLink,
        int DisplayOrder = 1,
        bool IsActive = true)
    {
        var main = new WomenFeature
        {
            Title = Title ?? "",
            Body = Body ?? "",
            DisplayOrder = DisplayOrder,
            IsActive = IsActive
        };

        if (imageFile != null && imageFile.Length > 0)
        {
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            if (allowed.Contains(ext))
            {
                var folder = Path.Combine("wwwroot", "assets", "img", "women");
                Directory.CreateDirectory(folder);
                var fileName = $"{Guid.NewGuid():N}{ext}";
                using (var stream = System.IO.File.Create(Path.Combine(folder, fileName)))
                    imageFile.CopyTo(stream);
                main.ImagePath = $"/assets/img/women/{fileName}";
            }
        }

        _db.WomenFeatures.Add(main);

        if (!string.IsNullOrWhiteSpace(MiniLabel))
        {
            var mini = new WomenMiniFeature
            {
                IconClass = string.IsNullOrWhiteSpace(MiniIconClass) ? "bi-star" : MiniIconClass,
                Label = MiniLabel,
                Link = MiniLink,
                DisplayOrder = DisplayOrder,
                IsActive = IsActive
            };
            _db.WomenMiniFeatures.Add(mini);
        }

        _db.SaveChanges();

        TempData["Success"] = $"\"{Title}\" created (main + mini).";
        await _logger.LogAsync("Created", "WomenFeature", 0, $"Title: {Title}");
        return RedirectToAction(nameof(Women));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> WomenMiniSave(WomenMiniFeature model)
    {
        string action;
        if (model.Id > 0)
        {
            _db.WomenMiniFeatures.Update(model);
            TempData["Success"] = $"\"{model.Label}\" updated.";
            action = "Updated";
        }
        else
        {
            _db.WomenMiniFeatures.Add(model);
            TempData["Success"] = $"\"{model.Label}\" added.";
            action = "Created";
        }
        _db.SaveChanges();

        await _logger.LogAsync(action, "WomenMiniFeature", model.Id, $"Label: {model.Label}");
        return RedirectToAction(nameof(Women));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> WomenMiniDelete(int id)
    {
        var m = _db.WomenMiniFeatures.Find(id);
        if (m != null)
        {
            _db.WomenMiniFeatures.Remove(m);
            _db.SaveChanges();
            TempData["Success"] = $"\"{m.Label}\" removed.";
            await _logger.LogAsync("Deleted", "WomenMiniFeature", id, $"Label: {m.Label}");
        }
        return RedirectToAction(nameof(Women));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> WomenSave(WomenEmpowermentContent model, IFormFile? imageFile)
    {
        var existing = _db.WomenEmpowermentContents.FirstOrDefault();

        if (imageFile != null && imageFile.Length > 0)
        {
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            if (allowed.Contains(ext))
            {
                var folder = Path.Combine("wwwroot", "assets", "img", "women");
                Directory.CreateDirectory(folder);
                var fileName = $"{Guid.NewGuid():N}{ext}";
                using (var stream = System.IO.File.Create(Path.Combine(folder, fileName)))
                    imageFile.CopyTo(stream);
                model.CoverImagePath = $"/assets/img/women/{fileName}";
            }
        }
        else if (existing != null)
        {
            if (string.IsNullOrEmpty(model.CoverImagePath))
                model.CoverImagePath = existing.CoverImagePath;
        }

        if (existing == null)
        {
            model.LastUpdated = DateTime.UtcNow;
            _db.WomenEmpowermentContents.Add(model);
            TempData["Success"] = "Women page intro created.";
        }
        else
        {
            existing.IntroTitle = model.IntroTitle;
            existing.IntroBody = model.IntroBody;
            existing.HighlightTitle = model.HighlightTitle;
            existing.HighlightBody = model.HighlightBody;
            existing.CoverImagePath = model.CoverImagePath;
            existing.LastUpdated = DateTime.UtcNow;
            TempData["Success"] = "Women page intro saved.";
        }
        _db.SaveChanges();

        await _logger.LogAsync("Saved", "WomenContent", model.Id, $"Title: {model.IntroTitle}");
        return RedirectToAction(nameof(Women));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> WomenFeatureSave(WomenFeature model, IFormFile? imageFile)
    {
        if (imageFile != null && imageFile.Length > 0)
        {
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            if (allowed.Contains(ext))
            {
                var folder = Path.Combine("wwwroot", "assets", "img", "women");
                Directory.CreateDirectory(folder);
                var fileName = $"{Guid.NewGuid():N}{ext}";
                using (var stream = System.IO.File.Create(Path.Combine(folder, fileName)))
                    imageFile.CopyTo(stream);
                model.ImagePath = $"/assets/img/women/{fileName}";
            }
        }
        else if (model.Id > 0)
        {
            var existing = _db.WomenFeatures.AsNoTracking().FirstOrDefault(f => f.Id == model.Id);
            if (existing != null && string.IsNullOrEmpty(model.ImagePath))
                model.ImagePath = existing.ImagePath;
        }

        string action;
        if (model.Id > 0)
        {
            _db.WomenFeatures.Update(model);
            TempData["Success"] = $"\"{model.Title}\" updated.";
            action = "Updated";
        }
        else
        {
            _db.WomenFeatures.Add(model);
            TempData["Success"] = $"\"{model.Title}\" added.";
            action = "Created";
        }
        _db.SaveChanges();

        await _logger.LogAsync(action, "WomenFeature", model.Id, $"Title: {model.Title}");
        return RedirectToAction(nameof(Women));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> WomenFeatureDelete(int id)
    {
        var f = _db.WomenFeatures.Find(id);
        if (f != null)
        {
            _db.WomenFeatures.Remove(f);
            _db.SaveChanges();
            TempData["Success"] = $"\"{f.Title}\" removed.";
            await _logger.LogAsync("Deleted", "WomenFeature", id, $"Title: {f.Title}");
        }
        return RedirectToAction(nameof(Women));
    }

    // ==================== INDUSTRY ====================
    public IActionResult Industry(bool showForm = false, int? id = null)
    {
        ViewBag.ShowForm = showForm;
        ViewBag.CurrentArea = id.HasValue
            ? _db.IndustryAreas.FirstOrDefault(a => a.Id == id.Value) : null;
        return View(_db.IndustryAreas.OrderBy(a => a.DisplayOrder).ToList());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> IndustrySave(IndustryArea model, IFormFile? imageFile)
    {
        if (imageFile != null && imageFile.Length > 0)
        {
            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            if (allowed.Contains(ext))
            {
                var folder = Path.Combine("wwwroot", "assets", "img", "industry");
                Directory.CreateDirectory(folder);
                var fileName = $"{Guid.NewGuid():N}{ext}";
                using (var stream = System.IO.File.Create(Path.Combine(folder, fileName)))
                    imageFile.CopyTo(stream);
                model.ImagePath = $"/assets/img/industry/{fileName}";
            }
        }
        else if (model.Id > 0)
        {
            var existing = _db.IndustryAreas.AsNoTracking().FirstOrDefault(a => a.Id == model.Id);
            if (existing != null && string.IsNullOrEmpty(model.ImagePath))
                model.ImagePath = existing.ImagePath;
        }

        var others = _db.IndustryAreas.Where(a => a.Id != model.Id).OrderBy(a => a.DisplayOrder).ToList();
        if (model.DisplayOrder <= 0) model.DisplayOrder = others.Count + 1;

        if (others.Any(o => o.DisplayOrder == model.DisplayOrder))
        {
            if (model.DisplayOrder > others.Count) model.DisplayOrder = others.Count + 1;
            foreach (var o in others.Where(x => x.DisplayOrder >= model.DisplayOrder)) o.DisplayOrder++;
        }

        string action;
        if (model.Id > 0)
        {
            _db.IndustryAreas.Update(model);
            TempData["Success"] = $"\"{model.Title}\" updated.";
            action = "Updated";
        }
        else
        {
            _db.IndustryAreas.Add(model);
            TempData["Success"] = $"\"{model.Title}\" added.";
            action = "Created";
        }
        _db.SaveChanges();

        var all = _db.IndustryAreas.OrderBy(a => a.DisplayOrder).ThenBy(a => a.Id).ToList();
        for (int i = 0; i < all.Count; i++) all[i].DisplayOrder = i + 1;
        _db.SaveChanges();

        await _logger.LogAsync(action, "IndustryArea", model.Id, $"Title: {model.Title}");
        return RedirectToAction(nameof(Industry));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> IndustryDelete(int id)
    {
        var area = _db.IndustryAreas.Find(id);
        if (area != null)
        {
            _db.IndustryAreas.Remove(area);
            _db.SaveChanges();
            TempData["Success"] = $"\"{area.Title}\" deleted.";
            await _logger.LogAsync("Deleted", "IndustryArea", id, $"Title: {area.Title}");
        }
        return RedirectToAction(nameof(Industry));
    }

    // ==================== CONTACT CONTENT ====================
    public IActionResult ContactContent(bool showForm = false)
    {
        ViewBag.ShowForm = showForm;
        var model = _db.ContactContents.AsNoTracking().FirstOrDefault() ?? new ContactContent();
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ContactContentSave(ContactContent model)
    {
        var existing = _db.ContactContents.FirstOrDefault();

        if (existing == null)
        {
            model.LastUpdated = DateTime.UtcNow;
            _db.ContactContents.Add(model);
            TempData["Success"] = "Contact content created.";
        }
        else
        {
            existing.Kicker = model.Kicker;
            existing.Heading = model.Heading;
            existing.Intro = model.Intro;
            existing.Address = model.Address;
            existing.MapUrl = model.MapUrl;
            existing.Email = model.Email;
            existing.Phone = model.Phone;
            existing.EmailDesc = model.EmailDesc;
            existing.PhoneDesc = model.PhoneDesc;
            existing.Weekdays = model.Weekdays;
            existing.Hours = model.Hours;
            existing.Note = model.Note;
            existing.LastUpdated = DateTime.UtcNow;
            TempData["Success"] = "Contact content saved.";
        }
        _db.SaveChanges();

        await _logger.LogAsync("Saved", "ContactContent", 0, "");
        return RedirectToAction(nameof(ContactContent));
    }

    // ==================== SYSTEM ====================
    public async Task<IActionResult> Users()
    {
        var users = await _userManager.Users.ToListAsync();
        var userList = new List<(Core.Data.ApplicationUser User, string Role)>();

        foreach (var u in users)
        {
            var roles = await _userManager.GetRolesAsync(u);
            userList.Add((u, roles.FirstOrDefault() ?? "Member"));
        }

        ViewBag.Users = userList;
        return View();
    }

    public IActionResult CreateUser() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(string Email, string Password, string Role)
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            TempData["Error"] = "Email and password are required.";
            return RedirectToAction(nameof(Users));
        }

        var existing = await _userManager.FindByEmailAsync(Email);
        if (existing != null)
        {
            TempData["Error"] = $"User {Email} already exists.";
            return RedirectToAction(nameof(Users));
        }

        var user = new Core.Data.ApplicationUser
        {
            UserName = Email,
            Email = Email,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, Password);

        if (!result.Succeeded)
        {
            TempData["Error"] = "Failed: " + string.Join(", ", result.Errors.Select(e => e.Description));
            return RedirectToAction(nameof(Users));
        }

        if (string.IsNullOrWhiteSpace(Role)) Role = "Member";
        if (!await _roleManager.RoleExistsAsync(Role))
            await _roleManager.CreateAsync(new IdentityRole(Role));

        await _userManager.AddToRoleAsync(user, Role);

        await _logger.LogAsync("Created", "User", 0, $"Email: {Email}, Role: {Role}");
        TempData["Success"] = $"User {Email} created with role {Role}.";
        return RedirectToAction(nameof(Users));
    }

    public async Task<IActionResult> EditUser(string id)
    {
        if (string.IsNullOrEmpty(id)) return RedirectToAction(nameof(Users));

        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return RedirectToAction(nameof(Users));

        var roles = await _userManager.GetRolesAsync(user);
        ViewBag.CurrentRole = roles.FirstOrDefault() ?? "Member";
        return View(user);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(string Id, string Role, string? NewPassword)
    {
        var user = await _userManager.FindByIdAsync(Id);
        if (user == null)
        {
            TempData["Error"] = "User not found.";
            return RedirectToAction(nameof(Users));
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);

        if (string.IsNullOrWhiteSpace(Role)) Role = "Member";
        if (!await _roleManager.RoleExistsAsync(Role))
            await _roleManager.CreateAsync(new IdentityRole(Role));

        await _userManager.AddToRoleAsync(user, Role);

        if (!string.IsNullOrWhiteSpace(NewPassword))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            await _userManager.ResetPasswordAsync(user, token, NewPassword);
        }

        await _logger.LogAsync("Updated", "User", 0, $"Email: {user.Email}, Role: {Role}");
        TempData["Success"] = $"User {user.Email} updated.";
        return RedirectToAction(nameof(Users));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            TempData["Error"] = "User not found.";
            return RedirectToAction(nameof(Users));
        }

        var email = user.Email;
        await _userManager.DeleteAsync(user);

        await _logger.LogAsync("Deleted", "User", 0, $"Email: {email}");
        TempData["Success"] = $"User {email} deleted.";
        return RedirectToAction(nameof(Users));
    }

    // ==================== PROFILE ====================
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction(nameof(Login));

        var roles = await _userManager.GetRolesAsync(user);
        ViewBag.Role = roles.FirstOrDefault() ?? "Admin";

        return View(user);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ProfileSave(
        string? PhoneNumber,
        string? CurrentPassword,
        string? NewPassword,
        string? ConfirmPassword)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return RedirectToAction(nameof(Login));

        if (PhoneNumber != user.PhoneNumber)
        {
            user.PhoneNumber = PhoneNumber;
            await _userManager.UpdateAsync(user);
        }

        if (!string.IsNullOrWhiteSpace(CurrentPassword) &&
            !string.IsNullOrWhiteSpace(NewPassword) &&
            !string.IsNullOrWhiteSpace(ConfirmPassword))
        {
            if (NewPassword != ConfirmPassword)
            {
                TempData["Error"] = "New password and confirm password do not match.";
                return RedirectToAction(nameof(Profile));
            }

            var result = await _userManager.ChangePasswordAsync(user, CurrentPassword, NewPassword);
            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join(", ", result.Errors.Select(e => e.Description));
                return RedirectToAction(nameof(Profile));
            }

            await _logger.LogAsync("Updated", "User", 0, $"Password changed: {user.Email}");
        }

        await _logger.LogAsync("Updated", "User", 0, $"Profile: {user.Email}");
        TempData["Success"] = "Profile updated successfully.";
        return RedirectToAction(nameof(Profile));
    }

    // ==================== ACTIVITY LOG ====================
    public IActionResult ActivityLog()
    {
        var logs = _db.ActivityLogEntries
            .OrderByDescending(l => l.Timestamp)
            .Take(500)
            .ToList();
        return View(logs);
    }

    // ==================== MOCK DATA ====================
    private static List<MembershipApplication> GetMockApplications() => new()
    {
        new MembershipApplication { Id=1, FullName="Ali Khan", FatherHusbandName="Muhammad Khan", Cnic="17301-1234567-1", BusinessName="Peshawar Gems & Co.", Designation="Owner", NatureOfBusiness="Gemstone Trading", MobileNo="0300-1234567", Email="ali.khan@example.com", CityDistrict="Peshawar", MembershipCategory="Ordinary Member", MembershipFee=1000m, BusinessAddress="Namak Mandi, Peshawar", BusinessTypes="Gemstone, Jewellery", Status="Pending Review", ApplicationDate=DateTime.UtcNow.AddDays(-2) },
        new MembershipApplication { Id=2, FullName="Hamza Ahmad", FatherHusbandName="Rashid Ahmad", Cnic="15402-7654321-3", BusinessName="Swat Jewellers", Designation="CEO", NatureOfBusiness="Jewellery Manufacturing", MobileNo="0333-9876543", Email="hamza@swatjewellers.pk", CityDistrict="Swat", MembershipCategory="Associate Member", MembershipFee=2500m, BusinessAddress="Mingora Bazaar, Swat", BusinessTypes="Jewellery, Retail", Status="Approved for Payment", ApplicationDate=DateTime.UtcNow.AddDays(-5) },
        new MembershipApplication { Id=3, FullName="Fatima Bibi", FatherHusbandName="Abdul Wali", Cnic="17301-1112223-4", BusinessName="Fatima Handcrafted Jewels", Designation="Founder", NatureOfBusiness="Handmade Jewellery", MobileNo="0345-1112223", Email="fatima@handcrafted.pk", CityDistrict="Peshawar", MembershipCategory="Ordinary Member", MembershipFee=1000m, BusinessAddress="University Road, Peshawar", BusinessTypes="Handmade, Artisan", Status="Active Member", ApplicationDate=DateTime.UtcNow.AddDays(-30), MembershipNo="KPG-2026-0003", FeeReceived=1000m, PaymentReference="TRX-99323" },
        new MembershipApplication { Id=4, FullName="Zubair Shah", FatherHusbandName="Naeem Shah", Cnic="14302-3334445-5", BusinessName="Shah Gemstones", Designation="Partner", NatureOfBusiness="Wholesale", MobileNo="0311-3334445", Email="zubair@shahgems.com", CityDistrict="Mardan", MembershipCategory="Honorary Member", MembershipFee=3500m, BusinessAddress="Main Bazaar, Mardan", BusinessTypes="Wholesale, Export", Status="Rejected", ApplicationDate=DateTime.UtcNow.AddDays(-10) },
        new MembershipApplication { Id=5, FullName="Ayesha Tariq", FatherHusbandName="Tariq Mehmood", Cnic="17301-5556667-6", BusinessName="Ayesha Silver Arts", Designation="Director", NatureOfBusiness="Silver Jewellery", MobileNo="0301-5556667", Email="ayesha@silverarts.pk", CityDistrict="Peshawar", MembershipCategory="Associate Member", MembershipFee=2500m, BusinessAddress="Saddar, Peshawar", BusinessTypes="Silver, Retail", Status="Payment Submitted", ApplicationDate=DateTime.UtcNow.AddDays(-3), FeeReceived=2500m, PaymentReference="TRX-88123" }
    };
}