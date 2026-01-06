using LocalScout.Application.DTOs;
using LocalScout.Application.Interfaces;
using LocalScout.Domain.Entities;
using LocalScout.Domain.Enums;
using LocalScout.Infrastructure.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace LocalScout.Web.Controllers
{
    public class CategoryRequestController : Controller
    {
        private readonly ICategoryRequestRepository _categoryRequestRepo;
        private readonly IServiceCategoryRepository _categoryRepo;
        private readonly IServiceProviderRepository _providerRepo;
        private readonly INotificationRepository _notificationRepo;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<CategoryRequestController> _logger;
        private readonly IAuditService _auditService;

        public CategoryRequestController(
            ICategoryRequestRepository categoryRequestRepo,
            IServiceCategoryRepository categoryRepo,
            IServiceProviderRepository providerRepo,
            INotificationRepository notificationRepo,
            UserManager<ApplicationUser> userManager,
            ILogger<CategoryRequestController> logger,
            IAuditService auditService)
        {
            _categoryRequestRepo = categoryRequestRepo;
            _categoryRepo = categoryRepo;
            _providerRepo = providerRepo;
            _notificationRepo = notificationRepo;
            _userManager = userManager;
            _logger = logger;
            _auditService = auditService;
        }

        // ===== PROVIDER ACTIONS =====

        [Authorize(Roles = RoleNames.ServiceProvider)]
        [HttpGet]
        public async Task<IActionResult> RequestForm()
        {
            // Check if provider is blocked
            var userId = _userManager.GetUserId(User);
            if (!string.IsNullOrEmpty(userId))
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null && !user.IsActive)
                {
                    return BadRequest(new { 
                        message = $"Your account has been blocked. Reason: {user.BlockReason ?? "No reason provided"}. You cannot request new categories.",
                        isBlocked = true
                    });
                }
            }

            return PartialView("~/Views/Provider/_RequestCategoryModal.cshtml", new CategoryRequestDto());
        }

        [Authorize(Roles = RoleNames.ServiceProvider)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitRequest(CategoryRequestDto dto)
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.FindByIdAsync(userId!);

            if (user == null)
                return Unauthorized();

            // Check if provider is blocked
            if (!user.IsActive)
            {
                return BadRequest(new { 
                    message = $"Your account has been blocked. Reason: {user.BlockReason ?? "No reason provided"}. You cannot request new categories.",
                    isBlocked = true
                });
            }

            // Check for duplicate pending request
            if (await _categoryRequestRepo.HasPendingRequestAsync(userId!, dto.RequestedCategoryName))
            {
                return BadRequest(new { message = "You already have a pending request for this category name." });
            }

            // Create the request
            await _categoryRequestRepo.CreateRequestAsync(userId!, user.FullName ?? user.Email!, dto);

            // Notify all admins
            var admins = await _userManager.GetUsersInRoleAsync(RoleNames.Admin);
            foreach (var admin in admins)
            {
                await _notificationRepo.CreateNotificationAsync(
                    admin.Id,
                    "New Category Request",
                    $"{user.FullName ?? user.Email} requested a new service category."
                );
            }

            // Audit Log: Category Request Submitted
            await _auditService.LogAsync(
                userId!,
                user.FullName ?? user.Email!,
                user.Email!,
                "CategoryRequestSubmitted",
                "ProviderManagement",
                "CategoryRequest",
                dto.RequestedCategoryName, // Using name as ID proxy or we'd need the ID from CreateRequestAsync if it returns it
                $"Provider requested new category: {dto.RequestedCategoryName}"
            );

            return Ok(new { success = true, message = "Category request submitted successfully!" });
        }

        [Authorize(Roles = RoleNames.ServiceProvider)]
        public async Task<IActionResult> MyRequests()
        {
            var userId = _userManager.GetUserId(User);
            var requests = await _categoryRequestRepo.GetRequestsByProviderIdAsync(userId!);
            return View("~/Views/Provider/MyCategoryRequests.cshtml", requests);
        }

        // ===== ADMIN ACTIONS =====

        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> PendingRequests()
        {
            ViewData["Title"] = "Category Requests";
            var requests = await _categoryRequestRepo.GetPendingRequestsAsync();

            // Map to DTOs with provider profile pictures
            var dtos = new List<CategoryRequestDto>();
            foreach (var r in requests)
            {
                var provider = await _providerRepo.GetProviderByIdAsync(r.ProviderId);
                dtos.Add(new CategoryRequestDto
                {
                    CategoryRequestId = r.CategoryRequestId,
                    ProviderId = r.ProviderId,
                    ProviderName = r.ProviderName,
                    ProviderProfilePictureUrl = provider?.ProfilePictureUrl,
                    RequestedCategoryName = r.RequestedCategoryName,
                    Description = r.Description,
                    Status = r.Status,
                    CreatedAt = r.CreatedAt
                });
            }

            return View("~/Views/Admin/ServiceCategory/CategoryRequests.cshtml", dtos);
        }

        [Authorize(Roles = RoleNames.Admin)]
        [HttpGet]
        public async Task<IActionResult> GetProviderDetails(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest();

            var provider = await _providerRepo.GetProviderByIdAsync(id);
            if (provider == null)
                return NotFound();

            return PartialView("~/Views/Admin/Provider/_ProviderDetailsPartial.cshtml", provider);
        }

        [Authorize(Roles = RoleNames.Admin)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(Guid id)
        {
            var request = await _categoryRequestRepo.GetByIdAsync(id);
            if (request == null || request.Status != VerificationStatus.Pending)
                return BadRequest(new { message = "Request not found or already processed." });

            // 1. Create the new ServiceCategory
            var newCategory = new ServiceCategory
            {
                ServiceCategoryId = Guid.NewGuid(),
                CategoryName = request.RequestedCategoryName,
                Description = request.Description,
                IsActive = true,
                IsApproved = true,
                CreatedAt = DateTime.UtcNow
            };
            await _categoryRepo.AddCategoryAsync(newCategory);

            // 2. Update request status
            await _categoryRequestRepo.UpdateStatusAsync(id, VerificationStatus.Approved);

            // 3. Notify the provider
            await _notificationRepo.CreateNotificationAsync(
                request.ProviderId,
                "Category Request Approved",
                $"Your request for category \"{request.RequestedCategoryName}\" has been approved."
            );

            // Audit Log: Category Approved
            var adminId = _userManager.GetUserId(User);
            var admin = await _userManager.FindByIdAsync(adminId);
            await _auditService.LogAsync(
                adminId,
                admin?.FullName,
                admin?.Email,
                "CategoryRequestApproved",
                "ProviderManagement",
                "CategoryRequest",
                id.ToString(),
                $"Admin approved category request: {request.RequestedCategoryName}"
            );

            return Ok(new { success = true, message = "Category approved and created successfully!" });
        }

        [Authorize(Roles = RoleNames.Admin)]
        [HttpGet]
        public async Task<IActionResult> GetApproveModal(Guid id)
        {
            var request = await _categoryRequestRepo.GetByIdAsync(id);
            if (request == null || request.Status != VerificationStatus.Pending)
            {
                return NotFound();
            }

            var dto = new ServiceCategoryDto
            {
                CategoryName = request.RequestedCategoryName,
                Description = request.Description
            };

            ViewBag.FormAction = Url.Action("ApproveWithCategory", "CategoryRequest");
            ViewBag.SubmitButtonText = "Approve";
            ViewBag.CategoryRequestId = request.CategoryRequestId.ToString();

            return PartialView("~/Views/Admin/ServiceCategory/_CreateEditModal.cshtml", dto);
        }

        [Authorize(Roles = RoleNames.Admin)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveWithCategory(Guid categoryRequestId, ServiceCategoryDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Invalid data submitted." });
            }

            var request = await _categoryRequestRepo.GetByIdAsync(categoryRequestId);
            if (request == null || request.Status != VerificationStatus.Pending)
            {
                return BadRequest(new { message = "Request not found or already processed." });
            }

            // Handle File Upload (same as SaveCategory)
            if (model.IconFile != null && model.IconFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "categories");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = Guid.NewGuid() + "_" + model.IconFile.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.IconFile.CopyToAsync(fileStream);
                }

                model.IconPath = "/images/categories/" + uniqueFileName;
            }

            var newCategory = new ServiceCategory
            {
                ServiceCategoryId = Guid.NewGuid(),
                CategoryName = model.CategoryName,
                Description = model.Description,
                IconPath = model.IconPath,
                IsActive = true,
                IsApproved = true,
                CreatedAt = DateTime.UtcNow
            };

            await _categoryRepo.AddCategoryAsync(newCategory);
            await _categoryRequestRepo.UpdateStatusAsync(categoryRequestId, VerificationStatus.Approved);

            await _notificationRepo.CreateNotificationAsync(
                request.ProviderId,
                "Category Request Approved",
                $"Your request for category \"{request.RequestedCategoryName}\" has been approved."
            );

            // Audit Log: Category Approved (Custom)
            var adminId = _userManager.GetUserId(User);
            var admin = await _userManager.FindByIdAsync(adminId);
            await _auditService.LogAsync(
                adminId,
                admin?.FullName,
                admin?.Email,
                "CategoryRequestApproved",
                "ProviderManagement",
                "CategoryRequest",
                categoryRequestId.ToString(),
                $"Admin approved category request with custom details: {model.CategoryName}"
            );

            return Json(new { success = true, message = "Category approved and created successfully!" });
        }

        [Authorize(Roles = RoleNames.Admin)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(Guid id, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                return BadRequest(new { message = "Rejection reason is required." });

            var request = await _categoryRequestRepo.GetByIdAsync(id);
            if (request == null || request.Status != VerificationStatus.Pending)
                return BadRequest(new { message = "Request not found or already processed." });

            // 1. Update request status with reason
            await _categoryRequestRepo.UpdateStatusAsync(id, VerificationStatus.Rejected, reason);

            // 2. Notify the provider with rejection reason
            await _notificationRepo.CreateNotificationAsync(
                request.ProviderId,
                "Category Request Rejected",
                $"Your request for category \"{request.RequestedCategoryName}\" was rejected. Reason: {reason}",
                $"{{\"reason\": \"{reason}\"}}"
            );

            // Audit Log: Category Rejected
            var adminId = _userManager.GetUserId(User);
            var admin = await _userManager.FindByIdAsync(adminId);
            await _auditService.LogAsync(
                adminId,
                admin?.FullName,
                admin?.Email,
                "CategoryRequestRejected",
                "ProviderManagement",
                "CategoryRequest",
                id.ToString(),
                $"Admin rejected category request: {request.RequestedCategoryName}. Reason: {reason}"
            );

            return Ok(new { success = true, message = "Category request rejected." });
        }
    }
}
