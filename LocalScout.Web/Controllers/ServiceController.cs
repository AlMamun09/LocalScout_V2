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

        // GET: Service/MyServices (List provider's services)
        public async Task<IActionResult> MyServices()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Account", new { area = "Identity" });
                }

                var services = await _serviceRepository.GetServiceByProviderAsync(userId);
                var serviceDtos = services.Select(MapToDto).ToList();

                return View(serviceDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading services for provider");
                TempData["ErrorMessage"] = "Failed to load services.";
                return View(new List<ServiceDto>());
            }
        }

        // GET: Service/Get/{id} (Get single service)
        [HttpGet]
        public async Task<IActionResult> Get(Guid id)
        {
            try
            {
                var service = await _serviceRepository.GetServiceByIdAsync(id);
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

                var dto = MapToDto(service);
                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving service {ServiceId}", id);
                return StatusCode(500, new { message = "Failed to retrieve service." });
            }
        }

        // GET: Service/Create (Show create form)
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = await _categoryRepository.GetActiveAndApprovedCategoryAsync();
            return View(new ServiceDto());
        }

        // POST: Service/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ServiceDto dto, List<IFormFile>? images)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ViewBag.Categories = await _categoryRepository.GetActiveAndApprovedCategoryAsync();
                    TempData["ErrorMessage"] = "Please correct the errors and try again.";
                    return View(dto);
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return RedirectToAction("Login", "Account", new { area = "Identity" });
                }

                // Handle file uploads
                var imagePaths = new List<string>();
                if (images != null && images.Any())
                {
                    imagePaths = await SaveImagesAsync(images);
                }

                // Map DTO to Entity
                var service = MapToEntity(dto);
                service.Id = userId;
                service.ImagePaths = JsonSerializer.Serialize(imagePaths);

                await _serviceRepository.AddServiceAsync(service);

                TempData["SuccessMessage"] = "Service created successfully!";
                return RedirectToAction(nameof(MyServices));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating service");
                ViewBag.Categories = await _categoryRepository.GetActiveAndApprovedCategoryAsync();
                TempData["ErrorMessage"] = "Failed to create service.";
                return View(dto);
            }
        }

        // GET: Service/Edit/{id}
        public async Task<IActionResult> Edit(Guid id)
        {
            try
            {
                var service = await _serviceRepository.GetServiceByIdAsync(id);
                if (service == null)
                {
                    TempData["ErrorMessage"] = "Service not found.";
                    return RedirectToAction(nameof(MyServices));
                }

                // Verify ownership
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (service.Id != userId)
                {
                    TempData["ErrorMessage"] = "You don't have permission to edit this service.";
                    return RedirectToAction(nameof(MyServices));
                }

                ViewBag.Categories = await _categoryRepository.GetActiveAndApprovedCategoryAsync();
                ViewBag.ExistingImages = GetImagePathsFromJson(service.ImagePaths);

                var dto = MapToDto(service);
                return View(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading service for edit {ServiceId}", id);
                TempData["ErrorMessage"] = "Failed to load service.";
                return RedirectToAction(nameof(MyServices));
            }
        }

        // POST: Service/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ServiceDto dto, List<IFormFile>? newImages, string? existingImagePaths)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    ViewBag.Categories = await _categoryRepository.GetActiveAndApprovedCategoryAsync();
                    TempData["ErrorMessage"] = "Please correct the errors and try again.";
                    return View(dto);
                }

                var service = await _serviceRepository.GetServiceByIdAsync(dto.ServiceId);
                if (service == null)
                {
                    TempData["ErrorMessage"] = "Service not found.";
                    return RedirectToAction(nameof(MyServices));
                }

                // Verify ownership
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (service.Id != userId)
                {
                    TempData["ErrorMessage"] = "You don't have permission to edit this service.";
                    return RedirectToAction(nameof(MyServices));
                }

                // Handle image paths
                var imagePaths = new List<string>();

                // Add existing images that weren't removed
                if (!string.IsNullOrEmpty(existingImagePaths))
                {
                    try
                    {
                        var existingPaths = JsonSerializer.Deserialize<List<string>>(existingImagePaths);
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

                // Add new uploaded images
                if (newImages != null && newImages.Any())
                {
                    var newPaths = await SaveImagesAsync(newImages);
                    imagePaths.AddRange(newPaths);
                }

                // Update service properties
                service.ServiceName = dto.ServiceName;
                service.Description = dto.Description;
                service.ServiceCategoryId = dto.ServiceCategoryId;
                service.PricingUnit = dto.PricingUnit;
                service.Price = dto.Price;
                service.ImagePaths = JsonSerializer.Serialize(imagePaths);

                await _serviceRepository.UpdateServiceAsync(service);

                TempData["SuccessMessage"] = "Service updated successfully!";
                return RedirectToAction(nameof(MyServices));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating service {ServiceId}", dto.ServiceId);
                ViewBag.Categories = await _categoryRepository.GetActiveAndApprovedCategoryAsync();
                TempData["ErrorMessage"] = "Failed to update service.";
                return View(dto);
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
        private ServiceDto MapToDto(Service service)
        {
            return new ServiceDto
            {
                ServiceId = service.ServiceId,
                Id = service.Id,
                ServiceCategoryId = service.ServiceCategoryId,
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

        // Helper: Map DTO to Entity
        private Service MapToEntity(ServiceDto dto)
        {
            return new Service
            {
                ServiceId = dto.ServiceId,
                Id = dto.Id,
                ServiceCategoryId = dto.ServiceCategoryId,
                ServiceName = dto.ServiceName,
                Description = dto.Description,
                PricingUnit = dto.PricingUnit,
                Price = dto.Price,
                ImagePaths = dto.ImagePaths,
                IsActive = dto.IsActive,
                IsDeleted = dto.IsDeleted
            };
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
                    if (image.Length > 5 * 1024 * 1024)
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

        // Helper: Get image paths from JSON string
        private List<string> GetImagePathsFromJson(string? jsonPaths)
        {
            if (string.IsNullOrEmpty(jsonPaths))
            {
                return new List<string>();
            }

            try
            {
                return JsonSerializer.Deserialize<List<string>>(jsonPaths) ?? new List<string>();
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize image paths");
                return new List<string>();
            }
        }
    }
}
