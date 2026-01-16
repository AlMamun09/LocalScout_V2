using LocalScout.Application.DTOs.ReportDTOs;
using LocalScout.Application.Interfaces;
using LocalScout.Domain.Entities;
using LocalScout.Domain.Enums;
using LocalScout.Infrastructure.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LocalScout.Web.Controllers
{
    [Authorize]
    public class ReportController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IBookingRepository _bookingRepository;
        private readonly IReviewRepository _reviewRepository;
        private readonly IServiceRepository _serviceRepository;
        private readonly IUserRepository _userRepository;
        private readonly IServiceProviderRepository _providerRepository;
        private readonly IReportPdfService _reportPdfService;
        private readonly IAuditService _auditService;

        public ReportController(
            UserManager<ApplicationUser> userManager,
            IBookingRepository bookingRepository,
            IReviewRepository reviewRepository,
            IServiceRepository serviceRepository,
            IUserRepository userRepository,
            IServiceProviderRepository providerRepository,
            IReportPdfService reportPdfService,
            IAuditService auditService)
        {
            _userManager = userManager;
            _bookingRepository = bookingRepository;
            _reviewRepository = reviewRepository;
            _serviceRepository = serviceRepository;
            _userRepository = userRepository;
            _providerRepository = providerRepository;
            _reportPdfService = reportPdfService;
            _auditService = auditService;
        }

        #region User Reports

        [Authorize(Roles = RoleNames.User)]
        public async Task<IActionResult> UserReports(string dateRange = "last30", DateTime? startDate = null, DateTime? endDate = null, int? month = null, int? year = null)
        {
            var report = await GetUserReportDataAsync(dateRange, startDate, endDate, month, year);
            return View(report);
        }

        [Authorize(Roles = RoleNames.User)]
        [HttpGet]
        public async Task<IActionResult> GetUserReportData(string dateRange = "last30", DateTime? startDate = null, DateTime? endDate = null, int? month = null, int? year = null)
        {
            var report = await GetUserReportDataAsync(dateRange, startDate, endDate, month, year);
            return PartialView("_UserReportContent", report);
        }

        private async Task<UserReportDto> GetUserReportDataAsync(string dateRange, DateTime? startDate, DateTime? endDate, int? month, int? year)
        {
            var userId = _userManager.GetUserId(User);
            var (start, end) = GetDateRange(dateRange, startDate, endDate, month, year);

            var allBookings = await _bookingRepository.GetUserBookingsAsync(userId!);
            var filteredBookings = allBookings
                .Where(b => b.CreatedAt >= start && b.CreatedAt <= end)
                .OrderByDescending(b => b.CreatedAt)
                .ToList();

            var bookingDetails = new List<UserBookingReportItem>();
            foreach (var booking in filteredBookings)
            {
                var service = await _serviceRepository.GetServiceByIdAsync(booking.ServiceId);
                var provider = await _userManager.FindByIdAsync(booking.ProviderId);
                
                bookingDetails.Add(new UserBookingReportItem
                {
                    BookingId = booking.BookingId,
                    ServiceName = service?.ServiceName ?? "Unknown Service",
                    ProviderName = provider?.FullName ?? provider?.BusinessName ?? "Unknown",
                    BookingDate = booking.CreatedAt,
                    CompletedDate = booking.CompletedAt,
                    Status = booking.Status.ToString(),
                    Amount = booking.NegotiatedPrice
                });
            }

            return new UserReportDto
            {
                DateRange = dateRange,
                StartDate = start,
                EndDate = end,
                TotalBookings = filteredBookings.Count,
                CompletedBookings = filteredBookings.Count(b => b.Status == BookingStatus.Completed),
                CancelledBookings = filteredBookings.Count(b => b.Status == BookingStatus.Cancelled),
                TotalSpent = filteredBookings
                    .Where(b => (b.Status == BookingStatus.PaymentReceived || 
                                 b.Status == BookingStatus.JobDone || 
                                 b.Status == BookingStatus.Completed) && 
                                 b.NegotiatedPrice.HasValue)
                    .Sum(b => b.NegotiatedPrice!.Value),
                Bookings = bookingDetails
            };
        }

        [Authorize(Roles = RoleNames.User)]
        public async Task<IActionResult> ExportUserReportPdf(string dateRange = "last30", DateTime? startDate = null, DateTime? endDate = null, int? month = null, int? year = null)
        {
            var user = await _userManager.GetUserAsync(User);
            var report = await GetUserReportDataAsync(dateRange, startDate, endDate, month, year);
            
            // Audit Log: User Report Exported
            await _auditService.LogAsync(
                user?.Id ?? "",
                user?.FullName,
                user?.Email,
                "ReportExported",
                "Report",
                "UserReport",
                null,
                $"User activity report exported. Date Range: {report.StartDate:yyyy-MM-dd} to {report.EndDate:yyyy-MM-dd}, Total Bookings: {report.TotalBookings}, Total Spent: {report.TotalSpent:C}"
            );
            
            var pdfBytes = _reportPdfService.GenerateUserReport(report, user?.FullName ?? "User");
            var fileName = $"ActivityReport_{DateTime.Now:yyyyMMdd}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        #endregion

        #region Provider Reports

        [Authorize(Roles = RoleNames.ServiceProvider)]
        public async Task<IActionResult> ProviderReports(string dateRange = "last30", DateTime? startDate = null, DateTime? endDate = null, int? month = null, int? year = null)
        {
            var report = await GetProviderReportDataAsync(dateRange, startDate, endDate, month, year);
            return View(report);
        }

        [Authorize(Roles = RoleNames.ServiceProvider)]
        [HttpGet]
        public async Task<IActionResult> GetProviderReportData(string dateRange = "last30", DateTime? startDate = null, DateTime? endDate = null, int? month = null, int? year = null)
        {
            var report = await GetProviderReportDataAsync(dateRange, startDate, endDate, month, year);
            return PartialView("_ProviderReportContent", report);
        }

        private async Task<ProviderReportDto> GetProviderReportDataAsync(string dateRange, DateTime? startDate, DateTime? endDate, int? month, int? year)
        {
            var providerId = _userManager.GetUserId(User);
            var (start, end) = GetDateRange(dateRange, startDate, endDate, month, year);

            var allBookings = await _bookingRepository.GetProviderBookingsAsync(providerId!);
            var filteredBookings = allBookings
                .Where(b => b.CreatedAt >= start && b.CreatedAt <= end)
                .OrderByDescending(b => b.CreatedAt)
                .ToList();

            var avgRating = await _reviewRepository.GetProviderAverageRatingAsync(providerId!);
            var reviewCount = await _reviewRepository.GetProviderReviewCountAsync(providerId!);

            var bookingDetails = new List<ProviderBookingReportItem>();
            foreach (var booking in filteredBookings)
            {
                var service = await _serviceRepository.GetServiceByIdAsync(booking.ServiceId);
                var customer = await _userManager.FindByIdAsync(booking.UserId);

                bookingDetails.Add(new ProviderBookingReportItem
                {
                    BookingId = booking.BookingId,
                    ServiceName = service?.ServiceName ?? "Unknown Service",
                    CustomerName = customer?.FullName ?? "Unknown",
                    BookingDate = booking.CreatedAt,
                    CompletedDate = booking.CompletedAt,
                    Status = booking.Status.ToString(),
                    Amount = booking.NegotiatedPrice
                });
            }

            return new ProviderReportDto
            {
                DateRange = dateRange,
                StartDate = start,
                EndDate = end,
                TotalBookings = filteredBookings.Count,
                CompletedBookings = filteredBookings.Count(b => b.Status == BookingStatus.Completed),
                CancelledBookings = filteredBookings.Count(b => b.Status == BookingStatus.Cancelled),
                PendingBookings = filteredBookings.Count(b => b.Status == BookingStatus.PendingProviderReview),
                TotalEarnings = filteredBookings
                    .Where(b => (b.Status == BookingStatus.PaymentReceived || 
                                 b.Status == BookingStatus.JobDone || 
                                 b.Status == BookingStatus.Completed) && 
                                 b.NegotiatedPrice.HasValue)
                    .Sum(b => b.NegotiatedPrice!.Value),
                AverageRating = avgRating,
                TotalReviews = reviewCount,
                Bookings = bookingDetails
            };
        }

        [Authorize(Roles = RoleNames.ServiceProvider)]
        public async Task<IActionResult> ExportProviderReportPdf(string dateRange = "last30", DateTime? startDate = null, DateTime? endDate = null, int? month = null, int? year = null)
        {
            var user = await _userManager.GetUserAsync(User);
            var report = await GetProviderReportDataAsync(dateRange, startDate, endDate, month, year);
            
            // Audit Log: Provider Report Exported
            await _auditService.LogAsync(
                user?.Id ?? "",
                user?.FullName,
                user?.Email,
                "ReportExported",
                "Report",
                "ProviderReport",
                null,
                $"Provider earnings report exported. Date Range: {report.StartDate:yyyy-MM-dd} to {report.EndDate:yyyy-MM-dd}, Total Bookings: {report.TotalBookings}, Total Earnings: {report.TotalEarnings:C}"
            );
            
            var pdfBytes = _reportPdfService.GenerateProviderReport(report, user?.FullName ?? user?.BusinessName ?? "Provider");
            var fileName = $"EarningsReport_{DateTime.Now:yyyyMMdd}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        #endregion

        #region Admin Reports

        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> AdminReports(string dateRange = "last30", DateTime? startDate = null, DateTime? endDate = null, int? month = null, int? year = null)
        {
            var report = await GetAdminReportDataAsync(dateRange, startDate, endDate, month, year);
            return View(report);
        }

        [Authorize(Roles = RoleNames.Admin)]
        [HttpGet]
        public async Task<IActionResult> GetAdminReportData(string dateRange = "last30", DateTime? startDate = null, DateTime? endDate = null, int? month = null, int? year = null)
        {
            var report = await GetAdminReportDataAsync(dateRange, startDate, endDate, month, year);
            return PartialView("_AdminReportContent", report);
        }

        private async Task<AdminReportDto> GetAdminReportDataAsync(string dateRange, DateTime? startDate, DateTime? endDate, int? month, int? year)
        {
            var (start, end) = GetDateRange(dateRange, startDate, endDate, month, year);

            var allUsers = (await _userRepository.GetAllUsersAsync()).ToList();
            var allProviders = (await _providerRepository.GetAllProvidersAsync()).ToList();

            var newUsers = allUsers.Where(u => u.CreatedAt >= start && u.CreatedAt <= end).ToList();
            var newProviders = allProviders.Where(p => p.CreatedAt >= start && p.CreatedAt <= end).ToList();

            // Get all bookings for revenue calculation
            var allBookings = await _bookingRepository.GetAllBookingsAsync();
            var periodBookings = allBookings
                .Where(b => b.CreatedAt >= start && b.CreatedAt <= end)
                .ToList();
            
            // Get bookings with payment received (for revenue calculation)
            var paidBookings = periodBookings
                .Where(b => b.Status == BookingStatus.PaymentReceived || 
                           b.Status == BookingStatus.JobDone || 
                           b.Status == BookingStatus.Completed)
                .ToList();

            var totalRevenue = paidBookings
                .Where(b => b.NegotiatedPrice.HasValue)
                .Sum(b => b.NegotiatedPrice!.Value);
            
            // Detailed booking stats
            var completedBookings = periodBookings.Count(b => b.Status == BookingStatus.Completed);
            var cancelledBookings = periodBookings.Count(b => b.Status == BookingStatus.Cancelled || b.Status == BookingStatus.AutoCancelled);
            var pendingBookings = periodBookings.Count(b => b.Status == BookingStatus.PendingProviderReview || b.Status == BookingStatus.PendingUserApproval || b.Status == BookingStatus.PendingProviderApproval);
            var inProgressBookings = periodBookings.Count(b => b.Status == BookingStatus.InProgress || b.Status == BookingStatus.AcceptedByProvider || b.Status == BookingStatus.PaymentReceived);

            // Get all services
            var allServices = await _serviceRepository.GetAllServicesAsync();
            var allServicesList = allServices.ToList();
            var newServicesInPeriod = allServicesList.Count(s => s.CreatedAt >= start && s.CreatedAt <= end);

            // Get all reviews for platform rating
            var allReviews = await _reviewRepository.GetAllReviewsAsync();
            var allReviewsList = allReviews.ToList();
            var avgRating = allReviewsList.Any() ? allReviewsList.Average(r => r.Rating) : 0;

            return new AdminReportDto
            {
                DateRange = dateRange,
                StartDate = start,
                EndDate = end,
                // User & Provider Stats
                TotalUsers = allUsers.Count,
                TotalProviders = allProviders.Count,
                ActiveUsers = allUsers.Count(u => u.IsActive),
                ActiveProviders = allProviders.Count(p => p.IsActive && p.IsVerified),
                NewUsersInPeriod = newUsers.Count,
                NewProvidersInPeriod = newProviders.Count,
                // Blocked & Pending
                BlockedUsers = allUsers.Count(u => !u.IsActive),
                BlockedProviders = allProviders.Count(p => !p.IsActive),
                PendingVerifications = allProviders.Count(p => !p.IsVerified && p.IsActive),
                // Service Stats
                TotalServices = allServicesList.Count,
                ActiveServices = allServicesList.Count(s => s.IsActive),
                NewServicesInPeriod = newServicesInPeriod,
                // Revenue & Booking Stats
                TotalRevenue = totalRevenue,
                TotalBookings = periodBookings.Count,
                CompletedBookings = completedBookings,
                CancelledBookings = cancelledBookings,
                PendingBookings = pendingBookings,
                InProgressBookings = inProgressBookings,
                // Review Stats
                TotalReviews = allReviewsList.Count,
                AverageRating = Math.Round(avgRating, 1)
            };
        }

        [Authorize(Roles = RoleNames.Admin)]
        public async Task<IActionResult> ExportAdminReportPdf(string dateRange = "last30", DateTime? startDate = null, DateTime? endDate = null, int? month = null, int? year = null)
        {
            var user = await _userManager.GetUserAsync(User);
            var report = await GetAdminReportDataAsync(dateRange, startDate, endDate, month, year);
            
            // Audit Log: Admin Report Exported
            await _auditService.LogAsync(
                user?.Id ?? "",
                user?.FullName,
                user?.Email,
                "ReportExported",
                "Report",
                "AdminReport",
                null,
                $"Admin system report exported. Date Range: {report.StartDate:yyyy-MM-dd} to {report.EndDate:yyyy-MM-dd}, Total Revenue: {report.TotalRevenue:C}, Total Bookings: {report.TotalBookings}"
            );
            
            var pdfBytes = _reportPdfService.GenerateAdminReport(report);
            var fileName = $"SystemReport_{DateTime.Now:yyyyMMdd}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        #endregion

        #region Helper Methods

        private static (DateTime start, DateTime end) GetDateRange(string dateRange, DateTime? startDate, DateTime? endDate, int? month, int? year)
        {
            var now = DateTime.UtcNow;
            
            return dateRange.ToLower() switch
            {
                "last7" => (now.AddDays(-7).Date, now.Date.AddDays(1).AddSeconds(-1)),
                "last30" => (now.AddDays(-30).Date, now.Date.AddDays(1).AddSeconds(-1)),
                "month" when month.HasValue && year.HasValue => 
                    (new DateTime(year.Value, month.Value, 1), 
                     new DateTime(year.Value, month.Value, 1).AddMonths(1).AddSeconds(-1)),
                "custom" when startDate.HasValue && endDate.HasValue => 
                    (startDate.Value.Date, endDate.Value.Date.AddDays(1).AddSeconds(-1)),
                _ => (now.AddDays(-30).Date, now.Date.AddDays(1).AddSeconds(-1))
            };
        }

        #endregion
    }
}
