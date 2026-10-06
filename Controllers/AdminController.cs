using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KPGJA.Web.ViewModels;
using KPGJA.Web.Models;

namespace KPGJA.Web.Controllers;

// [Authorize(Roles = "Admin")]  // <-- Uncomment in Phase 2
public class AdminController : Controller
{
    // ==================== DASHBOARD ====================
    public IActionResult Index()
    {
        var apps = GetMockApplications();
        ViewBag.TotalApplications = apps.Count;
        ViewBag.Pending = apps.Count(a => a.Status == "Pending Review");
        ViewBag.ApprovedForPayment = apps.Count(a => a.Status == "Approved for Payment" || a.Status == "Payment Submitted");
        ViewBag.TotalMembers = apps.Count(a => a.Status == "Active Member" || a.Status == "Approved");
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

  

    // ==================== NEWS ====================
    public IActionResult News(bool showForm = false, int? id = null)
    {
        ViewBag.ShowForm = showForm;
        ViewBag.CurrentArticle = id.HasValue
            ? GetMockNews().FirstOrDefault(n => n.Id == id.Value)
            : null;
        return View(GetMockNews());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult NewsSave(NewsArticle model)
    {
        TempData["Success"] = model.Id > 0
            ? $"\"{model.Title}\" updated (preview only)."
            : $"\"{model.Title}\" published (preview only).";
        return RedirectToAction(nameof(News));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult NewsDelete(int id)
    {
        TempData["Success"] = $"Article #{id} deleted (preview only).";
        return RedirectToAction(nameof(News));
    }

    // ==================== EVENTS ====================
    public IActionResult Events(bool showForm = false, int? id = null)
    {
        ViewBag.ShowForm = showForm;
        ViewBag.CurrentEvent = id.HasValue
            ? GetMockEvents().FirstOrDefault(e => e.Id == id.Value)
            : null;
        return View(GetMockEvents());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult EventSave(EventItem model)
    {
        TempData["Success"] = model.Id > 0
            ? $"\"{model.Title}\" updated (preview only)."
            : $"\"{model.Title}\" created (preview only).";
        return RedirectToAction(nameof(Events));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult EventDelete(int id)
    {
        TempData["Success"] = $"Event #{id} deleted (preview only).";
        return RedirectToAction(nameof(Events));
    }

    // ==================== COMMITTEE ====================
    public IActionResult Committee(bool showForm = false, int? id = null)
    {
        ViewBag.ShowForm = showForm;
        ViewBag.CurrentMember = id.HasValue
            ? GetMockCommittee().FirstOrDefault(m => m.Id == id.Value)
            : null;
        return View(GetMockCommittee());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult CommitteeSave(CommitteeMember model)
    {
        TempData["Success"] = model.Id > 0
            ? $"\"{model.FullName}\" updated (preview only)."
            : $"\"{model.FullName}\" added (preview only).";
        return RedirectToAction(nameof(Committee));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult CommitteeDelete(int id)
    {
        TempData["Success"] = $"Member #{id} removed (preview only).";
        return RedirectToAction(nameof(Committee));
    }

    // ==================== ABOUT ====================
    public IActionResult About(bool showForm = false, int? id = null)
    {
        ViewBag.ShowForm = showForm;
        ViewBag.Sections = GetMockAboutSections();
        ViewBag.CurrentSection = id.HasValue
            ? GetMockAboutSections().FirstOrDefault(s => s.Id == id.Value)
            : null;
        return View(GetMockAbout());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult AboutSave(AboutContent model)
    {
        TempData["Success"] = "About page header saved (preview only).";
        return RedirectToAction(nameof(About));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult AboutSectionSave(AboutSection model)
    {
        TempData["Success"] = model.Id > 0
            ? $"\"{model.Title}\" updated (preview only)."
            : $"\"{model.Title}\" added (preview only).";
        return RedirectToAction(nameof(About));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult AboutSectionDelete(int id)
    {
        TempData["Success"] = $"Section #{id} removed (preview only).";
        return RedirectToAction(nameof(About));
    }

    // ==================== WOMEN ====================
    public IActionResult Women(bool showForm = false, bool showIntroForm = false, int? id = null)
    {
        ViewBag.ShowForm = showForm;
        ViewBag.ShowIntroForm = showIntroForm;
        ViewBag.Features = GetMockWomenFeatures();
        ViewBag.CurrentFeature = id.HasValue
            ? GetMockWomenFeatures().FirstOrDefault(f => f.Id == id.Value)
            : null;
        return View(GetMockWomen());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult WomenSave(WomenEmpowermentContent model)
    {
        TempData["Success"] = "Women page header saved (preview only).";
        return RedirectToAction(nameof(Women));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult WomenFeatureSave(WomenFeature model)
    {
        TempData["Success"] = model.Id > 0
            ? $"\"{model.Title}\" updated (preview only)."
            : $"\"{model.Title}\" added (preview only).";
        return RedirectToAction(nameof(Women));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult WomenFeatureDelete(int id)
    {
        TempData["Success"] = $"Feature #{id} removed (preview only).";
        return RedirectToAction(nameof(Women));
    }

    // ==================== INDUSTRY ====================
    public IActionResult Industry(bool showForm = false, int? id = null)
    {
        ViewBag.ShowForm = showForm;
        ViewBag.CurrentArea = id.HasValue
            ? GetMockIndustryAreas().FirstOrDefault(a => a.Id == id.Value)
            : null;
        return View(GetMockIndustryAreas());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult IndustrySave(IndustryArea model)
    {
        TempData["Success"] = model.Id > 0
            ? $"\"{model.Title}\" updated (preview only)."
            : $"\"{model.Title}\" created (preview only).";
        return RedirectToAction(nameof(Industry));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult IndustryDelete(int id)
    {
        TempData["Success"] = $"Area #{id} deleted (preview only).";
        return RedirectToAction(nameof(Industry));
    }

    // ==================== CONTACT CONTENT (page editor) ====================
    public IActionResult ContactContent(bool showForm = false)
    {
        ViewBag.ShowForm = showForm;
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult ContactContentSave()
    {
        TempData["Success"] = "Contact content saved (preview only).";
        return RedirectToAction(nameof(ContactContent));
    }

    // ==================== CONTACT INQUIRIES (inbox) ====================
    public IActionResult Contact() => View(GetMockContactInquiries());

    public IActionResult ContactDetail(int id)
    {
        var inquiry = GetMockContactInquiries().FirstOrDefault(c => c.Id == id);
        if (inquiry == null) return RedirectToAction(nameof(Contact));
        return View(inquiry);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult ContactMarkRead(int id)
    {
        TempData["Success"] = $"Inquiry #{id} marked as read.";
        return RedirectToAction(nameof(Contact));
    }

    // ==================== SYSTEM ====================
    public IActionResult Users() => View();
    public IActionResult Settings() => View();
    public IActionResult Profile() => View();
    public IActionResult Reports() => View();
    public IActionResult ActivityLog() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult SettingsSave()
    {
        TempData["Success"] = "Settings saved (preview only).";
        return RedirectToAction(nameof(Settings));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult ProfileSave()
    {
        TempData["Success"] = "Profile updated (preview only).";
        return RedirectToAction(nameof(Profile));
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

    private static List<NewsArticle> GetMockNews() => new()
    {
        new NewsArticle { Id=1, Title="KPGJA Hosts Successful Gem & Jewellery Expo 2026", Category="Announcement", Summary="Over 200 exhibitors and 5,000 visitors attended the three-day expo in Peshawar.", Content="Full article content...", Author="Admin", IsPublished=true, PublishedDate=DateTime.UtcNow.AddDays(-3) },
        new NewsArticle { Id=2, Title="Women Artisans Complete Jewellery Design Training", Category="Women Empowerment", Summary="25 women from Peshawar and Swat completed a 6-week intensive program.", Content="Full article content...", Author="Admin", IsPublished=true, PublishedDate=DateTime.UtcNow.AddDays(-10) },
        new NewsArticle { Id=3, Title="New Export Compliance Guidelines for 2027", Category="Industry Update", Summary="The government has issued updated compliance guidelines for gemstone exporters.", Content="Full article content...", Author="Admin", IsPublished=false, PublishedDate=DateTime.UtcNow.AddDays(-1) },
        new NewsArticle { Id=4, Title="KPGJA Signs MoU with International Gem Institute", Category="Press Release", Summary="Partnership will bring internationally recognized certification to local artisans.", Content="Full article content...", Author="Admin", IsPublished=true, PublishedDate=DateTime.UtcNow.AddDays(-20) }
    };

    private static List<EventItem> GetMockEvents() => new()
    {
        new EventItem { Id=1, Title="Gem & Jewellery Expo 2026", Location="Peshawar Expo Center", Description="Three-day exhibition featuring 200+ exhibitors.", StartDate=new DateTime(2026,11,12), EndDate=new DateTime(2026,11,14), IsPublished=true },
        new EventItem { Id=2, Title="Women Artisans Workshop", Location="KPGJA Hall, Peshawar", Description="Hands-on jewellery design workshop for women artisans.", StartDate=new DateTime(2026,10,22), IsPublished=true },
        new EventItem { Id=3, Title="Export Compliance Training", Location="Online Webinar", Description="Government guidelines for gemstone exporters.", StartDate=new DateTime(2026,11,5), IsPublished=true },
        new EventItem { Id=4, Title="Annual General Meeting", Location="Serena Hotel, Peshawar", Description="Annual meeting of all KPGJA members.", StartDate=new DateTime(2026,12,30), IsPublished=false }
    };

    private static List<CommitteeMember> GetMockCommittee() => new()
    {
        new CommitteeMember { Id=1, FullName="Muhammad Aslam", Designation="President", Bio="Leading KPGJA since 2022.", DisplayOrder=1, IsActive=true },
        new CommitteeMember { Id=2, FullName="Imran Haider", Designation="Vice President", Bio="Industry veteran with 20 years experience.", DisplayOrder=2, IsActive=true },
        new CommitteeMember { Id=3, FullName="Rashid Mehmood", Designation="Secretary Chairman", Bio="Oversees KPGJA operations.", DisplayOrder=3, IsActive=true },
        new CommitteeMember { Id=4, FullName="Nasir Ahmad", Designation="Finance Secretary", Bio="Manages association finances.", DisplayOrder=4, IsActive=true },
        new CommitteeMember { Id=5, FullName="Sajid Ali", Designation="Office Secretary", Bio="Handles daily office operations.", DisplayOrder=5, IsActive=true },
        new CommitteeMember { Id=6, FullName="Bilal Khan", Designation="Youth Secretary", Bio="Coordinates youth programs.", DisplayOrder=6, IsActive=true }
    };

    private static AboutContent GetMockAbout() => new()
    {
        Id = 1,
        SectionTitle = "Who We Are",
        MissionStatement = "To unite, empower and represent the gemstone and jewellery community of Khyber Pakhtunkhwa through advocacy, skill development and sustainable growth.",
        VisionStatement = "A globally recognized, prosperous and inclusive gem and jewellery sector rooted in Khyber Pakhtunkhwa's rich heritage.",
        LastUpdated = DateTime.UtcNow.AddDays(-15)
    };

    private static List<AboutSection> GetMockAboutSections() => new()
    {
        new AboutSection { Id=1, Title="Who We Are", Body="KPGJA is the apex body representing stakeholders across the entire gem and jewellery value chain.", IconClass="bi-people", DisplayOrder=1, IsActive=true },
        new AboutSection { Id=2, Title="Our Mission", Body="To unite, empower and represent the gemstone and jewellery community of KP.", IconClass="bi-bullseye", DisplayOrder=2, IsActive=true },
        new AboutSection { Id=3, Title="Our Vision", Body="A globally recognized, prosperous and inclusive gem and jewellery sector.", IconClass="bi-eye", DisplayOrder=3, IsActive=true },
        new AboutSection { Id=4, Title="Our Values", Body="Integrity, transparency, craftsmanship and community-first growth.", IconClass="bi-heart", DisplayOrder=4, IsActive=true }
    };

    private static WomenEmpowermentContent GetMockWomen() => new()
    {
        Id = 1,
        IntroTitle = "Women in the Gem & Jewellery Sector",
        IntroBody = "KPGJA is committed to creating equal opportunities for women artisans, designers and entrepreneurs across Khyber Pakhtunkhwa.",
        HighlightTitle = "Training 500+ Women by 2027",
        HighlightBody = "Through partnerships with international organizations, KPGJA runs skill development programs focused on jewellery design, gemstone cutting and digital commerce.",
        LastUpdated = DateTime.UtcNow.AddDays(-5)
    };

    private static List<WomenFeature> GetMockWomenFeatures() => new()
    {
        new WomenFeature { Id=1, Title="Skill Training", Body="Hands-on jewellery design and gemstone cutting workshops.", IconClass="bi-mortarboard", DisplayOrder=1, IsActive=true },
        new WomenFeature { Id=2, Title="Financial Support", Body="Micro-grants and interest-free loans for women entrepreneurs.", IconClass="bi-cash-coin", DisplayOrder=2, IsActive=true },
        new WomenFeature { Id=3, Title="Digital Commerce", Body="Training on e-commerce, social media and international marketplaces.", IconClass="bi-globe2", DisplayOrder=3, IsActive=true },
        new WomenFeature { Id=4, Title="Mentorship", Body="Pairing emerging artisans with established industry leaders.", IconClass="bi-person-hearts", DisplayOrder=4, IsActive=true }
    };

    private static List<IndustryArea> GetMockIndustryAreas() => new()
    {
        new IndustryArea { Id=1, Title="Mining & Extraction", Description="Sustainable extraction of gemstones from KP's rich deposits.", IconClass="bi-gem", DisplayOrder=1, IsActive=true },
        new IndustryArea { Id=2, Title="Gemstone Processing", Description="Cutting, polishing and certification of raw gemstones.", IconClass="bi-scissors", DisplayOrder=2, IsActive=true },
        new IndustryArea { Id=3, Title="Jewellery Manufacturing", Description="Design and handcrafting of fine jewellery.", IconClass="bi-award", DisplayOrder=3, IsActive=true },
        new IndustryArea { Id=4, Title="Digital Commerce", Description="E-commerce and international export channels.", IconClass="bi-globe", DisplayOrder=4, IsActive=true }
    };

    private static List<ContactInquiry> GetMockContactInquiries() => new()
    {
        new ContactInquiry { Id=1, FullName="Ahmed Raza", Email="ahmed@example.com", Phone="0300-1112223", Subject="Membership Inquiry", Message="I would like to know the procedure to become a member of KPGJA.", SubmittedAt=DateTime.UtcNow.AddHours(-3), IsRead=false },
        new ContactInquiry { Id=2, FullName="Sara Khan", Email="sara@jewels.pk", Phone="0333-4445556", Subject="Partnership Proposal", Message="Our company would like to explore a partnership for the upcoming expo.", SubmittedAt=DateTime.UtcNow.AddDays(-1), IsRead=true, IsReplied=true },
        new ContactInquiry { Id=3, FullName="Bilal Ahmad", Email="bilal@tradeco.pk", Phone="0345-7778889", Subject="Export Documentation", Message="Need guidance on export documentation for gemstones to UAE.", SubmittedAt=DateTime.UtcNow.AddDays(-2), IsRead=true },
        new ContactInquiry { Id=4, FullName="Hina Malik", Email="hina@artisan.pk", Phone="0311-2223334", Subject="Women Program Inquiry", Message="I am interested in the women artisan training program. How can I apply?", SubmittedAt=DateTime.UtcNow.AddHours(-6), IsRead=false },
        new ContactInquiry { Id=5, FullName="Kashif Ali", Email="kashif@gems.com", Phone="0301-5556667", Subject="General Question", Message="What are the office timings of KPGJA?", SubmittedAt=DateTime.UtcNow.AddDays(-4), IsRead=true, IsReplied=true }
    };
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

        var slides = GetMockHeroSlides();
        var objectives = GetMockHomeSections("objectives");
        var quickLinks = GetMockHomeSections("quicklinks");

        ViewBag.Slides = slides;
        ViewBag.Objectives = objectives;
        ViewBag.QuickLinks = quickLinks;

        ViewBag.CurrentSlide = id.HasValue && showSlideForm
            ? slides.FirstOrDefault(s => s.Id == id.Value)
            : null;

        ViewBag.CurrentObjective = id.HasValue && showObjectiveForm
            ? objectives.FirstOrDefault(o => o.Id == id.Value)
            : null;

        ViewBag.CurrentQuickLink = id.HasValue && showQuickLinkForm
            ? quickLinks.FirstOrDefault(q => q.Id == id.Value)
            : null;

        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult HomeSlideSave(HeroSlide model)
    {
        TempData["Success"] = model.Id > 0
            ? $"Slide \"{model.Title}\" updated (preview only)."
            : $"Slide \"{model.Title}\" added (preview only).";
        return RedirectToAction(nameof(HomeContent));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult HomeSlideDelete(int id)
    {
        TempData["Success"] = $"Slide #{id} deleted (preview only).";
        return RedirectToAction(nameof(HomeContent));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult HomeObjectiveSave(HomeSection model)
    {
        TempData["Success"] = model.Id > 0
            ? $"Objective \"{model.Title}\" updated (preview only)."
            : $"Objective \"{model.Title}\" added (preview only).";
        return RedirectToAction(nameof(HomeContent));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult HomeObjectiveDelete(int id)
    {
        TempData["Success"] = $"Objective #{id} deleted (preview only).";
        return RedirectToAction(nameof(HomeContent));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult HomeQuickLinkSave(HomeSection model)
    {
        TempData["Success"] = model.Id > 0
            ? $"Quick link \"{model.Title}\" updated (preview only)."
            : $"Quick link \"{model.Title}\" added (preview only).";
        return RedirectToAction(nameof(HomeContent));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult HomeQuickLinkDelete(int id)
    {
        TempData["Success"] = $"Quick link #{id} deleted (preview only).";
        return RedirectToAction(nameof(HomeContent));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public IActionResult HomeCtaSave()
    {
        TempData["Success"] = "Membership CTA banner saved (preview only).";
        return RedirectToAction(nameof(HomeContent));
    }

    // Mock data helpers for HomeSections
    private static List<HomeSection> GetMockHomeSections(string sectionKey) => sectionKey switch
    {
        "objectives" => new()
        {
            new HomeSection { Id=1, SectionKey="objectives", Title="Promote Industry",    Body="Promote the gem and jewellery sector across KP.", IconClass="bi-gem",         DisplayOrder=1, IsActive=true },
            new HomeSection { Id=2, SectionKey="objectives", Title="Skill Development",   Body="Training and mentorship for artisans.",          IconClass="bi-mortarboard", DisplayOrder=2, IsActive=true },
            new HomeSection { Id=3, SectionKey="objectives", Title="Market Access",       Body="Connect members with domestic and international markets.", IconClass="bi-globe",    DisplayOrder=3, IsActive=true },
            new HomeSection { Id=4, SectionKey="objectives", Title="Policy Advocacy",     Body="Represent the industry to government bodies.",   IconClass="bi-megaphone",   DisplayOrder=4, IsActive=true }
        },
        "quicklinks" => new()
        {
            new HomeSection { Id=1, SectionKey="quicklinks", Title="Membership", Body="/Member/Register", IconClass="bi-person-plus",    DisplayOrder=1, IsActive=true },
            new HomeSection { Id=2, SectionKey="quicklinks", Title="Events",     Body="/Home/Events",     IconClass="bi-calendar-event", DisplayOrder=2, IsActive=true },
            new HomeSection { Id=3, SectionKey="quicklinks", Title="News",       Body="/Home/News",       IconClass="bi-newspaper",      DisplayOrder=3, IsActive=true },
            new HomeSection { Id=4, SectionKey="quicklinks", Title="Contact",    Body="/Home/Contact",    IconClass="bi-envelope",       DisplayOrder=4, IsActive=true }
        },
        _ => new()
    };
    private static List<HeroSlide> GetMockHeroSlides() => new()
    {
        new HeroSlide { Id=1, Title="Empowering Pakistan's Gem & Jewellery Industry", Subtitle="Uniting stakeholders across Khyber Pakhtunkhwa", CtaText="Become a Member", CtaLink="/Member/Register", DisplayOrder=1, IsActive=true },
        new HeroSlide { Id=2, Title="From Mines to Markets",                         Subtitle="Supporting the full gemstone value chain",       CtaText="Learn More",      CtaLink="/Home/About",      DisplayOrder=2, IsActive=true },
        new HeroSlide { Id=3, Title="Women Leading the Craft",                       Subtitle="Championing women artisans across KP",          CtaText="Explore Program", CtaLink="/Home/Women",      DisplayOrder=3, IsActive=false }
    };
}