// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Text;
using System.Threading.Tasks;
using LocalScout.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace LocalScout.Web.Areas.Identity.Pages.Account
{
  [AllowAnonymous]
  public class RegisterConfirmationModel : PageModel
  {
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailSender _sender;

    public RegisterConfirmationModel(UserManager<ApplicationUser> userManager, IEmailSender sender)
    {
      _userManager = userManager;
      _sender = sender;
    }

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///   directly from your code. This API may change or be removed in future releases.
    /// </summary>
    public string Email { get; set; }

    /// <summary>
    ///   Indicates if this is a provider registration (true) or user registration (false)
    /// </summary>
    public bool IsProvider { get; set; }

    /// <summary>
    ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
    ///     directly from your code. This API may change or be removed in future releases.
    /// </summary>
    [TempData]
    public string StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(string email, string returnUrl = null, string userType = null)
    {
      if (email == null)
      {
        return RedirectToPage("/Index");
      }

      returnUrl = returnUrl ?? Url.Content("~/");

      var user = await _userManager.FindByEmailAsync(email);
      if (user == null)
      {
        return NotFound($"Unable to load user with email '{email}'.");
      }

      Email = email;

      // Determine if this is a provider registration
      IsProvider = userType?.ToLower() == "provider" || await _userManager.IsInRoleAsync(user, "Provider");

      return Page();
    }

    public async Task<IActionResult> OnPostAsync(string email)
    {
        if (email == null)
        {
            return RedirectToPage("/Index");
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            return NotFound($"Unable to load user with email '{email}'.");
        }

        Email = email;
        var userId = await _userManager.GetUserIdAsync(user);
        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
        var callbackUrl = Url.Page(
            "/Account/ConfirmEmail",
            pageHandler: null,
            values: new { area = "Identity", userId = userId, code = code },
            protocol: Request.Scheme
        );

        var emailBody = Infrastructure.Services.EmailService.GetConfirmationEmailTemplate(
            user.FullName ?? "User",
            System.Text.Encodings.Web.HtmlEncoder.Default.Encode(callbackUrl),
            await _userManager.IsInRoleAsync(user, "Provider") ? "Provider" : "User"
        );

        await _sender.SendEmailAsync(
            email,
            "Welcome to Neighbourly - Please Confirm Your Email",
            emailBody
        );

        StatusMessage = "Verification email sent. Please check your email.";
        IsProvider = await _userManager.IsInRoleAsync(user, "Provider");
        return Page();
    }
  }
}
