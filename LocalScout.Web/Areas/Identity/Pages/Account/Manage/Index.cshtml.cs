using LocalScout.Domain.Entities;
using LocalScout.Infrastructure.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using LocalScout.Application.Interfaces;

namespace LocalScout.Web.Areas.Identity.Pages.Account.Manage
{
    public partial class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IWebHostEnvironment _environment;
        private readonly IAuditService _auditService;

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IWebHostEnvironment environment,
            IAuditService auditService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _environment = environment;
            _auditService = auditService;
        }

        public string Username { get; set; } = string.Empty;
        public string? CurrentProfilePictureUrl { get; set; }
        public bool IsProvider { get; set; }
        public DateTime JoiningDate { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            [StringLength(100, MinimumLength = 2)]
            [Display(Name = "Full name")]
            public string FullName { get; set; } = string.Empty;

            [Phone]
            [Display(Name = "Phone number")]
            public string? PhoneNumber { get; set; }

            [Required]
            [DataType(DataType.Date)]
            [Display(Name = "Date of birth")]
            public DateTime? DateOfBirth { get; set; }

            [StringLength(20)]
            public string? Gender { get; set; }

            [StringLength(200)]
            public string? Address { get; set; }

            public double? Latitude { get; set; }
            public double? Longitude { get; set; }

            [Display(Name = "Profile Picture")]
            public IFormFile? ProfilePicture { get; set; }

            [StringLength(150)]
            [Display(Name = "Business Name")]
            public string? BusinessName { get; set; }

            [StringLength(1000)]
            [Display(Name = "Business Description")]
            public string? Description { get; set; }

            [StringLength(100)]
            [Display(Name = "Working Days")]
            public string? WorkingDays { get; set; }

            [StringLength(100)]
            [Display(Name = "Working Hours")]
            public string? WorkingHours { get; set; }
        }

        private async Task LoadAsync(ApplicationUser user)
        {
            var userName = await _userManager.GetUserNameAsync(user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);

            Username = userName;
            CurrentProfilePictureUrl = user.ProfilePictureUrl;
            JoiningDate = user.CreatedAt;

            Input = new InputModel
            {
                FullName = user.FullName,
                PhoneNumber = phoneNumber,
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender,
                Address = user.Address,
                Latitude = user.Latitude,
                Longitude = user.Longitude,
                BusinessName = user.BusinessName,
                Description = user.Description,
                WorkingDays = user.WorkingDays,
                WorkingHours = user.WorkingHours
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var roles = await _userManager.GetRolesAsync(user);
            IsProvider = roles.Contains(RoleNames.ServiceProvider);

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var roles = await _userManager.GetRolesAsync(user);
            IsProvider = roles.Contains(RoleNames.ServiceProvider);

            // Validate age
            var minDate = DateTime.UtcNow.AddYears(-18).Date;
            if (!Input.DateOfBirth.HasValue)
            {
                ModelState.AddModelError("Input.DateOfBirth", "Date of birth is required.");
            }
            else if (Input.DateOfBirth.Value.Date > minDate)
            {
                ModelState.AddModelError("Input.DateOfBirth", "You must be at least 18 years old.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "Unexpected error when trying to set phone number.";
                    return RedirectToPage();
                }
            }

            // Handle Profile Picture Upload
            if (Input.ProfilePicture != null)
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "profiles");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = $"{user.Id}_{Guid.NewGuid()}{Path.GetExtension(Input.ProfilePicture.FileName)}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await Input.ProfilePicture.CopyToAsync(fileStream);
                }

                // Delete old profile picture if exists and not default
                if (!string.IsNullOrEmpty(user.ProfilePictureUrl) && user.ProfilePictureUrl.StartsWith("/uploads/profiles/"))
                {
                    var oldPath = Path.Combine(_environment.WebRootPath, user.ProfilePictureUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                    if (System.IO.File.Exists(oldPath))
                    {
                        try
                        {
                            System.IO.File.Delete(oldPath);
                        }
                        catch
                        {
                            // Ignore delete errors
                        }
                    }
                }

                user.ProfilePictureUrl = $"/uploads/profiles/{uniqueFileName}";
            }

            user.FullName = Input.FullName.Trim();
            user.DateOfBirth = Input.DateOfBirth;
            user.Gender = string.IsNullOrWhiteSpace(Input.Gender) ? null : Input.Gender.Trim();
            user.Address = string.IsNullOrWhiteSpace(Input.Address) ? null : Input.Address.Trim();
            user.Latitude = Input.Latitude;
            user.Longitude = Input.Longitude;
            user.UpdatedAt = DateTime.UtcNow;

            if (IsProvider)
            {
                user.BusinessName = string.IsNullOrWhiteSpace(Input.BusinessName) ? null : Input.BusinessName.Trim();
                user.Description = string.IsNullOrWhiteSpace(Input.Description) ? null : Input.Description.Trim();
                user.WorkingDays = string.IsNullOrWhiteSpace(Input.WorkingDays) ? null : Input.WorkingDays.Trim();
                user.WorkingHours = string.IsNullOrWhiteSpace(Input.WorkingHours) ? null : Input.WorkingHours.Trim();
            }

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                await LoadAsync(user);
                return Page();
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Your profile has been updated";

            // Audit Log: Profile Updated
            await _auditService.LogAsync(
                user.Id,
                user.FullName ?? user.UserName,
                user.Email,
                "ProfileUpdated",
                IsProvider ? "ProviderManagement" : "UserManagement",
                "User",
                user.Id,
                "User updated their profile information"
            );

            return RedirectToPage();
        }
    }
}
