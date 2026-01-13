// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using LocalScout.Domain.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace LocalScout.Web.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly IUserEmailStore<ApplicationUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            IUserStore<ApplicationUser> userStore,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender
        )
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required]
            [StringLength(100, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 100 characters.")]
            [Display(Name = "Full Name")]
            public string FullName { get; set; }

            [Required]
            [Phone]
            [Display(Name = "Phone Number")]
            public string PhoneNumber { get; set; }

            [Required]
            [Display(Name = "Address")]
            public string Address { get; set; }

            public double? Latitude { get; set; }
            public double? Longitude { get; set; }

            [Required(ErrorMessage = "Gender is required.")]
            [RegularExpression("^(Male|Female|Other)$", ErrorMessage = "Please select a valid gender.")]
            [Display(Name = "Gender")]
            public string Gender { get; set; }

            [Display(Name = "Profile Picture")]
            public IFormFile ProfilePicture { get; set; }

            [Required]
            [Display(Name = "Date of Birth")]
            [DataType(DataType.Date)]
            public DateTime DateOfBirth { get; set; }

            // --- PROVIDER FIELDS ---
            [Display(Name = "Business Name (Optional)")]
            public string BusinessName { get; set; }

            [Display(Name = "Description (Optional)")]
            public string Description { get; set; }

            // --- END PROVIDER FIELDS ---

            [Required]
            [StringLength(
                100,
                ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.",
                MinimumLength = 8
            )]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare(
                "Password",
                ErrorMessage = "The password and confirmation password do not match."
            )]
            public string ConfirmPassword { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (
                await _signInManager.GetExternalAuthenticationSchemesAsync()
            ).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (
                await _signInManager.GetExternalAuthenticationSchemesAsync()
            ).ToList();

            // Validate age (must be 18+)
            var age = DateTime.UtcNow.Year - Input.DateOfBirth.Year;
            if (Input.DateOfBirth > DateTime.UtcNow.AddYears(-age)) age--;
            
            if (age < 18)
            {
                ModelState.AddModelError("Input.DateOfBirth", "You must be at least 18 years old to register.");
            }

            // Validate ProfilePicture if provided
            if (Input.ProfilePicture != null && Input.ProfilePicture.Length > 0)
            {
                const long maxFileSize = 5 * 1024 * 1024; // 5MB
                var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
                
                if (Input.ProfilePicture.Length > maxFileSize)
                {
                    ModelState.AddModelError("Input.ProfilePicture", "Profile picture must be less than 5MB.");
                }
                
                if (!allowedTypes.Contains(Input.ProfilePicture.ContentType.ToLower()))
                {
                    ModelState.AddModelError("Input.ProfilePicture", "Only JPEG, PNG, GIF, and WebP images are allowed.");
                }
            }

            if (ModelState.IsValid)
            {
                var user = CreateUser();

                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

                // --- START: Map all properties ---
                user.FullName = Input.FullName;
                user.PhoneNumber = Input.PhoneNumber;
                user.Address = Input.Address;
                user.Latitude = Input.Latitude;
                user.Longitude = Input.Longitude;
                user.Gender = Input.Gender;
                user.DateOfBirth = Input.DateOfBirth;
                user.CreatedAt = DateTime.UtcNow;
                user.BusinessName = Input.BusinessName;
                user.Description = Input.Description;

                // Handle profile picture upload
                if (Input.ProfilePicture != null && Input.ProfilePicture.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles");
                    Directory.CreateDirectory(uploadsFolder);
                    
                    var uniqueFileName = $"{Guid.NewGuid()}_{Input.ProfilePicture.FileName}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await Input.ProfilePicture.CopyToAsync(fileStream);
                    }
                    
                    user.ProfilePictureUrl = $"/uploads/profiles/{uniqueFileName}";
                }
                // --- END: Map all properties ---

                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");
                    await _userManager.AddToRoleAsync(user, "User");
                    var userId = await _userManager.GetUserIdAsync(user);
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new
                        {
                            area = "Identity",
                            userId = userId,
                            code = code,
                            returnUrl = returnUrl,
                        },
                        protocol: Request.Scheme
                    );

                    // Generate professional email template
                    var emailBody = LocalScout.Infrastructure.Services.EmailService.GetConfirmationEmailTemplate(
                        Input.FullName,
                        HtmlEncoder.Default.Encode(callbackUrl),
                        "User"
                    );

                    await _emailSender.SendEmailAsync(
                        Input.Email,
                        "Welcome to Neighbourly - Please Confirm Your Email",
                        emailBody
                    );

                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        return RedirectToPage(
                            "RegisterConfirmation",
                            new { email = Input.Email, returnUrl = returnUrl }
                        );
                    }
                    else
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }

        private ApplicationUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<ApplicationUser>();
            }
            catch
            {
                throw new InvalidOperationException(
                    $"Can't create an instance of '{nameof(ApplicationUser)}'. "
                        + $"Ensure that '{nameof(ApplicationUser)}' is not an abstract class and has a parameterless constructor, or alternatively "
                        + $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml"
                );
            }
        }

        private IUserEmailStore<ApplicationUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException(
                    "The default UI requires a user store with email support."
                );
            }
            return (IUserEmailStore<ApplicationUser>)_userStore;
        }
    }
}
