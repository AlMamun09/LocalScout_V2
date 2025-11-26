using LocalScout.Application.DTOs;
using LocalScout.Application.Interfaces;
using LocalScout.Domain.Entities;
using LocalScout.Domain.Enums;
using LocalScout.Infrastructure.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LocalScout.Web.Controllers
{
    [Authorize(Roles = RoleNames.ServiceProvider)]
    public class ProviderController : Controller
    {
        private readonly IServiceProviderRepository _providerRepository;
        private readonly IVerificationRepository _verificationRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;

        public ProviderController(
            IServiceProviderRepository providerRepository,
            IVerificationRepository verificationRepository,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment)
        {
            _providerRepository = providerRepository;
            _verificationRepository = verificationRepository;
            _userManager = userManager;
            _environment = environment;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            // Get provider dashboard data
            var dashboardData = new ProviderDashboardDto
            {
                ProviderId = userId,
                TotalEarnings = 0, // TODO: Implement earnings calculation
                TotalBookings = 0, // TODO: Get from booking repository
                PendingRequestsCount = 0, // TODO: Get pending bookings
                AverageRating = 0.0m, // TODO: Calculate from reviews
                RecentBookings = new List<BookingDto>() // TODO: Get recent bookings
            };

            // Get verification request status
            var verificationRequest = await _verificationRepository.GetLatestRequestByProviderIdAsync(userId);
            dashboardData.VerificationRequest = verificationRequest;

            return View(dashboardData);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitVerification(IFormFile document, string documentType)
        {
            if (document == null || document.Length == 0)
            {
                TempData["Error"] = "Please select a document to upload.";
                return RedirectToAction(nameof(Index));
            }

            // Validate file size (5MB max)
            if (document.Length > 5 * 1024 * 1024)
            {
                TempData["Error"] = "File size must not exceed 5MB.";
                return RedirectToAction(nameof(Index));
            }

            // Validate file type
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
            var fileExtension = Path.GetExtension(document.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(fileExtension))
            {
                TempData["Error"] = "Only JPG, PNG, and PDF files are allowed.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var userId = _userManager.GetUserId(User);
                
                // Create uploads directory if it doesn't exist
                var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads", "verifications");
                Directory.CreateDirectory(uploadsPath);

                // Generate unique filename
                var fileName = $"{userId}_{DateTime.Now:yyyyMMddHHmmss}{fileExtension}";
                var filePath = Path.Combine(uploadsPath, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await document.CopyToAsync(stream);
                }

                // Create verification submission DTO
                var submissionDto = new VerificationSubmissionDto
                {
                    DocumentType = documentType
                };

                // Submit verification request using repository method
                await _verificationRepository.SubmitRequestAsync(userId, submissionDto, $"/uploads/verifications/{fileName}");

                TempData["Success"] = "Your verification document has been submitted successfully. Please wait for admin approval.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"An error occurred while uploading the document: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
