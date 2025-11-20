using LocalScout.Infrastructure.Constants; // Ensure this namespace is correct for RoleNames
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocalScout.Web.Controllers
{
    [Authorize(Roles = RoleNames.Admin)] // Restrict access to Admins only
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
