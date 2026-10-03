using Microsoft.AspNetCore.Mvc;

namespace KPGJA.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => View();
    public IActionResult About() => View();
    public IActionResult Membership() => View();
    public IActionResult Executive() => View();
    public IActionResult Women() => View();
    public IActionResult Industry() => View();
    public IActionResult Events() => View();
    public IActionResult News() => View();
    public IActionResult Contact() => View();
    public IActionResult Error() => View();
}
