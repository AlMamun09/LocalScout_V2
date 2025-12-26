using LocalScout.Application.DTOs;
using LocalScout.Application.DTOs.BookingDTOs;
using LocalScout.Application.Interfaces;
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
        private readonly IBookingRepository _bookingRepository;
        private readonly IServiceRepository _serviceRepository;
        private readonly IServiceCategoryRepository _categoryRepository;

        public UserController(
            UserManager<ApplicationUser> userManager,
            IBookingRepository bookingRepository,
            IServiceRepository serviceRepository,
            IServiceCategoryRepository categoryRepository)
        {
            _userManager = userManager;
            _bookingRepository = bookingRepository;
            _serviceRepository = serviceRepository;
            _categoryRepository = categoryRepository;
        }

        public async Task<IActionResult> Dashboard()
        {
            var userId = _userManager.GetUserId(User);
            
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            var user = await _userManager.FindByIdAsync(userId);

            // Get booking statistics
            var totalBookings = await _bookingRepository.GetUserBookingCountAsync(userId);
            var activeBookings = await _bookingRepository.GetUserActiveBookingCountAsync(userId);
            var completedBookings = await _bookingRepository.GetUserCompletedBookingCountAsync(userId);
            var totalSpent = await _bookingRepository.GetUserTotalSpentAsync(userId);

            // Get recent bookings for display
            var recentBookings = await _bookingRepository.GetUserBookingsAsync(userId);
            var recentBookingDtos = new List<BookingDto>();

            foreach (var b in recentBookings.Take(5))
            {
                // Get service details
                var service = await _serviceRepository.GetServiceByIdAsync(b.ServiceId);
                
                // Get category for icon
                var category = service != null ? await _categoryRepository.GetCategoryByIdAsync(service.ServiceCategoryId) : null;
                
                // Get provider details
                var provider = await _userManager.FindByIdAsync(b.ProviderId);

                recentBookingDtos.Add(new BookingDto
                {
                    BookingId = b.BookingId,
                    ServiceName = service?.ServiceName ?? "Service",
                    ProviderName = provider?.FullName ?? provider?.BusinessName ?? "Provider",
                    CategoryIcon = category?.IconPath ?? "fas fa-briefcase",
                    Date = b.CreatedAt.ToString("MMM dd, yyyy"),
                    Location = b.AddressArea,
                    Status = GetStatusDisplayText(b.Status),
                    StatusEnum = b.Status,
                    NegotiatedPrice = b.NegotiatedPrice
                });
            }

            var dashboardData = new UserDashboardDto
            {
                UserId = userId,
                FullName = user?.FullName ?? "User",
                Email = user?.Email ?? "",
                TotalBookings = totalBookings,
                ActiveBookings = activeBookings,
                CompletedBookings = completedBookings,
                TotalSpent = totalSpent,
                RecentBookings = recentBookingDtos
            };

            return View(dashboardData);
        }

        private static string GetStatusDisplayText(Domain.Enums.BookingStatus status)
        {
            return status switch
            {
                Domain.Enums.BookingStatus.PendingProviderReview => "Pending",
                Domain.Enums.BookingStatus.AcceptedByProvider => "Accepted",
                Domain.Enums.BookingStatus.AwaitingPayment => "Awaiting Payment",
                Domain.Enums.BookingStatus.PaymentReceived => "Paid",
                Domain.Enums.BookingStatus.InProgress => "In Progress",
                Domain.Enums.BookingStatus.JobDone => "Job Done",
                Domain.Enums.BookingStatus.Completed => "Completed",
                Domain.Enums.BookingStatus.Cancelled => "Cancelled",
                _ => status.ToString()
            };
        }
    }
}
