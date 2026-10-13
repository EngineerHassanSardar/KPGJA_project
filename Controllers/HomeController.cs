using Core.Data;
using Entities.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KPGJA.Web.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;

    public HomeController(ApplicationDbContext db)
    {
        _db = db;
    }

    public IActionResult Index()
    {
        // Hero + Sections
        ViewBag.Slides = _db.HeroSlides.AsNoTracking()
            .Where(s => s.IsActive).OrderBy(s => s.DisplayOrder).ToList();

        ViewBag.Objectives = _db.HomeSections.AsNoTracking()
            .Where(s => s.SectionKey == "objectives" && s.IsActive)
            .OrderBy(s => s.DisplayOrder).ToList();

        ViewBag.QuickLinks = _db.HomeSections.AsNoTracking()
            .Where(s => s.SectionKey == "quicklinks" && s.IsActive)
            .OrderBy(s => s.DisplayOrder).ToList();

        ViewBag.Cta = _db.HomeCtas.AsNoTracking().FirstOrDefault()
                      ?? new Entities.Entities.HomeCta();

        // Home Blocks (intro, president, women, contact-cta)
        var blocks = _db.HomeBlocks.AsNoTracking()
            .Where(b => b.IsActive)
            .ToList();

        ViewBag.Intro = blocks.FirstOrDefault(b => b.Key == "intro");
        ViewBag.President = blocks.FirstOrDefault(b => b.Key == "president");
        ViewBag.Women = blocks.FirstOrDefault(b => b.Key == "women");
        ViewBag.ContactCta = blocks.FirstOrDefault(b => b.Key == "contact-cta");

        // Women mini features
        ViewBag.WomenMinis = _db.WomenMiniFeatures.AsNoTracking()
            .Where(m => m.IsActive)
            .OrderBy(m => m.DisplayOrder)
            .ToList();

        // Latest 3 from existing tables
        ViewBag.Committee = _db.CommitteeMembers.AsNoTracking()
            .Where(m => m.IsActive).OrderBy(m => m.DisplayOrder).Take(3).ToList();

        ViewBag.UpcomingEvents = _db.Events.AsNoTracking()
            .Where(e => e.IsPublished && e.StartDate >= DateTime.Today)
            .OrderBy(e => e.StartDate).Take(3).ToList();

        ViewBag.LatestNews = _db.NewsArticles.AsNoTracking()
            .Where(n => n.IsPublished).OrderByDescending(n => n.PublishedDate).Take(3).ToList();

        return View();
    }

    public IActionResult About()
    {
        var sections = _db.AboutSections
            .Where(s => s.IsActive)
            .OrderBy(s => s.DisplayOrder)
            .ToList();
        return View(sections);
    }

    public IActionResult Membership()
    {
        var fees = _db.MembershipFees.AsNoTracking()
            .Where(f => f.IsActive)
            .OrderBy(f => f.DisplayOrder)
            .ToList();

        return View(fees);
    }

    public IActionResult Executive()
    {
        var members = _db.CommitteeMembers
            .Where(m => m.IsActive)
            .OrderBy(m => m.DisplayOrder)
            .ToList();
        return View(members);
    }

    public IActionResult Women()
    {
        ViewBag.Content = _db.WomenEmpowermentContents.AsNoTracking().FirstOrDefault()
                          ?? new Entities.Entities.WomenEmpowermentContent();

        ViewBag.WomenMinis = _db.WomenMiniFeatures.AsNoTracking()
            .Where(m => m.IsActive)
            .OrderBy(m => m.DisplayOrder)
            .ToList();

        var features = _db.WomenFeatures.AsNoTracking()
            .Where(f => f.IsActive)
            .OrderBy(f => f.DisplayOrder)
            .ToList();

        return View(features);
    }
    public IActionResult Industry()
    {
        var areas = _db.IndustryAreas
            .Where(a => a.IsActive)
            .OrderBy(a => a.DisplayOrder)
            .ToList();
        return View(areas);
    }

    public IActionResult Events()
    {
        var events = _db.Events
            .Where(e => e.IsPublished)
            .OrderByDescending(e => e.StartDate)
            .ToList();
        return View(events);
    }

    public IActionResult News()
    {
        var articles = _db.NewsArticles
            .Where(n => n.IsPublished)
            .OrderByDescending(n => n.PublishedDate)
            .ToList();
        return View(articles);
    }

    public IActionResult Contact()
    {
        var content = _db.ContactContents.AsNoTracking().FirstOrDefault()
                      ?? new Entities.Entities.ContactContent();
        return View(content);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult ContactSubmit(ContactInquiry model)
    {
        if (!ModelState.IsValid)
        {
            var content = _db.ContactContents.AsNoTracking().FirstOrDefault()
                          ?? new Entities.Entities.ContactContent();
            return View("Contact", content);
        }

        model.SubmittedAt = DateTime.UtcNow;
        model.IsRead = false;
        model.IsReplied = false;

        _db.ContactInquiries.Add(model);
        _db.SaveChanges();

        TempData["ContactSuccess"] = "Thank you! Your message has been submitted. We'll respond shortly.";
        return RedirectToAction(nameof(Contact));
    }

    public IActionResult Error() => View();
}