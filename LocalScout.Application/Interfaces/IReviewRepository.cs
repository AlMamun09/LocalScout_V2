using LocalScout.Application.DTOs.ReviewDTOs;
using LocalScout.Domain.Entities;

namespace LocalScout.Application.Interfaces
{
    public interface IReviewRepository
    {
        /// <summary>
        /// Create a new review
        /// </summary>
        Task<bool> CreateReviewAsync(Review review);

        /// <summary>
        /// Get review by booking ID
        /// </summary>
        Task<Review?> GetByBookingIdAsync(Guid bookingId);

        /// <summary>
        /// Get all reviews for a service (for calculating average rating)
        /// </summary>
        Task<List<Review>> GetReviewsByServiceIdAsync(Guid serviceId);

        /// <summary>
        /// Get reviews for a service with pagination
        /// </summary>
        Task<List<ReviewDisplayDto>> GetServiceReviewsAsync(Guid serviceId, int page = 1, int pageSize = 10);

        /// <summary>
        /// Get rating summary for a service
        /// </summary>
        Task<ServiceRatingDto> GetServiceRatingAsync(Guid serviceId);

        /// <summary>
        /// Get average rating for a provider (across all their services)
        /// </summary>
        Task<double> GetProviderAverageRatingAsync(string providerId);

        /// <summary>
        /// Check if user has already reviewed a booking
        /// </summary>
        Task<bool> HasUserReviewedBookingAsync(Guid bookingId);

        /// <summary>
        /// Get total review count for a provider
        /// </summary>
        Task<int> GetProviderReviewCountAsync(string providerId);

        /// <summary>
        /// Get all reviews submitted by a user
        /// </summary>
        Task<List<UserReviewListDto>> GetUserReviewsAsync(string userId);

        /// <summary>
        /// Get all reviews received by a provider
        /// </summary>
        Task<List<ProviderReviewListDto>> GetProviderReviewsAsync(string providerId);

        /// <summary>
        /// Get all reviews for admin reports
        /// </summary>
        Task<List<Review>> GetAllReviewsAsync();
    }
}
