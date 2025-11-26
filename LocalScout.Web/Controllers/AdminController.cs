using LocalScout.Application.DTOs;
using LocalScout.Application.Interfaces;
using LocalScout.Domain.Entities;
using LocalScout.Domain.Enums;
using LocalScout.Infrastructure.Constants;
using LocalScout.Web.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

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
        private readonly IHubContext<NotificationHub> _hubContext;

        public AdminController(
            IUserRepository userRepository,
            IServiceProviderRepository providerRepository,
            IVerificationRepository verificationRepo,
            UserManager<ApplicationUser> userManager,
            IHubContext<NotificationHub> hubContext
        )
        {
            _userRepository = userRepository;
            _providerRepository = providerRepository;
            _verificationRepo = verificationRepo;
            _userManager = userManager;
            _hubContext = hubContext;
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

            return PartialView("User/_UserDetailsPartial", user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserStatus(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest(new { message = "Invalid user ID." });

            var success = await _userRepository.ToggleUserStatusAsync(id);
            if (success)
            {
                return Ok(new { success = true, message = "User status updated successfully." });
            }

            return BadRequest(new { message = "Failed to update user status." });
        }

        // ==================== PROVIDER MANAGEMENT ====================

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

            return PartialView("Provider/_ProviderDetailsPartial", provider);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleProviderStatus(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest(new { message = "Invalid provider ID." });

            var success = await _providerRepository.ToggleProviderStatusAsync(id);
            if (success)
                return Ok(
                    new { success = true, message = "Provider status updated successfully." }
                );

            return BadRequest(new { message = "Failed to update provider status." });
        }

        // --- UPDATED: Approve Provider (SignalR Integrated) ---
        [HttpPost]
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
            // You can use _providerRepository.ApproveProviderAsync(id) if it sets IsVerified=true
            var providerUpdateSuccess = await _providerRepository.ApproveProviderAsync(id);

            if (providerUpdateSuccess)
            {
                // 4. Send SignalR Notification
                await _hubContext
                    .Clients.User(id)
                    .SendAsync(
                        "ReceiveStatusUpdate",
                        "Approved",
                        "Congratulations! Your account has been verified."
                    );
                return Ok(
                    new { success = true, message = "Provider approved and verified successfully." }
                );
            }

            return BadRequest(new { message = "Failed to approve provider." });
        }

        // --- UPDATED: Reject Provider (SignalR Integrated) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectProvider(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest(new { message = "Invalid provider ID." });

            var request = await _verificationRepo.GetLatestRequestByProviderIdAsync(id);
            if (request == null)
                return BadRequest(new { message = "Request not found." });

            // 1. Update Request Status
            await _verificationRepo.UpdateRequestStatusAsync(
                request.VerificationRequestId,
                VerificationStatus.Rejected,
                "Documents did not meet requirements."
            );

            // 2. Send SignalR Notification
            await _hubContext
                .Clients.User(id)
                .SendAsync(
                    "ReceiveStatusUpdate",
                    "Rejected",
                    "Your verification request was rejected. Please check details and resubmit."
                );

            return Ok(new { success = true, message = "Provider verification rejected." });
        }
    }
}
