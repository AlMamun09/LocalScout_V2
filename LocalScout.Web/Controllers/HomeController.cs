using System.Diagnostics;
using System.Text.Json;
using System.Linq;
using LocalScout.Application.DTOs;
using LocalScout.Application.Interfaces;
using LocalScout.Domain.Entities;
using LocalScout.Web.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LocalScout.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IServiceCategoryRepository _serviceCategoryRepository;
        private readonly IServiceRepository _serviceRepository;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(
            ILogger<HomeController> logger,
            IServiceCategoryRepository serviceCategoryRepository,
            IServiceRepository serviceRepository,
            UserManager<ApplicationUser> userManager
        )
        {
            _logger = logger;
            _serviceCategoryRepository = serviceCategoryRepository;
            _serviceRepository = serviceRepository;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            // Fetch active/approved categories for the carousel
            var categories = await _serviceCategoryRepository.GetActiveAndApprovedCategoryAsync();
            return View(categories);
        }

        [HttpGet]
        public async Task<IActionResult> GetNearbyServices(double? latitude, double? longitude, int count = 20)
        {
            try
            {
                var services = await _serviceRepository.GetNearbyServicesAsync(latitude, longitude, count);
                var serviceCards = await BuildServiceCardsAsync(services);
                return Json(new { success = true, data = serviceCards });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching nearby services");
                return StatusCode(500, new { success = false, message = "Failed to load services" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> SearchServices(string? query, Guid? categoryId, int take = 20)
        {
            try
            {
                var services = await _serviceRepository.SearchServicesAsync(query, categoryId, take);
                var serviceCards = await BuildServiceCardsAsync(services);
                return Json(new { success = true, data = serviceCards });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching services with query {Query} and category {CategoryId}", query, categoryId);
                return StatusCode(500, new { success = false, message = "Failed to search services" });
            }
        }

        private async Task<List<ServiceCardDto>> BuildServiceCardsAsync(IEnumerable<Service> services)
        {
            var cards = new List<ServiceCardDto>();
            var categories = await _serviceCategoryRepository.GetActiveAndApprovedCategoryAsync();
            var categoryDict = categories.ToDictionary(c => c.ServiceCategoryId, c => c.CategoryName);

            foreach (var service in services)
            {
                if (string.IsNullOrEmpty(service.Id))
                {
                    continue;
                }

                var provider = await _userManager.FindByIdAsync(service.Id);

                // Skip if provider doesn't exist or is blocked (IsActive = false)
                if (provider == null || !provider.IsActive)
                {
                    continue;
                }

                var firstImage = GetFirstImagePath(service.ImagePaths);

                cards.Add(new ServiceCardDto
                {
                    ServiceId = service.ServiceId,
                    ServiceName = service.ServiceName ?? "Unnamed Service",
                    CategoryName = categoryDict.GetValueOrDefault(service.ServiceCategoryId) ?? "General",
                    Description = service.Description,
                    Price = service.Price,
                    PricingUnit = service.PricingUnit ?? "Fixed",
                    FirstImagePath = firstImage,
                    CreatedAt = service.CreatedAt,
                    ProviderId = provider.Id,
                    ProviderName = provider.FullName ?? "Unknown Provider",
                    ProviderLocation = provider.Address,
                    ProviderJoinedDate = provider.CreatedAt,
                    WorkingDays = provider.WorkingDays,
                    WorkingHours = provider.WorkingHours,
                    Rating = 4.6 // TODO: Implement actual rating system
                });
            }

            return cards;
        }

        private string? GetFirstImagePath(string? imagePaths)
        {
            if (string.IsNullOrEmpty(imagePaths)) return "/images/placeholder-service.jpg";
            
            try
            {
                var paths = JsonSerializer.Deserialize<List<string>>(imagePaths);
                return paths?.FirstOrDefault() ?? "/images/placeholder-service.jpg";
            }
            catch
            {
                return "/images/placeholder-service.jpg";
            }
        }

        public async Task<IActionResult> Search(string? query, Guid? categoryId)
        {
            var categories = await _serviceCategoryRepository.GetActiveAndApprovedCategoryAsync();
            ServiceCategory? selectedCategory = null;

            if (categoryId.HasValue)
            {
                selectedCategory = categories.FirstOrDefault(c => c.ServiceCategoryId == categoryId.Value);
            }

            var viewModel = new SearchPageDto
            {
                Categories = categories,
                Query = string.IsNullOrWhiteSpace(query) ? null : query.Trim(),
                SelectedCategoryId = selectedCategory?.ServiceCategoryId,
                SelectedCategoryName = selectedCategory?.CategoryName
            };

            return View(viewModel);
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
