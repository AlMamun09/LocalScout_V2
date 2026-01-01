using System.Diagnostics;
using System.Text.Json;
using System.Linq;
using LocalScout.Application.DTOs;
using LocalScout.Application.Interfaces;
using LocalScout.Application.Utilities;
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
        private readonly IReviewRepository _reviewRepository;

        public HomeController(
            ILogger<HomeController> logger,
            IServiceCategoryRepository serviceCategoryRepository,
            IServiceRepository serviceRepository,
            UserManager<ApplicationUser> userManager,
            IReviewRepository reviewRepository
        )
        {
            _logger = logger;
            _serviceCategoryRepository = serviceCategoryRepository;
            _serviceRepository = serviceRepository;
            _userManager = userManager;
            _reviewRepository = reviewRepository;
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
                // Get user's location from parameters or from logged in user
                double? userLat = latitude;
                double? userLon = longitude;

                if ((!userLat.HasValue || !userLon.HasValue) && User.Identity?.IsAuthenticated == true)
                {
                    var currentUser = await _userManager.GetUserAsync(User);
                    if (currentUser != null)
                    {
                        userLat = currentUser.Latitude;
                        userLon = currentUser.Longitude;
                    }
                }

                var services = await _serviceRepository.GetNearbyServicesAsync(userLat, userLon, count);
                var serviceCards = await BuildServiceCardsAsync(services, userLat, userLon);

                // Sort by distance if we have user location
                if (userLat.HasValue && userLon.HasValue)
                {
                    serviceCards = serviceCards
                        .OrderBy(s => s.DistanceInKm ?? double.MaxValue)
                        .ToList();
                }

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
                // Get user's location if authenticated
                double? userLat = null;
                double? userLon = null;

                if (User.Identity?.IsAuthenticated == true)
                {
                    var currentUser = await _userManager.GetUserAsync(User);
                    if (currentUser != null)
                    {
                        userLat = currentUser.Latitude;
                        userLon = currentUser.Longitude;
                    }
                }

                var services = await _serviceRepository.SearchServicesAsync(query, categoryId, take);
                var serviceCards = await BuildServiceCardsAsync(services, userLat, userLon);

                // Sort by distance if we have user location
                if (userLat.HasValue && userLon.HasValue)
                {
                    serviceCards = serviceCards
                        .OrderBy(s => s.DistanceInKm ?? double.MaxValue)
                        .ToList();
                }

                return Json(new { success = true, data = serviceCards });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching services with query {Query} and category {CategoryId}", query, categoryId);
                return StatusCode(500, new { success = false, message = "Failed to search services" });
            }
        }

        private async Task<List<ServiceCardDto>> BuildServiceCardsAsync(IEnumerable<Service> services, double? userLat = null, double? userLon = null)
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

                // Calculate distance using Haversine formula
                var distance = DistanceCalculator.CalculateDistance(userLat, userLon, provider.Latitude, provider.Longitude);

                // Calculate dynamic rating from reviews
                var reviews = await _reviewRepository.GetReviewsByServiceIdAsync(service.ServiceId);
                var activeReviews = reviews.Where(r => !r.IsDeleted).ToList();
                double? averageRating = null;
                if (activeReviews.Any())
                {
                    averageRating = activeReviews.Average(r => r.Rating);
                }

                cards.Add(new ServiceCardDto
                {
                    ServiceId = service.ServiceId,
                    ServiceName = service.ServiceName ?? "Unnamed Service",
                    CategoryName = categoryDict.GetValueOrDefault(service.ServiceCategoryId) ?? "General",
                    Description = service.Description,
                    MinPrice = service.MinPrice,
                    PricingUnit = service.PricingUnit ?? "Fixed",
                    FirstImagePath = firstImage,
                    CreatedAt = service.CreatedAt,
                    ProviderId = provider.Id,
                    ProviderName = provider.FullName ?? "Unknown Provider",
                    ProviderLocation = provider.Address,
                    ProviderLatitude = provider.Latitude,
                    ProviderLongitude = provider.Longitude,
                    ProviderJoinedDate = provider.CreatedAt,
                    WorkingDays = provider.WorkingDays,
                    WorkingHours = provider.WorkingHours,
                    Rating = averageRating,
                    DistanceInKm = distance
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
