using LocalScout.Application.Interfaces;
using LocalScout.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LocalScout.Web.Areas.Identity.Pages.Account
{
    public class LogoutModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<LogoutModel> _logger;
        private readonly IAuditService _auditService;

        public LogoutModel(
            SignInManager<ApplicationUser> signInManager, 
            ILogger<LogoutModel> logger,
            IAuditService auditService)
        {
            _signInManager = signInManager;
            _logger = logger;
            _auditService = auditService;
        }

        public async Task<IActionResult> OnPost(string returnUrl = null)
        {
            var user = await _signInManager.UserManager.GetUserAsync(User);
            if (user != null)
            {
                await _auditService.LogAsync(
                    user.Id,
                    user.FullName ?? user.UserName ?? "Unknown",
                    user.Email,
                    "Logout",
                    "Authentication",
                    "User",
                    user.Id,
                    "User logged out"
                );
            }

            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            if (returnUrl != null)
            {
                return LocalRedirect(returnUrl);
            }
            else
            {
                // This needs to be a redirect so that the browser performs a new
                // request and the identity for the user gets updated.
                return RedirectToPage();
            }
        }
    }
}
