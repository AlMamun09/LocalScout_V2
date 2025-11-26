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
    }
}
