using LocalScout.Application.DTOs;
using LocalScout.Application.Interfaces;
using LocalScout.Domain.Entities;
using LocalScout.Infrastructure.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace LocalScout.Web.Controllers
{
    [Authorize(Roles = RoleNames.ServiceProvider)]
    public class ServiceController : Controller
    {
        private readonly IServiceRepository _serviceRepository;
        private readonly IServiceCategoryRepository _categoryRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ServiceController> _logger;

        // Constants
        private const int MaxImagesPerService = 10;
        private const long MaxImageSizeBytes = 5 * 1024 * 1024; // 5MB

        public ServiceController(
            IServiceRepository serviceRepository,
            IServiceCategoryRepository categoryRepository,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment,
            ILogger<ServiceController> logger)
        {
            _serviceRepository = serviceRepository;
            _categoryRepository = categoryRepository;
            _userManager = userManager;
            _environment = environment;
            _logger = logger;
        }

        // GET: Service/MyServices - Redirects to ActiveServices
        public IActionResult MyServices()
        {
            return RedirectToAction(nameof(ActiveServices));
        }

        // GET: Service/Details/{id} - Public service details page
        [AllowAnonymous]
        public async Task<IActionResult> Details(Guid id)
        {
            try
            {
                var service = await _serviceRepository.GetServiceByIdAsync(id);
                if (service == null || !service.IsActive || service.IsDeleted)
                {
                    return NotFound();
                }

                // Get provider information
                var provider = await _userManager.FindByIdAsync(service.Id ?? "");
                if (provider == null || !provider.IsActive)
                {
                    return NotFound();
                }

                // Get category information
                var category = await _categoryRepository.GetCategoryByIdAsync(service.ServiceCategoryId);
                var categoryName = category?.CategoryName ?? "General";

                // Parse image paths
                var imagePaths = new List<string>();
                if (!string.IsNullOrEmpty(service.ImagePaths))
                {
                    try
                    {
                        imagePaths = JsonSerializer.Deserialize<List<string>>(service.ImagePaths) ?? new List<string>();
                    }
                    catch { }
                }

                // Get other services by same provider
                var otherProviderServices = await _serviceRepository.GetOtherServicesByProviderAsync(
                    service.Id ?? "", service.ServiceId, 4);
                
                // Get related services (same category, different provider)
                var relatedServices = await _serviceRepository.GetRelatedServicesAsync(
                    service.ServiceCategoryId, service.Id ?? "", 6);

                // Build category dictionary for service cards
                var categories = await _categoryRepository.GetActiveAndApprovedCategoryAsync();
                var categoryDict = categories.ToDictionary(c => c.ServiceCategoryId, c => c.CategoryName);

                // Build the DTO
                var dto = new ServiceDetailsDto
                {
                    ServiceId = service.ServiceId,
                    ServiceName = service.ServiceName ?? "Unnamed Service",
                    Description = service.Description,
                    Price = service.Price,
                    PricingUnit = service.PricingUnit ?? "Fixed",
                    ImagePaths = imagePaths,
                    CreatedAt = service.CreatedAt,
                    CategoryId = service.ServiceCategoryId,
                    CategoryName = categoryName,
                    ProviderId = provider.Id,
                    ProviderName = provider.FullName ?? "Unknown Provider",
                    ProviderBusinessName = provider.BusinessName,
                    ProviderProfilePicture = provider.ProfilePictureUrl,
                    ProviderDescription = provider.Description,
                    ProviderLocation = provider.Address,
                    ProviderPhone = provider.PhoneNumber,
                    WorkingDays = provider.WorkingDays,
                    WorkingHours = provider.WorkingHours,
                    ProviderJoinedDate = provider.CreatedAt,
                    IsProviderVerified = provider.IsVerified,
                    Rating = 4.6, // Placeholder
                    ReviewCount = 0, // Placeholder
                    OtherProviderServices = await BuildServiceCardsAsync(otherProviderServices, categoryDict),
                    RelatedServices = await BuildServiceCardsAsync(relatedServices, categoryDict)
                };

                return View(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading service details for {ServiceId}", id);
                return StatusCode(500);
            }
        }

        // Helper method to build service cards from services
        private async Task<List<ServiceCardDto>> BuildServiceCardsAsync(IEnumerable<Service> services, Dictionary<Guid, string?> categoryDict)
        {
            var cards = new List<ServiceCardDto>();

            foreach (var service in services)
            {
                if (string.IsNullOrEmpty(service.Id)) continue;

                var provider = await _userManager.FindByIdAsync(service.Id);
                if (provider == null || !provider.IsActive) continue;

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
                    Rating = 4.6
                });
            }

            return cards;
        }

        // GET: Service/ActiveServices
        public async Task<IActionResult> ActiveServices()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Account", new { area = "Identity" });
                }

                var services = await _serviceRepository.GetActiveServicesByProviderAsync(userId);
                var categories = await _categoryRepository.GetActiveAndApprovedCategoryAsync();
                var categoryDict = categories.ToDictionary(c => c.ServiceCategoryId, c => c.CategoryName);

                var serviceDtos = services.Select(s => MapToDto(s, categoryDict)).ToList();

                ViewData["Title"] = "Active Services";
                ViewData["IsActive"] = true;
                return View("MyServices", serviceDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading active services for provider");
                TempData["ErrorMessage"] = "Failed to load services.";
                return View("MyServices", new List<ServiceDto>());
            }
        }

        // GET: Service/InactiveServices
        public async Task<IActionResult> InactiveServices()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Account", new { area = "Identity" });
                }

                var services = await _serviceRepository.GetInactiveServicesByProviderAsync(userId);
                var categories = await _categoryRepository.GetActiveAndApprovedCategoryAsync();
                var categoryDict = categories.ToDictionary(c => c.ServiceCategoryId, c => c.CategoryName);

                var serviceDtos = services.Select(s => MapToDto(s, categoryDict)).ToList();

                ViewData["Title"] = "Inactive Services";
                ViewData["IsActive"] = false;
                return View("MyServices", serviceDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading inactive services for provider");
                TempData["ErrorMessage"] = "Failed to load services.";
                return View("MyServices", new List<ServiceDto>());
            }
        }

        // GET: Service/GetServicesTableData (AJAX - returns JSON for table rebuild)
        [HttpGet]
        public async Task<IActionResult> GetServicesTableData(bool isActive)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var services = isActive
                    ? await _serviceRepository.GetActiveServicesByProviderAsync(userId)
                    : await _serviceRepository.GetInactiveServicesByProviderAsync(userId);

                var categories = await _categoryRepository.GetActiveAndApprovedCategoryAsync();
                var categoryDict = categories.ToDictionary(c => c.ServiceCategoryId, c => c.CategoryName);

                var serviceDtos = services.Select(s => new
                {
                    serviceId = s.ServiceId,
                    serviceName = s.ServiceName,
                    description = s.Description,
                    categoryName = categoryDict.GetValueOrDefault(s.ServiceCategoryId) ?? "Unknown",
                    price = s.Price,
                    pricingUnit = s.PricingUnit ?? "Fixed",
                    createdAt = s.CreatedAt.ToString("MMM dd, yyyy"),
                    firstImage = GetFirstImagePath(s.ImagePaths),
                    isActive = s.IsActive
                }).ToList();

                return Json(new { success = true, data = serviceDtos });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading services table data");
                return StatusCode(500, new { success = false, message = "Failed to load services." });
            }
        }

        // GET: Service/GetCreateOrEditModal
        [HttpGet]
        public async Task<IActionResult> GetCreateOrEditModal(Guid? id)
        {
            try
            {
                ViewBag.Categories = await _categoryRepository.GetActiveAndApprovedCategoryAsync();

                if (id == null || id == Guid.Empty)
                {
                    return PartialView("_CreateEditModal", new ServiceDto());
                }

                var service = await _serviceRepository.GetServiceByIdAsync(id.Value);
                if (service == null)
                {
                    return NotFound(new { message = "Service not found." });
                }

                // Verify ownership
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (service.Id != userId)
                {
                    return Forbid();
                }

                var dto = MapToDto(service, null);
                return PartialView("_CreateEditModal", dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading service modal for {ServiceId}", id);
                return StatusCode(500, new { message = "Failed to load service data." });
            }
        }

        // POST: Service/SaveService (Create or Update via AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveService(ServiceDto dto, List<IFormFile>? images, string? existingImageUrls)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User not authenticated." });
                }

                // Check if provider is verified
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Unauthorized(new { message = "User not found." });
                }

                if (!user.IsVerified)
                {
                    return BadRequest(new
                    {
                        message = "You must be verified before creating services. Please submit verification documents first.",
                        requiresVerification = true
                    });
                }

                // Basic validation
                if (string.IsNullOrWhiteSpace(dto.ServiceName))
                {
                    return BadRequest(new { message = "Service name is required." });
                }

                if (dto.ServiceCategoryId == Guid.Empty)
                {
                    return BadRequest(new { message = "Please select a category." });
                }

                if (dto.Price <= 0)
                {
                    return BadRequest(new { message = "Price must be greater than zero." });
                }

                // Handle image paths
                var imagePaths = new List<string>();

                // Add existing images that weren't removed
                if (!string.IsNullOrEmpty(existingImageUrls))
                {
                    try
                    {
                        var existingPaths = JsonSerializer.Deserialize<List<string>>(existingImageUrls);
                        if (existingPaths != null)
                        {
                            imagePaths.AddRange(existingPaths);
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Failed to deserialize existing image paths");
                    }
                }

                // Calculate how many new images can be added
                var availableSlots = MaxImagesPerService - imagePaths.Count;

                // Add new uploaded images (limited by available slots)
                if (images != null && images.Any())
                {
                    if (images.Count > availableSlots)
                    {
                        _logger.LogWarning("Too many images uploaded. Limit is {MaxImages}, existing: {Existing}, new: {New}",
                            MaxImagesPerService, imagePaths.Count, images.Count);
                    }

                    var imagesToProcess = images.Take(availableSlots).ToList();
                    var newPaths = await SaveImagesAsync(imagesToProcess);
                    imagePaths.AddRange(newPaths);
                }

                // Final validation - ensure total doesn't exceed limit
                if (imagePaths.Count > MaxImagesPerService)
                {
                    return BadRequest(new { message = $"Maximum {MaxImagesPerService} images allowed per service." });
                }

                if (dto.ServiceId == Guid.Empty)
                {
                    // Create new service
                    var service = new Service
                    {
                        ServiceId = Guid.NewGuid(),
                        Id = userId,
                        ServiceCategoryId = dto.ServiceCategoryId,
                        ServiceName = dto.ServiceName,
                        Description = dto.Description,
                        PricingUnit = dto.PricingUnit ?? "Fixed",
                        Price = dto.Price,
                        ImagePaths = JsonSerializer.Serialize(imagePaths),
                        IsActive = dto.IsActive,
                        IsDeleted = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _serviceRepository.AddServiceAsync(service);
                    return Json(new { success = true, message = "Service created successfully!" });
                }
                else
                {
                    // Update existing service
                    var service = await _serviceRepository.GetServiceByIdAsync(dto.ServiceId);
                    if (service == null)
                    {
                        return NotFound(new { message = "Service not found." });
                    }

                    // Verify ownership
                    if (service.Id != userId)
                    {
                        return Forbid();
                    }

                    service.ServiceCategoryId = dto.ServiceCategoryId;
                    service.ServiceName = dto.ServiceName;
                    service.Description = dto.Description;
                    service.PricingUnit = dto.PricingUnit ?? "Fixed";
                    service.Price = dto.Price;
                    service.ImagePaths = JsonSerializer.Serialize(imagePaths);
                    service.IsActive = dto.IsActive;

                    await _serviceRepository.UpdateServiceAsync(service);
                    return Json(new { success = true, message = "Service updated successfully!" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving service: {Message}", ex.Message);
                return StatusCode(500, new { message = "Failed to save service." });
            }
        }

        // POST: Service/SoftDelete/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SoftDelete(Guid id)
        {
            try
            {
                var service = await _serviceRepository.GetServiceByIdAsync(id);
                if (service == null)
                {
                    return BadRequest(new { message = "Service not found." });
                }

                // Verify ownership
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (service.Id != userId)
                {
                    return Forbid();
                }

                await _serviceRepository.SoftDeleteServiceAsync(id);

                return Ok(new { success = true, message = "Service deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting service {ServiceId}", id);
                return StatusCode(500, new { message = "Failed to delete service." });
            }
        }

        // POST: Service/ToggleActive/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(Guid id)
        {
            try
            {
                var service = await _serviceRepository.GetServiceByIdAsync(id);
                if (service == null)
                {
                    return BadRequest(new { message = "Service not found." });
                }

                // Verify ownership
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (service.Id != userId)
                {
                    return Forbid();
                }

                service.IsActive = !service.IsActive;
                await _serviceRepository.UpdateServiceAsync(service);

                var status = service.IsActive ? "activated" : "deactivated";
                return Ok(new { success = true, message = $"Service {status} successfully.", isActive = service.IsActive });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling service status {ServiceId}", id);
                return StatusCode(500, new { message = "Failed to update service status." });
            }
        }

        // Helper: Map Entity to DTO
        private ServiceDto MapToDto(Service service, Dictionary<Guid, string?>? categoryDict)
        {
            return new ServiceDto
            {
                ServiceId = service.ServiceId,
                Id = service.Id,
                ServiceCategoryId = service.ServiceCategoryId,
                CategoryName = categoryDict?.GetValueOrDefault(service.ServiceCategoryId) ?? "Unknown",
                ServiceName = service.ServiceName,
                Description = service.Description,
                PricingUnit = service.PricingUnit,
                Price = service.Price,
                ImagePaths = service.ImagePaths,
                IsActive = service.IsActive,
                IsDeleted = service.IsDeleted,
                CreatedAt = service.CreatedAt,
                UpdatedAt = service.UpdatedAt
            };
        }

        // Helper: Get first image path from JSON
        private string? GetFirstImagePath(string? imagePaths)
        {
            if (string.IsNullOrEmpty(imagePaths)) return null;

            try
            {
                var paths = JsonSerializer.Deserialize<List<string>>(imagePaths);
                return paths?.FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        // Helper: Save uploaded images
        private async Task<List<string>> SaveImagesAsync(List<IFormFile> images)
        {
            var imagePaths = new List<string>();
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "services");

            // Create directory if it doesn't exist
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            foreach (var image in images)
            {
                if (image != null && image.Length > 0)
                {
                    // Validate file type
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                    var extension = Path.GetExtension(image.FileName).ToLower();

                    if (!allowedExtensions.Contains(extension))
                    {
                        _logger.LogWarning("Invalid file type: {FileName}", image.FileName);
                        continue;
                    }

                    // Validate file size (max 5MB)
                    if (image.Length > MaxImageSizeBytes)
                    {
                        _logger.LogWarning("File too large: {FileName}", image.FileName);
                        continue;
                    }

                    // Generate unique filename
                    var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(image.FileName)}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    // Save file
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await image.CopyToAsync(fileStream);
                    }

                    // Store relative path
                    imagePaths.Add($"/uploads/services/{uniqueFileName}");
                }
            }

            return imagePaths;
        }
    }
}
