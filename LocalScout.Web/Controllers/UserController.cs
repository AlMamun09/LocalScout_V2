using LocalScout.Application.DTOs;
using LocalScout.Domain.Entities;
using LocalScout.Infrastructure.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LocalScout.Web.Controllers
{
    [Authorize(Roles = RoleNames.User)]
    public class UserController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UserController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            var userId = _userManager.GetUserId(User);
            
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            var user = await _userManager.FindByIdAsync(userId);

            // Create dashboard data model
            var dashboardData = new UserDashboardDto
            {
                UserId = userId,
                FullName = user?.FullName ?? "User",
                Email = user?.Email ?? "",
                TotalBookings = 0, // TODO: Implement booking count
                ActiveBookings = 0, // TODO: Get active bookings
                CompletedBookings = 0, // TODO: Get completed bookings
                TotalSpent = 0, // TODO: Calculate from payments
                RecentBookings = new List<BookingDto>() // TODO: Get recent bookings
            };

            return View(dashboardData);
        }
    }
}
