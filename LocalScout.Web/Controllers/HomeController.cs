using System.Diagnostics;
using LocalScout.Application.Interfaces; // Required for Repository
using LocalScout.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace LocalScout.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IServiceCategoryRepository _serviceCategoryRepository; // Inject Repository

        public HomeController(
            ILogger<HomeController> logger,
            IServiceCategoryRepository serviceCategoryRepository
        )
        {
            _logger = logger;
            _serviceCategoryRepository = serviceCategoryRepository;
        }

        public async Task<IActionResult> Index()
        {
            // Fetch active/approved categories for the carousel
            var categories = await _serviceCategoryRepository.GetActiveAndApprovedCategoryAsync();
            return View(categories);
        }

        public IActionResult Search(string query)
        {
            // Redirect or handle search logic here
            // For now, just returning the view or redirection
            return RedirectToAction("Index"); // Placeholder
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(
                new ErrorViewModel
                {
                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                }
            );
        }
    }
}
