using LocalScout.Application.Interfaces;
using LocalScout.Infrastructure.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocalScout.Web.Controllers
{
    [Authorize(Roles = RoleNames.Admin)]
    public class AdminController : Controller
    {
        private readonly IUserRepository _userRepository;
        private readonly IServiceProviderRepository _providerRepository;

        public AdminController(IUserRepository userRepository, IServiceProviderRepository providerRepository)
        {
            _userRepository = userRepository;
            _providerRepository = providerRepository;
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

                // Active providers = IsVerified = true AND IsActive = true
                var activeProviders = allProviders.Where(p => p.IsVerified && p.IsActive);

                // Blocked providers = IsActive = false (regardless of verification)
                var blockedProviders = allProviders.Where(p => !p.IsActive);

                // Pending verifications = IsVerified = false
                var pendingVerifications = await _providerRepository.GetVerificationRequestsAsync();

                var stats = new
                {
                    totalUsers = allUsers.Count(),
                    activeUsers = activeUsers.Count(),
                    blockedUsers = blockedUsers.Count(),
                    newUsersToday = newUsersToday.Count(),
                    totalProviders = allProviders.Count(),
                    activeProviders = activeProviders.Count(),
                    blockedProviders = blockedProviders.Count(),
                    pendingVerifications = pendingVerifications.Count(),
                    recentUsers = allUsers.OrderByDescending(u => u.CreatedAt).Take(5),
                    recentProviders = allProviders.OrderByDescending(p => p.CreatedAt).Take(5)
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to load dashboard statistics", error = ex.Message });
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

            return View("Users", users);
        }

        // --- 3. Blocked Users Page ---
        public async Task<IActionResult> BlockedUsers()
        {
            ViewData["Title"] = "Blocked Users";
            var users = await _userRepository.GetUsersByStatusAsync(false);

            return View("Users", users);
        }

        [HttpGet]
        public async Task<IActionResult> GetUserDetails(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest();

            var user = await _userRepository.GetUserByIdAsync(id);
            if (user == null)
                return NotFound();

            return PartialView("_UserDetailsPartial", user);
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

        // --- 1. All Providers ---
        public async Task<IActionResult> Providers()
        {
            ViewData["Title"] = "All Service Providers";
            var providers = await _providerRepository.GetAllProvidersAsync();
            return View(providers);
        }

        // --- 2. Active Providers Page ---
        // Shows providers that are BOTH verified AND active
        public async Task<IActionResult> ActiveProviders()
        {
            ViewData["Title"] = "Active Service Providers";
            var allProviders = await _providerRepository.GetProvidersByStatusAsync(true);

            // Filter to show only verified AND active providers
            var verifiedActiveProviders = allProviders.Where(p => p.IsVerified).ToList();

            return View("Providers", verifiedActiveProviders);
        }

        // --- 3. Blocked Providers Page ---
        // Shows providers that are blocked (IsActive = false), regardless of verification
        public async Task<IActionResult> BlockedProviders()
        {
            ViewData["Title"] = "Blocked Service Providers";
            var providers = await _providerRepository.GetProvidersByStatusAsync(false);

            return View("Providers", providers);
        }

        // --- 4. Verification Requests ---
        // Shows providers that are NOT yet verified (IsVerified = false)
        public async Task<IActionResult> VerificationRequests()
        {
            ViewData["Title"] = "Provider Verification Requests";
            var providers = await _providerRepository.GetVerificationRequestsAsync();

            return View("Providers", providers);
        }

        [HttpGet]
        public async Task<IActionResult> GetProviderDetails(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest();

            var provider = await _providerRepository.GetProviderByIdAsync(id);
            if (provider == null)
                return NotFound();

            return PartialView("_ProviderDetailsPartial", provider);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleProviderStatus(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest(new { message = "Invalid provider ID." });

            var success = await _providerRepository.ToggleProviderStatusAsync(id);
            if (success)
            {
                return Ok(new { success = true, message = "Provider status updated successfully." });
            }

            return BadRequest(new { message = "Failed to update provider status." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveProvider(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest(new { message = "Invalid provider ID." });

            var success = await _providerRepository.ApproveProviderAsync(id);
            if (success)
            {
                return Ok(new { success = true, message = "Provider approved successfully." });
            }

            return BadRequest(new { message = "Failed to approve provider." });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectProvider(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest(new { message = "Invalid provider ID." });

            var success = await _providerRepository.RejectProviderAsync(id);
            if (success)
            {
                return Ok(new { success = true, message = "Provider rejected successfully." });
            }

            return BadRequest(new { message = "Failed to reject provider." });
        }
    }
}
