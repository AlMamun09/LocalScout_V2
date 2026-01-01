using LocalScout.Application.DTOs.ReviewDTOs;
using LocalScout.Application.Interfaces;
using LocalScout.Domain.Entities;
using LocalScout.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LocalScout.Infrastructure.Repositories
{
    public class ReviewRepository : IReviewRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReviewRepository> _logger;

        public ReviewRepository(ApplicationDbContext context, ILogger<ReviewRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> CreateReviewAsync(Review review)
        {
            try
            {
                review.ReviewId = Guid.NewGuid();
                review.CreatedAt = DateTime.UtcNow;
                review.IsDeleted = false;

                _context.Reviews.Add(review);

                // Update booking to mark it has a review
                var booking = await _context.Bookings.FindAsync(review.BookingId);
                if (booking != null)
                {
                    booking.HasReview = true;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating review for booking {BookingId}", review.BookingId);
                return false;
            }
        }

        public async Task<Review?> GetByBookingIdAsync(Guid bookingId)
        {
            return await _context.Reviews
                .FirstOrDefaultAsync(r => r.BookingId == bookingId && !r.IsDeleted);
        }

        public async Task<List<Review>> GetReviewsByServiceIdAsync(Guid serviceId)
        {
            return await _context.Reviews
                .Where(r => r.ServiceId == serviceId && !r.IsDeleted)
                .ToListAsync();
        }

        public async Task<List<ReviewDisplayDto>> GetServiceReviewsAsync(Guid serviceId, int page = 1, int pageSize = 10)
        {
            var reviews = await _context.Reviews
                .Where(r => r.ServiceId == serviceId && !r.IsDeleted)
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var userIds = reviews.Select(r => r.UserId).Distinct().ToList();
            var users = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => new { u.FullName, u.ProfilePictureUrl });

            return reviews.Select(r => new ReviewDisplayDto
            {
                ReviewId = r.ReviewId,
                UserName = users.ContainsKey(r.UserId) ? users[r.UserId].FullName ?? "User" : "User",
                UserProfilePicture = users.ContainsKey(r.UserId) ? users[r.UserId].ProfilePictureUrl : null,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt
            }).ToList();
        }

        public async Task<ServiceRatingDto> GetServiceRatingAsync(Guid serviceId)
        {
            var reviews = await _context.Reviews
                .Where(r => r.ServiceId == serviceId && !r.IsDeleted)
                .ToListAsync();

            if (!reviews.Any())
            {
                return new ServiceRatingDto
                {
                    ServiceId = serviceId,
                    AverageRating = 0,
                    TotalReviews = 0
                };
            }

            return new ServiceRatingDto
            {
                ServiceId = serviceId,
                AverageRating = Math.Round(reviews.Average(r => r.Rating), 1),
                TotalReviews = reviews.Count,
                FiveStarCount = reviews.Count(r => r.Rating == 5),
                FourStarCount = reviews.Count(r => r.Rating == 4),
                ThreeStarCount = reviews.Count(r => r.Rating == 3),
                TwoStarCount = reviews.Count(r => r.Rating == 2),
                OneStarCount = reviews.Count(r => r.Rating == 1)
            };
        }

        public async Task<double> GetProviderAverageRatingAsync(string providerId)
        {
            var reviews = await _context.Reviews
                .Where(r => r.ProviderId == providerId && !r.IsDeleted)
                .ToListAsync();

            if (!reviews.Any())
                return 0;

            return Math.Round(reviews.Average(r => r.Rating), 1);
        }

        public async Task<int> GetProviderReviewCountAsync(string providerId)
        {
            return await _context.Reviews
                .CountAsync(r => r.ProviderId == providerId && !r.IsDeleted);
        }

        public async Task<bool> HasUserReviewedBookingAsync(Guid bookingId)
        {
            return await _context.Reviews
                .AnyAsync(r => r.BookingId == bookingId && !r.IsDeleted);
        }

        public async Task<List<UserReviewListDto>> GetUserReviewsAsync(string userId)
        {
            var reviews = await _context.Reviews
                .Where(r => r.UserId == userId && !r.IsDeleted)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            if (!reviews.Any())
                return new List<UserReviewListDto>();

            var serviceIds = reviews.Select(r => r.ServiceId).Distinct().ToList();
            var providerIds = reviews.Where(r => r.ProviderId != null).Select(r => r.ProviderId!).Distinct().ToList();

            var services = await _context.Services
                .Where(s => serviceIds.Contains(s.ServiceId))
                .ToDictionaryAsync(s => s.ServiceId);

            var providers = await _context.Users
                .Where(u => providerIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.FullName);

            return reviews.Select(r =>
            {
                var service = services.ContainsKey(r.ServiceId) ? services[r.ServiceId] : null;
                var providerName = r.ProviderId != null && providers.ContainsKey(r.ProviderId) ? providers[r.ProviderId] : "Provider";
                
                // Get first image
                string? serviceImage = null;
                if (service != null && !string.IsNullOrEmpty(service.ImagePaths))
                {
                    try
                    {
                        var images = System.Text.Json.JsonSerializer.Deserialize<List<string>>(service.ImagePaths);
                        serviceImage = images?.FirstOrDefault();
                    }
                    catch
                    {
                        // Fallback in case it's not JSON
                        serviceImage = null; 
                    }
                }

                return new UserReviewListDto
                {
                    ReviewId = r.ReviewId,
                    ServiceId = r.ServiceId,
                    ServiceName = service?.ServiceName ?? "Unknown Service",
                    ServiceImage = serviceImage,
                    ProviderName = providerName ?? "Unknown Provider",
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt
                };
            }).ToList();
        }

        public async Task<List<ProviderReviewListDto>> GetProviderReviewsAsync(string providerId)
        {
            var reviews = await _context.Reviews
                .Where(r => r.ProviderId == providerId && !r.IsDeleted)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            if (!reviews.Any())
                return new List<ProviderReviewListDto>();

            var serviceIds = reviews.Select(r => r.ServiceId).Distinct().ToList();
            var userIds = reviews.Where(r => r.UserId != null).Select(r => r.UserId!).Distinct().ToList();

            var services = await _context.Services
                .Where(s => serviceIds.Contains(s.ServiceId))
                .ToDictionaryAsync(s => s.ServiceId, s => s.ServiceName);

            var users = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);

            return reviews.Select(r =>
            {
                var user = r.UserId != null && users.ContainsKey(r.UserId) ? users[r.UserId] : null;

                return new ProviderReviewListDto
                {
                    ReviewId = r.ReviewId,
                    ServiceId = r.ServiceId,
                    ServiceName = services.ContainsKey(r.ServiceId) ? services[r.ServiceId] ?? "Service" : "Service",
                    UserName = user?.FullName ?? "Anonymous",
                    UserProfilePicture = user?.ProfilePictureUrl,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt
                };
            }).ToList();
        }
    }
}
