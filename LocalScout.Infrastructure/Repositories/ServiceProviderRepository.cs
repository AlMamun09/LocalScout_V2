using LocalScout.Application.DTOs;
using LocalScout.Application.DTOs.BookingDTOs;
using LocalScout.Application.Interfaces;
using LocalScout.Domain.Entities;
using LocalScout.Infrastructure.Constants;
using LocalScout.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;

namespace LocalScout.Infrastructure.Repositories
{
    public class ServiceProviderRepository : IServiceProviderRepository
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context; // Added DbContext

        public ServiceProviderRepository(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context
        )
        {
            _userManager = userManager;
            _context = context;
        }

        // --- NEW: Dynamic Dashboard Data ---
        public async Task<ProviderDashboardDto> GetProviderDashboardAsync(string providerId)
        {
            // 1. Initialize default DTO
            var dashboard = new ProviderDashboardDto();

            // 2. Fetch Verification Status (Logic is handled in VerificationRepo, but we can double check here if needed)
            // For this DTO, we usually let the Controller merge the Verification Request info.

            // 3. Fetch KPIs from Database
            // NOTE: Ensure you have 'Bookings' and 'Reviews' DbSets in your ApplicationDbContext
            // If these entities don't exist yet, these queries will need to be uncommented once created.

            /* -----------------------------------------------------------------
             * UNCOMMENT THE BLOCK BELOW ONCE 'Booking' AND 'Review' ENTITIES EXIST
             * ----------------------------------------------------------------- */

            // Example Logic:
            // var bookingsQuery = _context.Bookings.Where(b => b.ProviderId == providerId);
            // var reviewsQuery = _context.Reviews.Where(r => r.ProviderId == providerId);

            // dashboard.TotalBookings = await bookingsQuery.CountAsync();
            // dashboard.PendingRequestsCount = await bookingsQuery.CountAsync(b => b.Status == "Pending");

            // // Assuming 'IsPaid' and 'TotalAmount' exist on Booking
            // dashboard.TotalEarnings = await bookingsQuery
            //     .Where(b => b.Status == "Completed" && b.IsPaid)
            //     .SumAsync(b => b.TotalAmount);

            // dashboard.AverageRating = await reviewsQuery.AnyAsync()
            //     ? await reviewsQuery.AverageAsync(r => r.Rating)
            //     : 0.0;

            // // Fetch Recent Bookings List
            // dashboard.RecentBookings = await bookingsQuery
            //     .OrderByDescending(b => b.BookingDate)
            //     .Take(5)
            //     .Select(b => new BookingDto
            //     {
            //         CustomerName = b.Customer.FullName,
            //         ServiceName = b.Service.ServiceName,
            //         Date = b.BookingDate.ToString("MMM dd, yyyy"),
            //         Location = b.Address,
            //         Status = b.Status.ToString() // Ensure Status is string or Enum
            //     })
            //     .ToListAsync();

            /* -----------------------------------------------------------------
             * END DB QUERY BLOCK
             * ----------------------------------------------------------------- */

            // --- TEMPORARY FALLBACK (To keep app running without DB errors) ---
            // Remove this block when you implement the actual entities above.
            dashboard.TotalEarnings = 0;
            dashboard.TotalBookings = 0;
            dashboard.PendingRequestsCount = 0;
            dashboard.AverageRating = 0.0m;
            dashboard.RecentBookings = new List<BookingDto>();
            // ----------------------------------------------------------------

            return dashboard;
        }

        public async Task<IEnumerable<ServiceProviderDto>> GetAllProvidersAsync()
        {
            var providers = await _userManager.GetUsersInRoleAsync(RoleNames.ServiceProvider);
            return providers.Select(MapToDto).ToList();
        }

        public async Task<IEnumerable<ServiceProviderDto>> GetProvidersByStatusAsync(bool isActive)
        {
            var providers = await _userManager.GetUsersInRoleAsync(RoleNames.ServiceProvider);
            var filteredProviders = providers.Where(p => p.IsActive == isActive);
            return filteredProviders.Select(MapToDto).ToList();
        }

        public async Task<IEnumerable<ServiceProviderDto>> GetVerificationRequestsAsync()
        {
            var providers = await _userManager.GetUsersInRoleAsync(RoleNames.ServiceProvider);
            // Filter for providers that are NOT verified yet
            var pendingProviders = providers.Where(p => !p.IsVerified);
            return pendingProviders.Select(MapToDto).ToList();
        }

        public async Task<ServiceProviderDto?> GetProviderByIdAsync(string providerId)
        {
            var provider = await _userManager.FindByIdAsync(providerId);
            if (provider == null)
                return null;

            var roles = await _userManager.GetRolesAsync(provider);
            if (!roles.Contains(RoleNames.ServiceProvider))
                return null;

            return MapToDto(provider);
        }

        public async Task<bool> ToggleProviderStatusAsync(string providerId)
        {
            var provider = await _userManager.FindByIdAsync(providerId);
            if (provider == null)
                return false;

            provider.IsActive = !provider.IsActive;
            provider.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(provider);
            return result.Succeeded;
        }

        public async Task<bool> ApproveProviderAsync(string providerId)
        {
            var provider = await _userManager.FindByIdAsync(providerId);
            if (provider == null)
                return false;

            provider.IsVerified = true;
            provider.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(provider);
            return result.Succeeded;
        }

        public async Task<bool> RejectProviderAsync(string providerId)
        {
            var provider = await _userManager.FindByIdAsync(providerId);
            if (provider == null)
                return false;

            provider.IsVerified = false;
            provider.IsActive = false;
            provider.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(provider);
            return result.Succeeded;
        }

        // Helper to map Entity to DTO
        private static ServiceProviderDto MapToDto(ApplicationUser provider)
        {
            string verificationStatus = provider.IsVerified ? "Approved" : "Pending";

            return new ServiceProviderDto
            {
                Id = provider.Id,
                FullName = provider.FullName,
                Email = provider.Email,
                PhoneNumber = provider.PhoneNumber ?? "N/A",
                ProfilePictureUrl = provider.ProfilePictureUrl,
                IsActive = provider.IsActive,
                CreatedAt = provider.CreatedAt,
                UpdatedAt = provider.UpdatedAt,
                Address = provider.Address ?? "Not Provided",
                Gender = provider.Gender ?? "N/A",
                EmailConfirmed = provider.EmailConfirmed,
                Role = RoleNames.ServiceProvider,
                BusinessName = provider.BusinessName ?? "N/A",
                Description = provider.Description ?? "No description provided",
                IsVerified = provider.IsVerified,
                VerifiedAt = provider.IsVerified ? provider.UpdatedAt : null,
                VerificationStatus = verificationStatus,
                TotalServices = 0, // Placeholder until Service Entity exists
                TotalBookings = 0, // Placeholder until Booking Entity exists
                AverageRating = 0.0, // Placeholder until Review Entity exists
            };
        }
    }
}
