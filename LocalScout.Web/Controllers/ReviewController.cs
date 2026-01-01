using LocalScout.Application.Interfaces;
using LocalScout.Domain.Entities;
using LocalScout.Infrastructure.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LocalScout.Web.Controllers
{
    [Authorize]
    public class ReviewController : Controller
    {
        private readonly IReviewRepository _reviewRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ReviewController> _logger;

        public ReviewController(
            IReviewRepository reviewRepository,
            UserManager<ApplicationUser> userManager,
            ILogger<ReviewController> logger)
        {
            _reviewRepository = reviewRepository;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// User's My Reviews page
        /// </summary>
        [Authorize(Roles = RoleNames.User)]
        public async Task<IActionResult> MyReviews()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            var reviews = await _reviewRepository.GetUserReviewsAsync(userId);
            return View(reviews);
        }

        /// <summary>
        /// Provider's received reviews page
        /// </summary>
        [Authorize(Roles = RoleNames.ServiceProvider)]
        public async Task<IActionResult> ProviderReviews()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            // Get provider specific review data
            var reviews = await _reviewRepository.GetProviderReviewsAsync(userId);
            var averageRating = await _reviewRepository.GetProviderAverageRatingAsync(userId);
            var totalReviews = await _reviewRepository.GetProviderReviewCountAsync(userId);

            ViewBag.AverageRating = averageRating;
            ViewBag.TotalReviews = totalReviews;

            return View(reviews);
        }
    }
}
