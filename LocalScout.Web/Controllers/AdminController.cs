using LocalScout.Application.DTOs;
using LocalScout.Application.DTOs.AuditDTOs;
using LocalScout.Application.Interfaces;
using LocalScout.Domain.Entities;
using LocalScout.Domain.Enums;
using LocalScout.Infrastructure.Constants;
using LocalScout.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace LocalScout.Web.Controllers
{
    [Authorize(Roles = RoleNames.Admin)]
    public class AdminController : Controller
    {
        private readonly IUserRepository _userRepository;
        private readonly IServiceProviderRepository _providerRepository;

        // NEW Dependencies
        private readonly IVerificationRepository _verificationRepo;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly INotificationRepository _notificationRepository;
        private readonly ILogger<AdminController> _logger;
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly IAuditService _auditService;
        private readonly ApplicationDbContext _context;

        public AdminController(
            IUserRepository userRepository,
            IServiceProviderRepository providerRepository,
            IVerificationRepository verificationRepo,
            UserManager<ApplicationUser> userManager,
            INotificationRepository notificationRepository,
            ILogger<AdminController> logger,
            IAuditLogRepository auditLogRepository,
            IAuditService auditService,
            ApplicationDbContext context
        )
        {
            _userRepository = userRepository;
            _providerRepository = providerRepository;
            _verificationRepo = verificationRepo;
            _userManager = userManager;
            _notificationRepository = notificationRepository;
            _logger = logger;
            _auditLogRepository = auditLogRepository;
            _auditService = auditService;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        // --- Dashboard Statistics ---
        [HttpGet]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var allUsers = await _userRepository.GetAllUsersAsync();
                var activeUsers = allUsers.Where(u => u.IsActive);
                var blockedUsers = allUsers.Where(u => !u.IsActive);
                var newUsersToday = allUsers.Where(u => u.CreatedAt.Date == DateTime.UtcNow.Date);

                var allProviders = await _providerRepository.GetAllProvidersAsync();
                var activeProviders = allProviders.Where(p => p.IsActive);
                var blockedProviders = allProviders.Where(p => !p.IsActive);

                // Updated: Get pending count from VerificationRepo for accuracy
                var pendingRequests = await _verificationRepo.GetPendingRequestsAsync();

                var stats = new
                {
                    totalUsers = allUsers.Count(),
                    activeUsers = activeUsers.Count(),
                    blockedUsers = blockedUsers.Count(),
                    newUsersToday = newUsersToday.Count(),
                    totalProviders = allProviders.Count(),
                    activeProviders = activeProviders.Count(),
                    blockedProviders = blockedProviders.Count(),
                    pendingVerifications = pendingRequests.Count, // Updated source
                    recentUsers = allUsers.OrderByDescending(u => u.CreatedAt).Take(5),
                    recentProviders = allProviders.OrderByDescending(p => p.CreatedAt).Take(5),
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new { message = "Failed to load stats", error = ex.Message }
                );
            }
        }

        // --- 1. All Users ---
        public async Task<IActionResult> Users()
        {
            ViewData["Title"] = "All Users";
            var users = await _userRepository.GetAllUsersAsync();
            return View(users);
        }

        // --- 2. Active Users Page ---
        public async Task<IActionResult> ActiveUsers()
        {
            ViewData["Title"] = "Active Users";
            var users = await _userRepository.GetUsersByStatusAsync(true);

            return View("User/Users", users);
        }

        // --- 3. Blocked Users Page ---
        public async Task<IActionResult> BlockedUsers()
        {
            ViewData["Title"] = "Blocked Users";
            var users = await _userRepository.GetUsersByStatusAsync(false);

            return View("User/Users", users);
        }

        [HttpGet]
        public async Task<IActionResult> GetUserDetails(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest();

            var user = await _userRepository.GetUserByIdAsync(id);
            if (user == null)
                return NotFound();

            // Get booking statistics for this user
            var userBookings = await _context.Bookings
                .Where(b => b.UserId == id)
                .ToListAsync();

            ViewBag.TotalBookings = userBookings.Count;
            ViewBag.CompletedBookings = userBookings.Count(b => b.Status == Domain.Enums.BookingStatus.Completed);
            ViewBag.CancelledBookings = userBookings.Count(b => b.Status == Domain.Enums.BookingStatus.Cancelled || b.Status == Domain.Enums.BookingStatus.AutoCancelled);
            ViewBag.PendingBookings = userBookings.Count(b => b.Status == Domain.Enums.BookingStatus.PendingProviderReview || b.Status == Domain.Enums.BookingStatus.PendingUserApproval || b.Status == Domain.Enums.BookingStatus.PendingProviderApproval);
            ViewBag.InProgressBookings = userBookings.Count(b => b.Status == Domain.Enums.BookingStatus.InProgress || b.Status == Domain.Enums.BookingStatus.AcceptedByProvider || b.Status == Domain.Enums.BookingStatus.PaymentReceived);

            return PartialView("User/_UserDetailsPartial", user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserStatus(string id, string? reason = null)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest(new { message = "Invalid user ID." });

            // Get user details before toggling status
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return BadRequest(new { message = "User not found." });

            // Store the current status to determine the action
            var wasActive = user.IsActive;

            // Toggle the status with block reason
            var success = await _userRepository.ToggleUserStatusAsync(id, reason);
            if (success)
            {
                // Determine the new status and message
                var newStatus = wasActive ? "Blocked" : "Unblocked";
                var message = wasActive
                    ? $"Your account has been blocked by the administrator. Reason: {reason ?? "No reason provided"}. Please contact support for assistance."
                    : "Your account has been unblocked. You can now access all features.";

                // Create persistent notification
                try
                {
                    var title = wasActive ? "Account Blocked" : "Account Unblocked";
                    _logger.LogInformation($"Creating notification for User ID: {id} - {title}");
                    await _notificationRepository.CreateNotificationAsync(id, title, message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to create notification for User ID: {id}");
                }

                // Audit Log
                try
                {
                    await _auditService.LogAsync(
                        wasActive ? "UserBlocked" : "UserUnblocked",
                        "UserManagement",
                        "User",
                        id,
                        wasActive ? $"User '{user.FullName}' blocked. Reason: {reason ?? "No reason provided"}" : $"User '{user.FullName}' unblocked.",
                        true
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to create audit log for User ID: {id}");
                }

                return Ok(new
                {
                    success = true,
                    message = $"User status updated successfully. User has been {newStatus.ToLower()}."
                });
            }

            return BadRequest(new { message = "Failed to update user status." });
        }

        public async Task<IActionResult> Providers()
        {
            ViewData["Title"] = "All Service Providers";
            var providers = await _providerRepository.GetAllProvidersAsync();
            return View(providers);
        }

        public async Task<IActionResult> ActiveProviders()
        {
            ViewData["Title"] = "Active Service Providers";
            var allProviders = await _providerRepository.GetProvidersByStatusAsync(true);
            return View("Provider/Providers", allProviders);
        }

        public async Task<IActionResult> BlockedProviders()
        {
            ViewData["Title"] = "Blocked Service Providers";
            var providers = await _providerRepository.GetProvidersByStatusAsync(false);
            return View("Provider/Providers", providers);
        }

        // --- UPDATED: Verification Requests ---
        public async Task<IActionResult> VerificationRequests()
        {
            ViewData["Title"] = "Provider Verification Requests";

            // 1. Get pending requests from the new Verification Repository
            var pendingRequests = await _verificationRepo.GetPendingRequestsAsync();

            // 2. Map to ServiceProviderDto manually or via AutoMapper
            var providerDtos = new List<ServiceProviderDto>();

            foreach (var request in pendingRequests)
            {
                // We need the user details for the list
                var user = await _userManager.FindByIdAsync(request.ProviderId);
                if (user != null)
                {
                    providerDtos.Add(
                        new ServiceProviderDto
                        {
                            Id = user.Id,
                            FullName = user.FullName,
                            Email = user.Email,
                            PhoneNumber = user.PhoneNumber,
                            BusinessName = user.BusinessName,
                            CreatedAt = request.SubmittedAt, // Display submission date
                            IsActive = user.IsActive,
                            IsVerified = false, // It is pending
                            ProfilePictureUrl = user.ProfilePictureUrl,
                        }
                    );
                }
            }

            return View("Provider/Providers", providerDtos);
        }

        // --- UPDATED: Get Details (Includes Document) ---
        [HttpGet]
        public async Task<IActionResult> GetProviderDetails(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest();

            // 1. Get Basic Provider Info
            var provider = await _providerRepository.GetProviderByIdAsync(id);
            if (provider == null)
                return NotFound();

            // 2. Fetch the Verification Request to get the Document Path
            var verificationRequest = await _verificationRepo.GetLatestRequestByProviderIdAsync(id);

            // 3. Pass it to the View using ViewBag
            ViewBag.VerificationRequest = verificationRequest;

            // 4. Get provider statistics
            var providerServices = await _context.Services.Where(s => s.Id == id).ToListAsync();
            ViewBag.TotalServices = providerServices.Count;
            ViewBag.ActiveServices = providerServices.Count(s => s.IsActive);

            var providerBookings = await _context.Bookings.Where(b => b.ProviderId == id).ToListAsync();
            ViewBag.TotalBookingsReceived = providerBookings.Count;
            ViewBag.CompletedBookings = providerBookings.Count(b => b.Status == Domain.Enums.BookingStatus.Completed);
            ViewBag.CancelledBookings = providerBookings.Count(b => b.Status == Domain.Enums.BookingStatus.Cancelled || b.Status == Domain.Enums.BookingStatus.AutoCancelled);
            ViewBag.PendingBookings = providerBookings.Count(b => b.Status == Domain.Enums.BookingStatus.PendingProviderReview || b.Status == Domain.Enums.BookingStatus.PendingUserApproval);
            ViewBag.InProgressBookings = providerBookings.Count(b => b.Status == Domain.Enums.BookingStatus.InProgress || b.Status == Domain.Enums.BookingStatus.AcceptedByProvider || b.Status == Domain.Enums.BookingStatus.PaymentReceived);

            // 5. Calculate total earnings from completed bookings
            var totalEarnings = providerBookings
                .Where(b => b.Status == Domain.Enums.BookingStatus.Completed && b.NegotiatedPrice.HasValue)
                .Sum(b => b.NegotiatedPrice!.Value);
            ViewBag.TotalEarnings = totalEarnings;

            // 6. Get average rating
            var reviews = await _context.Reviews.Where(r => r.ProviderId == id).ToListAsync();
            ViewBag.TotalReviews = reviews.Count;
            ViewBag.AverageRating = reviews.Any() ? Math.Round(reviews.Average(r => r.Rating), 1) : 0;

            return PartialView("Provider/_ProviderDetailsPartial", provider);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleProviderStatus(string id, string? reason = null)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest(new { message = "Invalid provider ID." });

            // Get provider details before toggling status
            var provider = await _userManager.FindByIdAsync(id);
            if (provider == null)
                return BadRequest(new { message = "Provider not found." });

            // Store the current status to determine the action
            var wasActive = provider.IsActive;

            // Toggle the status with block reason
            var success = await _providerRepository.ToggleProviderStatusAsync(id, reason);
            if (success)
            {
                // Determine the new status and message
                var newStatus = wasActive ? "Blocked" : "Unblocked";
                var message = wasActive
                    ? $"Your provider account has been blocked by the administrator. Reason: {reason ?? "No reason provided"}. You cannot accept new bookings or create services. Please contact support for assistance."
                    : "Your provider account has been unblocked. You can now accept bookings and provide services.";

                // Create persistent notification
                try
                {
                    var title = wasActive ? "Provider Account Blocked" : "Provider Account Unblocked";
                    _logger.LogInformation($"Creating notification for Provider ID: {id} - {title}");
                    await _notificationRepository.CreateNotificationAsync(id, title, message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to create notification for Provider ID: {id}");
                }

                // Audit Log
                try
                {
                    await _auditService.LogAsync(
                        wasActive ? "ProviderBlocked" : "ProviderUnblocked",
                        "ProviderManagement",
                        "Provider",
                        id,
                        wasActive ? $"Provider '{provider.FullName}' ({provider.BusinessName}) blocked. Reason: {reason ?? "No reason provided"}" : $"Provider '{provider.FullName}' ({provider.BusinessName}) unblocked.",
                        true
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to create audit log for Provider ID: {id}");
                }

                return Ok(
                    new
                    {
                        success = true,
                        message = $"Provider status updated successfully. Provider has been {newStatus.ToLower()}."
                    }
                );
            }

            return BadRequest(new { message = "Failed to update provider status." });
        }
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveProvider(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest(new { message = "Invalid provider ID." });

            // 1. Find the Verification Request
            var request = await _verificationRepo.GetLatestRequestByProviderIdAsync(id);
            if (request == null || request.Status != VerificationStatus.Pending)
                return BadRequest(new { message = "No pending verification request found." });

            // 2. Update Request Status
            await _verificationRepo.UpdateRequestStatusAsync(
                request.VerificationRequestId,
                VerificationStatus.Approved
            );

            // 3. Update Provider Entity (Set IsVerified = true)
            var providerUpdateSuccess = await _providerRepository.ApproveProviderAsync(id);

            if (providerUpdateSuccess)
            {
                var title = "Verification Approved";
                var message = "Congratulations! Your account has been verified.";

                // 4. Create persistent notification
                try
                {
                    await _notificationRepository.CreateNotificationAsync(id, title, message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to create notification: {ex.Message}");
                }



                return Ok(
                    new { success = true, message = "Provider approved and verified successfully." }
                );
            }

            return BadRequest(new { message = "Failed to approve provider." });
        }

        // --- Reject Provider ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectProvider(string id, string? reason = null)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest(new { message = "Invalid provider ID." });

            var request = await _verificationRepo.GetLatestRequestByProviderIdAsync(id);
            if (request == null)
                return BadRequest(new { message = "Request not found." });

            // Use custom reason if provided, otherwise fall back to default
            var adminComments = string.IsNullOrWhiteSpace(reason)
                ? "Documents did not meet requirements."
                : reason.Trim();

            // 1. Update Request Status
            await _verificationRepo.UpdateRequestStatusAsync(
                request.VerificationRequestId,
                VerificationStatus.Rejected,
                adminComments
            );

            var title = "Verification Rejected";
            var message = "Your verification request was rejected. Please check details and resubmit.";

            // 2. Create persistent notification
            try
            {
                await _notificationRepository.CreateNotificationAsync(id, title, message,
                    $"{{\"reason\": \"{adminComments}\"}}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create notification: {ex.Message}");
            }

            return Ok(new { success = true, message = "Provider verification rejected." });
        }

        // --- Audit Log ---
        public async Task<IActionResult> AuditLog(
            string? search = null,
            string? category = null,
            string? actionFilter = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int page = 1)
        {
            ViewData["Title"] = "Audit Log";

            if (endDate.HasValue)
            {
                endDate = endDate.Value.Date.AddDays(1).AddTicks(-1);
            }

            var filter = new AuditLogFilterDto
            {
                SearchQuery = search,
                Category = category,
                Action = actionFilter,
                StartDate = startDate,
                EndDate = endDate,
                Page = page,
                PageSize = 25,
                SkipCount = true // Skip COUNT for fast initial load
            };

            var result = await _auditLogRepository.GetLogsAsync(filter);
            
            // Use hardcoded filter options for fast load (these are known values)
            ViewBag.Categories = new List<string> { 
                "Authentication", "UserManagement", "ProviderManagement", 
                "Booking", "Payment", "Service", "Review", "Report", "Category" 
            };
            ViewBag.Actions = new List<string> { 
                "Login", "Logout", "Register", "UserBlocked", "UserUnblocked",
                "ProviderBlocked", "ProviderUnblocked", "BookingCreated", "BookingCancelled",
                "BookingAccepted", "BookingCompleted", "PaymentInitiated", "PaymentSuccess",
                "PaymentFailed", "ServiceCreated", "ServiceUpdated", "ServiceDeleted",
                "ReviewCreated", "ReportGenerated", "CategoryRequested", "CategoryApproved"
            };
            
            return View(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAuditLogsData(
            string? searchTerm = null,
            string? category = null,
            string? actionFilter = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            bool? isSuccess = null,
            int start = 0,
            int length = 25,
            int draw = 1)
        {
            if (endDate.HasValue)
            {
                endDate = endDate.Value.Date.AddDays(1).AddTicks(-1);
            }

            var pageSize = length <= 0 ? 25 : length;
            var page = (start / pageSize) + 1;

            var filter = new AuditLogFilterDto
            {
                SearchQuery = searchTerm,
                Category = category,
                Action = actionFilter,
                StartDate = startDate,
                EndDate = endDate,
                IsSuccess = isSuccess,
                Page = page,
                PageSize = pageSize,
                SkipCount = false
            };

            var result = await _auditLogRepository.GetLogsAsync(filter);

            var data = result.Items.Select(l => new
            {
                auditLogId = l.AuditLogId,
                timestampDate = l.Timestamp.ToLocalTime().ToString("MMM dd, yyyy"),
                timestampTime = l.Timestamp.ToLocalTime().ToString("HH:mm:ss"),
                userName = string.IsNullOrEmpty(l.UserName) ? "System" : l.UserName,
                userEmail = l.UserEmail,
                category = l.Category,
                action = l.Action,
                entityType = l.EntityType,
                entityId = l.EntityId,
                details = l.Details,
                ipAddress = l.IpAddress,
                isSuccess = l.IsSuccess
            });

            return Json(new
            {
                draw,
                recordsTotal = result.TotalCount,
                recordsFiltered = result.TotalCount,
                data
            });
        }

        public async Task<IActionResult> GetAuditLogsTable(
            string? search = null,
            string? category = null,
            string? actionFilter = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int page = 1)
        {
            if (endDate.HasValue)
            {
                endDate = endDate.Value.Date.AddDays(1).AddTicks(-1);
            }

            var filter = new AuditLogFilterDto
            {
                SearchQuery = search,
                Category = category,
                Action = actionFilter,
                StartDate = startDate,
                EndDate = endDate,
                Page = page,
                PageSize = 25,
                SkipCount = true
            };

            var result = await _auditLogRepository.GetLogsAsync(filter);
            
            return PartialView("_AuditLogTable", result);
        }

        /// <summary>
        /// Returns just the table rows for infinite scroll (no wrapper, no pagination)
        /// </summary>
        public async Task<IActionResult> GetAuditLogsChunk(
            string? search = null,
            string? category = null,
            string? actionFilter = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int page = 1)
        {
            if (endDate.HasValue)
            {
                endDate = endDate.Value.Date.AddDays(1).AddTicks(-1);
            }

            var filter = new AuditLogFilterDto
            {
                SearchQuery = search,
                Category = category,
                Action = actionFilter,
                StartDate = startDate,
                EndDate = endDate,
                Page = page,
                PageSize = 25,
                SkipCount = true // Skip COUNT for faster chunk loading
            };

            var result = await _auditLogRepository.GetLogsAsync(filter);
            
            return PartialView("_AuditLogRows", result);
        }
    }
}
