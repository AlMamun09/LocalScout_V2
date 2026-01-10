using LocalScout.Application.DTOs;
using LocalScout.Application.DTOs.BookingDTOs;
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
        private readonly IBookingRepository _bookingRepository;
        private readonly IServiceRepository _serviceRepository;
        private readonly IReviewRepository _reviewRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;

        public ProviderController(
            IServiceProviderRepository providerRepository,
            IVerificationRepository verificationRepository,
            IBookingRepository bookingRepository,
            IServiceRepository serviceRepository,
            IReviewRepository reviewRepository,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment)
        {
            _providerRepository = providerRepository;
            _verificationRepository = verificationRepository;
            _bookingRepository = bookingRepository;
            _serviceRepository = serviceRepository;
            _reviewRepository = reviewRepository;
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

            // Get booking statistics
            var totalBookings = await _bookingRepository.GetProviderCompletedBookingCountAsync(userId);
            var pendingRequests = await _bookingRepository.GetProviderPendingRequestCountAsync(userId);
            var totalEarnings = await _bookingRepository.GetProviderTotalEarningsAsync(userId);

            // Get dynamic average rating from reviews
            var averageRating = await _reviewRepository.GetProviderAverageRatingAsync(userId);

            // Get recent bookings
            var recentBookings = await _bookingRepository.GetProviderBookingsAsync(userId);
            var recentBookingDtos = new List<BookingDto>();

            foreach (var b in recentBookings.Take(5))
            {
                // Get service details
                var service = await _serviceRepository.GetServiceByIdAsync(b.ServiceId);
                
                // Get customer details
                var customer = await _userManager.FindByIdAsync(b.UserId);

                recentBookingDtos.Add(new BookingDto
                {
                    BookingId = b.BookingId,
                    CustomerName = customer?.FullName ?? "Customer",
                    ServiceName = service?.ServiceName ?? "Service",
                    Date = b.CreatedAt.ToString("MMM dd, yyyy"),
                    Location = TruncateAddress(b.AddressArea, 3),
                    Status = GetStatusDisplayText(b.Status),
                    StatusEnum = b.Status,
                    NegotiatedPrice = b.NegotiatedPrice
                });
            }

            // Get provider info
            var provider = await _userManager.FindByIdAsync(userId);
            
            // Get active services count
            var activeServicesCount = await _serviceRepository.GetProviderActiveServiceCountAsync(userId);

            // Get provider dashboard data
            var dashboardData = new ProviderDashboardDto
            {
                ProviderId = userId,
                ProviderName = provider?.FullName ?? "Provider",
                BusinessName = provider?.BusinessName,
                ProfilePictureUrl = provider?.ProfilePictureUrl,
                ActiveServicesCount = activeServicesCount,
                TotalEarnings = totalEarnings,
                TotalBookings = totalBookings,
                PendingRequestsCount = pendingRequests,
                AverageRating = (decimal)averageRating,
                RecentBookings = recentBookingDtos
            };

            // Get verification request status
            var verificationRequest = await _verificationRepository.GetLatestRequestByProviderIdAsync(userId);
            dashboardData.VerificationRequest = verificationRequest;

            return View(dashboardData);
        }

        /// <summary>
        /// Truncates address to show only up to the specified number of commas
        /// </summary>
        private static string? TruncateAddress(string? address, int commaCount)
        {
            if (string.IsNullOrEmpty(address))
                return address;

            var parts = address.Split(',');
            if (parts.Length <= commaCount)
                return address;

            return string.Join(",", parts.Take(commaCount)).Trim();
        }

        private static string GetStatusDisplayText(BookingStatus status)
        {
            return status switch
            {
                BookingStatus.PendingProviderReview => "Pending",
                BookingStatus.AcceptedByProvider => "Accepted",
                BookingStatus.AwaitingPayment => "Awaiting Payment",
                BookingStatus.PaymentReceived => "Paid",
                BookingStatus.InProgress => "In Progress",
                BookingStatus.JobDone => "Job Done",
                BookingStatus.Completed => "Completed",
                BookingStatus.Cancelled => "Cancelled",
                _ => status.ToString()
            };
        }
    }
}
