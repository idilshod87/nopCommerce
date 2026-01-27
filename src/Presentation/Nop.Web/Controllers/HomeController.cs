using Microsoft.AspNetCore.Mvc;

namespace Nop.Web.Controllers;

public partial class HomeController : BasePublicController
{
    public virtual IActionResult Index()
    {
        // Redirect to admin panel as the site is only for vendors and administrators
        // The marketplace itself functions in the mobile application
        return RedirectToAction("Index", "Home", new { area = "Admin" });
    }
}