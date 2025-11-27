// LocalScout.Web\Controllers\VerificationController.cs
using System.Security.Claims;
using LocalScout.Application.DTOs;
using LocalScout.Application.Interfaces;
using LocalScout.Domain.Entities;
using LocalScout.Infrastructure.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;


namespace LocalScout.Web.Controllers
{
    [Authorize(Roles = "Provider")]
    public class VerificationController : Controller
    {
        private readonly IVerificationRepository _repo;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationRepository _notificationRepository;
        private readonly IWebHostEnvironment _env;

        public VerificationController(
            IVerificationRepository repo,
            UserManager<ApplicationUser> userManager,
            INotificationRepository notificationRepository,
            IWebHostEnvironment env
        )
        {
            _repo = repo;
            _userManager = userManager;
            _notificationRepository = notificationRepository;
            _env = env;
        }

        // GET: Verification/Index (The Dashboard)
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Fetch the latest request to determine UI state (Pending vs Form vs Approved)
            var latestRequest = await _repo.GetLatestRequestByProviderIdAsync(userId);

            return View(latestRequest);
        }

        // POST: Verification/SubmitVerification
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitVerification(VerificationSubmissionDto dto)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Log for debugging
                Console.WriteLine($"Submitting verification for user: {userId}");
                Console.WriteLine($"Document Type: {dto.DocumentType}");
                Console.WriteLine($"Document File: {dto.Document?.FileName ?? "null"}");

                // Check ModelState
                if (!ModelState.IsValid)
                {
                    TempData["ErrorMessage"] = "Please fill in all required fields.";
                    return RedirectToAction("Index", "Provider");
                }

                // 1. Validate Logic (Business rules in Repository)
                string error = await _repo.ValidateSubmissionAsync(userId, dto.Document);
                if (error != null)
                {
                    TempData["ErrorMessage"] = error;
                    return RedirectToAction("Index", "Provider");
                }

                // 2. Submit Request
                await _repo.SubmitRequestAsync(userId, dto, _env.WebRootPath);

                // 3. Notify Admins (Persistent DB Notification)
                var user = await _userManager.FindByIdAsync(userId);
                var admins = await _userManager.GetUsersInRoleAsync(RoleNames.Admin);
                
                foreach (var admin in admins)
                {
                    await _notificationRepository.CreateNotificationAsync(
                        admin.Id,
                        "New Verification Request",
                        $"New verification request from {user.FullName}"
                    );
                }

                TempData["SuccessMessage"] = "Verification documents submitted successfully! Please wait for admin review.";
                return RedirectToAction("Index", "Provider");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error submitting verification: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                TempData["ErrorMessage"] = $"An error occurred while submitting your verification: {ex.Message}";
                return RedirectToAction("Index", "Provider");
            }
        }
    }
}
