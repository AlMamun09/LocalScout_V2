using LocalScout.Application.DTOs.BookingDTOs;
using LocalScout.Application.Interfaces;
using LocalScout.Domain.Entities;
using LocalScout.Domain.Enums;
using LocalScout.Infrastructure.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace LocalScout.Web.Controllers
{
    [Authorize]
    public class BookingController : Controller
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly IServiceRepository _serviceRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<BookingController> _logger;

        private const long MaxImageSizeBytes = 5 * 1024 * 1024; // 5MB
        private const int MaxImagesPerBooking = 5;

        public BookingController(
            IBookingRepository bookingRepository,
            IServiceRepository serviceRepository,
            INotificationRepository notificationRepository,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment,
            ILogger<BookingController> logger)
        {
            _bookingRepository = bookingRepository;
            _serviceRepository = serviceRepository;
            _notificationRepository = notificationRepository;
            _userManager = userManager;
            _environment = environment;
            _logger = logger;
        }

        #region User Actions

        /// <summary>
        /// User's booking list page - initial page load
        /// </summary>
        [Authorize(Roles = RoleNames.User)]
        public IActionResult MyBookings()
        {
            return View();
        }

        /// <summary>
        /// AJAX endpoint to load bookings for a specific tab
        /// </summary>
        [Authorize(Roles = RoleNames.User)]
        [HttpGet]
        public async Task<IActionResult> GetUserBookingsTab(BookingStatus? status = null)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "User not authenticated." });
                }

                var bookings = await _bookingRepository.GetUserBookingsAsync(userId, status);
                var bookingDtos = new List<BookingListItemDto>();

                foreach (var booking in bookings)
                {
                    var dto = await MapToUserListItemDtoAsync(booking);
                    if (dto != null)
                    {
                        bookingDtos.Add(dto);
                    }
                }

                return PartialView("_UserBookingsList", bookingDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user bookings tab");
                return Json(new { success = false, message = "Failed to load bookings." });
            }
        }

        /// <summary>
        /// User views booking details
        /// </summary>
        [Authorize(Roles = RoleNames.User)]
        public async Task<IActionResult> UserDetails(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            var booking = await _bookingRepository.GetByIdAsync(id);
            if (booking == null || booking.UserId != userId)
            {
                return NotFound();
            }

            var dto = await MapToUserDtoAsync(booking);
            if (dto == null)
            {
                return NotFound();
            }

            return View(dto);
        }

        /// <summary>
        /// Create booking modal (AJAX)
        /// </summary>
        [Authorize(Roles = RoleNames.User)]
        [HttpGet]
        public async Task<IActionResult> GetCreateBookingModal(Guid serviceId)
        {
            var service = await _serviceRepository.GetServiceByIdAsync(serviceId);
            if (service == null || !service.IsActive || service.IsDeleted)
            {
                return NotFound(new { message = "Service not found or unavailable." });
            }

            var provider = await _userManager.FindByIdAsync(service.Id ?? "");
            if (provider == null || !provider.IsActive || !provider.IsVerified)
            {
                return NotFound(new { message = "Provider not available." });
            }

            var currentUser = await _userManager.GetUserAsync(User);

            var model = new CreateBookingModalDto
            {
                ServiceId = service.ServiceId,
                ServiceName = service.ServiceName ?? "Service",
                ServiceMinPrice = service.MinPrice,
                ServicePricingUnit = service.PricingUnit ?? "Fixed",
                ProviderName = provider.FullName ?? "Provider",
                ProviderBusinessName = provider.BusinessName,
                UserAddress = currentUser?.Address
            };

            return PartialView("_CreateBookingModal", model);
        }

        /// <summary>
        /// Create a new booking (AJAX)
        /// </summary>
        [Authorize(Roles = RoleNames.User)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateBookingDto dto, List<IFormFile>? images)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User not authenticated." });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return Unauthorized(new { message = "User not found." });
                }

                // Get service and provider
                var service = await _serviceRepository.GetServiceByIdAsync(dto.ServiceId);
                if (service == null || !service.IsActive || service.IsDeleted)
                {
                    return BadRequest(new { message = "Service not available." });
                }

                var provider = await _userManager.FindByIdAsync(service.Id ?? "");
                if (provider == null || !provider.IsActive || !provider.IsVerified)
                {
                    return BadRequest(new { message = "Provider not available." });
                }

                // Handle image uploads
                var imagePaths = new List<string>();
                if (images != null && images.Any())
                {
                    imagePaths = await SaveImagesAsync(images.Take(MaxImagesPerBooking).ToList(), "bookings");
                }

                // Create booking
                var booking = new Booking
                {
                    ServiceId = dto.ServiceId,
                    UserId = userId,
                    ProviderId = service.Id ?? "",
                    Description = dto.Description,
                    AddressArea = dto.AddressArea ?? user.Address,
                    ImagePaths = imagePaths.Any() ? JsonSerializer.Serialize(imagePaths) : null,
                    Status = BookingStatus.PendingProviderReview
                };

                await _bookingRepository.CreateAsync(booking);

                // Notify provider - simplified notification without metadata
                await _notificationRepository.CreateNotificationAsync(
                    provider.Id,
                    "New Booking Request",
                    $"You have a new booking request from {user.FullName} for your service '{service.ServiceName}'. Check your bookings to review and respond.",
                    null
                );

                return Json(new { success = true, message = "Booking request sent successfully! The provider will review and respond soon.", bookingId = booking.BookingId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating booking");
                return StatusCode(500, new { message = "Failed to create booking." });
            }
        }

        /// <summary>
        /// User cancels booking (AJAX)
        /// </summary>
        [Authorize(Roles = RoleNames.User)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UserCancel(CancelBookingDto dto)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User not authenticated." });
                }

                var booking = await _bookingRepository.GetByIdAsync(dto.BookingId);
                if (booking == null || booking.UserId != userId)
                {
                    return NotFound(new { message = "Booking not found." });
                }

                if (booking.Status != BookingStatus.PendingProviderReview &&
                    booking.Status != BookingStatus.AcceptedByProvider)
                {
                    return BadRequest(new { message = "Cannot cancel this booking at current status." });
                }

                var result = await _bookingRepository.CancelBookingAsync(dto.BookingId, "User", dto.Reason);
                if (!result)
                {
                    return BadRequest(new { message = "Failed to cancel booking." });
                }

                // Notify provider - simplified notification
                var user = await _userManager.FindByIdAsync(userId);
                await _notificationRepository.CreateNotificationAsync(
                    booking.ProviderId,
                    "Booking Cancelled",
                    $"{user?.FullName ?? "A user"} has cancelled their booking request. Check your bookings for details.",
                    null
                );

                return Json(new { success = true, message = "Booking cancelled successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling booking");
                return StatusCode(500, new { message = "Failed to cancel booking." });
            }
        }

        /// <summary>
        /// User confirms payment (simulated - actual payment integration would go here)
        /// </summary>
        [Authorize(Roles = RoleNames.User)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPayment(Guid bookingId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User not authenticated." });
                }

                var booking = await _bookingRepository.GetByIdAsync(bookingId);
                if (booking == null || booking.UserId != userId)
                {
                    return NotFound(new { message = "Booking not found." });
                }

                if (booking.Status != BookingStatus.AcceptedByProvider &&
                    booking.Status != BookingStatus.AwaitingPayment)
                {
                    return BadRequest(new { message = "Payment not available for this booking status." });
                }

                // Simulate payment success - in real implementation, integrate with payment gateway
                var result = await _bookingRepository.MarkPaymentReceivedAsync(bookingId);
                if (!result)
                {
                    return BadRequest(new { message = "Failed to process payment." });
                }

                // Notify provider - simplified notification
                var user = await _userManager.FindByIdAsync(userId);
                await _notificationRepository.CreateNotificationAsync(
                    booking.ProviderId,
                    "Payment Received",
                    $"{user?.FullName ?? "User"} has completed payment. You can now proceed with the job. Check your bookings for details.",
                    null
                );

                return Json(new { success = true, message = "Payment successful! The provider will now proceed with your service." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment");
                return StatusCode(500, new { message = "Failed to process payment." });
            }
        }

        /// <summary>
        /// User confirms job completion
        /// </summary>
        [Authorize(Roles = RoleNames.User)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmCompletion(Guid bookingId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User not authenticated." });
                }

                var booking = await _bookingRepository.GetByIdAsync(bookingId);
                if (booking == null || booking.UserId != userId)
                {
                    return NotFound(new { message = "Booking not found." });
                }

                if (booking.Status != BookingStatus.JobDone)
                {
                    return BadRequest(new { message = "Job completion not yet marked by provider." });
                }

                var result = await _bookingRepository.MarkCompletedAsync(bookingId);
                if (!result)
                {
                    return BadRequest(new { message = "Failed to confirm completion." });
                }

                // Notify provider - simplified notification
                var user = await _userManager.FindByIdAsync(userId);
                await _notificationRepository.CreateNotificationAsync(
                    booking.ProviderId,
                    "Job Completed",
                    $"{user?.FullName ?? "User"} has confirmed the job completion. Thank you for your service!",
                    null
                );

                return Json(new { success = true, message = "Job completion confirmed! Thank you for using our service." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming completion");
                return StatusCode(500, new { message = "Failed to confirm completion." });
            }
        }

        #endregion

        #region Provider Actions

        /// <summary>
        /// Provider's booking requests page - initial page load
        /// </summary>
        [Authorize(Roles = RoleNames.ServiceProvider)]
        public IActionResult ProviderBookings()
        {
            return View();
        }

        /// <summary>
        /// AJAX endpoint to load bookings for a specific tab (Provider)
        /// </summary>
        [Authorize(Roles = RoleNames.ServiceProvider)]
        [HttpGet]
        public async Task<IActionResult> GetProviderBookingsTab(BookingStatus? status = null)
        {
            try
            {
                var providerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(providerId))
                {
                    return Json(new { success = false, message = "Provider not authenticated." });
                }

                var bookings = await _bookingRepository.GetProviderBookingsAsync(providerId, status);
                var bookingDtos = new List<BookingListItemDto>();

                foreach (var booking in bookings)
                {
                    var dto = await MapToProviderListItemDtoAsync(booking);
                    if (dto != null)
                    {
                        bookingDtos.Add(dto);
                    }
                }

                return PartialView("_ProviderBookingsList", bookingDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading provider bookings tab");
                return Json(new { success = false, message = "Failed to load bookings." });
            }
        }

        /// <summary>
        /// Provider views booking details
        /// </summary>
        [Authorize(Roles = RoleNames.ServiceProvider)]
        public async Task<IActionResult> ProviderDetails(Guid id)
        {
            var providerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(providerId))
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            var booking = await _bookingRepository.GetByIdAsync(id);
            if (booking == null || booking.ProviderId != providerId)
            {
                return NotFound();
            }

            var dto = await MapToProviderDtoAsync(booking);
            if (dto == null)
            {
                return NotFound();
            }

            return View(dto);
        }

        /// <summary>
        /// Provider accepts booking and sets price (AJAX)
        /// </summary>
        [Authorize(Roles = RoleNames.ServiceProvider)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Accept(AcceptBookingDto dto)
        {
            try
            {
                var providerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(providerId))
                {
                    return Unauthorized(new { message = "Provider not authenticated." });
                }

                var booking = await _bookingRepository.GetByIdAsync(dto.BookingId);
                if (booking == null || booking.ProviderId != providerId)
                {
                    return NotFound(new { message = "Booking not found." });
                }

                if (booking.Status != BookingStatus.PendingProviderReview)
                {
                    return BadRequest(new { message = "This booking has already been processed." });
                }

                if (dto.NegotiatedPrice <= 0)
                {
                    return BadRequest(new { message = "Please enter a valid price." });
                }

                var result = await _bookingRepository.AcceptBookingAsync(dto.BookingId, dto.NegotiatedPrice, dto.ProviderNotes);
                if (!result)
                {
                    return BadRequest(new { message = "Failed to accept booking." });
                }

                // Notify user - simplified notification without price/id in metadata
                var provider = await _userManager.FindByIdAsync(providerId);
                var service = await _serviceRepository.GetServiceByIdAsync(booking.ServiceId);
                await _notificationRepository.CreateNotificationAsync(
                    booking.UserId,
                    "Booking Accepted!",
                    $"{provider?.FullName ?? "Provider"} has accepted your booking for '{service?.ServiceName}'. You can now view their contact details and proceed to payment. Check your bookings for details.",
                    null
                );

                return Json(new { success = true, message = "Booking accepted! The user has been notified and can now proceed with payment." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting booking");
                return StatusCode(500, new { message = "Failed to accept booking." });
            }
        }

        /// <summary>
        /// Provider cancels/declines booking (AJAX)
        /// </summary>
        [Authorize(Roles = RoleNames.ServiceProvider)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProviderCancel(CancelBookingDto dto)
        {
            try
            {
                var providerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(providerId))
                {
                    return Unauthorized(new { message = "Provider not authenticated." });
                }

                var booking = await _bookingRepository.GetByIdAsync(dto.BookingId);
                if (booking == null || booking.ProviderId != providerId)
                {
                    return NotFound(new { message = "Booking not found." });
                }

                if (booking.Status != BookingStatus.PendingProviderReview &&
                    booking.Status != BookingStatus.AcceptedByProvider)
                {
                    return BadRequest(new { message = "Cannot cancel this booking at current status." });
                }

                var result = await _bookingRepository.CancelBookingAsync(dto.BookingId, "Provider", dto.Reason);
                if (!result)
                {
                    return BadRequest(new { message = "Failed to cancel booking." });
                }

                // Notify user - simplified notification
                var provider = await _userManager.FindByIdAsync(providerId);
                await _notificationRepository.CreateNotificationAsync(
                    booking.UserId,
                    "Booking Declined",
                    $"{provider?.FullName ?? "The provider"} has declined your booking request." +
                    (!string.IsNullOrEmpty(dto.Reason) ? $" Reason: {dto.Reason}" : "") + " Check your bookings for details.",
                    null
                );

                return Json(new { success = true, message = "Booking declined successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error declining booking");
                return StatusCode(500, new { message = "Failed to decline booking." });
            }
        }

        /// <summary>
        /// Provider marks job as done (AJAX)
        /// </summary>
        [Authorize(Roles = RoleNames.ServiceProvider)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkJobDone(Guid bookingId)
        {
            try
            {
                var providerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(providerId))
                {
                    return Unauthorized(new { message = "Provider not authenticated." });
                }

                var booking = await _bookingRepository.GetByIdAsync(bookingId);
                if (booking == null || booking.ProviderId != providerId)
                {
                    return NotFound(new { message = "Booking not found." });
                }

                if (booking.Status != BookingStatus.PaymentReceived &&
                    booking.Status != BookingStatus.InProgress)
                {
                    return BadRequest(new { message = "Cannot mark job done at current status." });
                }

                var result = await _bookingRepository.MarkJobDoneAsync(bookingId);
                if (!result)
                {
                    return BadRequest(new { message = "Failed to update status." });
                }

                // Notify user - simplified notification
                var provider = await _userManager.FindByIdAsync(providerId);
                await _notificationRepository.CreateNotificationAsync(
                    booking.UserId,
                    "Job Completed by Provider",
                    $"{provider?.FullName ?? "The provider"} has marked the job as completed. Please confirm if you are satisfied with the service. Check your bookings to confirm.",
                    null
                );

                return Json(new { success = true, message = "Job marked as done! Waiting for user confirmation." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking job done");
                return StatusCode(500, new { message = "Failed to update status." });
            }
        }

        #endregion

        #region Helper Methods

        private async Task<BookingDetailsForUserDto?> MapToUserDtoAsync(Booking booking)
        {
            var service = await _serviceRepository.GetServiceByIdAsync(booking.ServiceId);
            var provider = await _userManager.FindByIdAsync(booking.ProviderId);

            if (service == null || provider == null)
            {
                return null;
            }

            var imagePaths = new List<string>();
            if (!string.IsNullOrEmpty(booking.ImagePaths))
            {
                try
                {
                    imagePaths = JsonSerializer.Deserialize<List<string>>(booking.ImagePaths) ?? new List<string>();
                }
                catch { }
            }

            var serviceImagePaths = new List<string>();
            if (!string.IsNullOrEmpty(service.ImagePaths))
            {
                try
                {
                    serviceImagePaths = JsonSerializer.Deserialize<List<string>>(service.ImagePaths) ?? new List<string>();
                }
                catch { }
            }

            // CRITICAL: Provider contact info only visible after acceptance
            var canSeeProviderContact = booking.Status != BookingStatus.PendingProviderReview &&
                                        booking.Status != BookingStatus.Cancelled;

            return new BookingDetailsForUserDto
            {
                BookingId = booking.BookingId,
                ServiceId = booking.ServiceId,
                ServiceName = service.ServiceName ?? "Service",
                ServiceDescription = service.Description,
                ServiceMinPrice = service.MinPrice,
                ServicePricingUnit = service.PricingUnit,
                ServiceImagePath = serviceImagePaths.FirstOrDefault(),

                ProviderId = provider.Id,
                ProviderName = provider.FullName ?? "Provider",
                ProviderBusinessName = provider.BusinessName,
                ProviderProfilePicture = provider.ProfilePictureUrl,
                IsProviderVerified = provider.IsVerified,

                // SECURITY: Only include contact info if allowed
                ProviderEmail = canSeeProviderContact ? provider.Email : null,
                ProviderPhone = canSeeProviderContact ? provider.PhoneNumber : null,
                ProviderAddress = canSeeProviderContact ? provider.Address : null,
                CanSeeProviderContact = canSeeProviderContact,

                Description = booking.Description,
                ImagePaths = imagePaths,
                AddressArea = booking.AddressArea,
                NegotiatedPrice = booking.NegotiatedPrice,
                ProviderNotes = booking.ProviderNotes,

                Status = booking.Status,
                StatusDisplay = GetStatusDisplayText(booking.Status),

                CreatedAt = booking.CreatedAt,
                AcceptedAt = booking.AcceptedAt,
                PaymentReceivedAt = booking.PaymentReceivedAt,
                JobDoneAt = booking.JobDoneAt,
                CompletedAt = booking.CompletedAt
            };
        }

        private async Task<BookingListItemDto?> MapToUserListItemDtoAsync(Booking booking)
        {
            var service = await _serviceRepository.GetServiceByIdAsync(booking.ServiceId);
            var provider = await _userManager.FindByIdAsync(booking.ProviderId);

            if (service == null || provider == null)
            {
                return null;
            }

            var imagePaths = new List<string>();
            if (!string.IsNullOrEmpty(booking.ImagePaths))
            {
                try
                {
                    imagePaths = JsonSerializer.Deserialize<List<string>>(booking.ImagePaths) ?? new List<string>();
                }
                catch { }
            }

            var serviceImagePaths = new List<string>();
            if (!string.IsNullOrEmpty(service.ImagePaths))
            {
                try
                {
                    serviceImagePaths = JsonSerializer.Deserialize<List<string>>(service.ImagePaths) ?? new List<string>();
                }
                catch { }
            }

            var canSeeProviderContact = booking.Status != BookingStatus.PendingProviderReview &&
                                        booking.Status != BookingStatus.Cancelled;

            return new BookingListItemDto
            {
                BookingId = booking.BookingId,
                ServiceName = service.ServiceName ?? "Service",
                ServiceImagePath = serviceImagePaths.FirstOrDefault(),
                ServiceMinPrice = service.MinPrice,

                ProviderName = provider.FullName ?? "Provider",
                ProviderBusinessName = provider.BusinessName,
                ProviderProfilePicture = provider.ProfilePictureUrl,
                IsProviderVerified = provider.IsVerified,
                ProviderPhone = canSeeProviderContact ? provider.PhoneNumber : null,
                ProviderEmail = canSeeProviderContact ? provider.Email : null,
                ProviderAddress = canSeeProviderContact ? provider.Address : null,
                CanSeeProviderContact = canSeeProviderContact,

                Description = booking.Description,
                AddressArea = booking.AddressArea,
                NegotiatedPrice = booking.NegotiatedPrice,
                ImagePaths = imagePaths,

                Status = booking.Status,
                StatusDisplay = GetStatusDisplayText(booking.Status),
                StatusBadgeClass = GetStatusBadgeClass(booking.Status),

                CreatedAt = booking.CreatedAt,
                CreatedAtFormatted = booking.CreatedAt.ToString("MMM dd, yyyy hh:mm tt"),
                AcceptedAt = booking.AcceptedAt,
                AcceptedAtFormatted = booking.AcceptedAt?.ToString("MMM dd, yyyy"),

                CanPay = booking.Status == BookingStatus.AcceptedByProvider || booking.Status == BookingStatus.AwaitingPayment,
                CanCancel = booking.Status == BookingStatus.PendingProviderReview || booking.Status == BookingStatus.AcceptedByProvider,
                CanConfirmCompletion = booking.Status == BookingStatus.JobDone
            };
        }

        private async Task<BookingDetailsForProviderDto?> MapToProviderDtoAsync(Booking booking)
        {
            var service = await _serviceRepository.GetServiceByIdAsync(booking.ServiceId);
            var user = await _userManager.FindByIdAsync(booking.UserId);

            if (service == null || user == null)
            {
                return null;
            }

            var imagePaths = new List<string>();
            if (!string.IsNullOrEmpty(booking.ImagePaths))
            {
                try
                {
                    imagePaths = JsonSerializer.Deserialize<List<string>>(booking.ImagePaths) ?? new List<string>();
                }
                catch { }
            }

            // PROVIDERS ALWAYS SEE USER CONTACT INFO
            return new BookingDetailsForProviderDto
            {
                BookingId = booking.BookingId,
                ServiceId = booking.ServiceId,
                ServiceName = service.ServiceName ?? "Service",
                ServiceDescription = service.Description,
                ServiceMinPrice = service.MinPrice,
                ServicePricingUnit = service.PricingUnit,

                // User info - ALWAYS visible to provider
                UserId = user.Id,
                UserName = user.FullName ?? "User",
                UserEmail = user.Email,
                UserPhone = user.PhoneNumber,
                UserProfilePicture = user.ProfilePictureUrl,
                UserAddress = user.Address,

                Description = booking.Description,
                ImagePaths = imagePaths,
                AddressArea = booking.AddressArea,
                NegotiatedPrice = booking.NegotiatedPrice,
                ProviderNotes = booking.ProviderNotes,

                Status = booking.Status,
                StatusDisplay = GetStatusDisplayText(booking.Status),

                CreatedAt = booking.CreatedAt,
                AcceptedAt = booking.AcceptedAt,
                PaymentReceivedAt = booking.PaymentReceivedAt,
                JobDoneAt = booking.JobDoneAt,
                CompletedAt = booking.CompletedAt
            };
        }

        private async Task<BookingListItemDto?> MapToProviderListItemDtoAsync(Booking booking)
        {
            var service = await _serviceRepository.GetServiceByIdAsync(booking.ServiceId);
            var user = await _userManager.FindByIdAsync(booking.UserId);

            if (service == null || user == null)
            {
                return null;
            }

            var imagePaths = new List<string>();
            if (!string.IsNullOrEmpty(booking.ImagePaths))
            {
                try
                {
                    imagePaths = JsonSerializer.Deserialize<List<string>>(booking.ImagePaths) ?? new List<string>();
                }
                catch { }
            }

            return new BookingListItemDto
            {
                BookingId = booking.BookingId,
                ServiceName = service.ServiceName ?? "Service",
                ServiceMinPrice = service.MinPrice,

                // User info - ALWAYS visible to provider
                UserName = user.FullName ?? "User",
                UserProfilePicture = user.ProfilePictureUrl,
                UserPhone = user.PhoneNumber,
                UserEmail = user.Email,
                UserAddress = user.Address,

                Description = booking.Description,
                AddressArea = booking.AddressArea,
                NegotiatedPrice = booking.NegotiatedPrice,
                ImagePaths = imagePaths,

                Status = booking.Status,
                StatusDisplay = GetStatusDisplayText(booking.Status),
                StatusBadgeClass = GetStatusBadgeClass(booking.Status),

                CreatedAt = booking.CreatedAt,
                CreatedAtFormatted = booking.CreatedAt.ToString("MMM dd, yyyy hh:mm tt"),
                AcceptedAt = booking.AcceptedAt,
                AcceptedAtFormatted = booking.AcceptedAt?.ToString("MMM dd, yyyy"),

                CanAcceptAndSetPrice = booking.Status == BookingStatus.PendingProviderReview,
                CanMarkJobDone = booking.Status == BookingStatus.PaymentReceived || booking.Status == BookingStatus.InProgress,
                CanCancel = booking.Status == BookingStatus.PendingProviderReview || booking.Status == BookingStatus.AcceptedByProvider
            };
        }

        private static string GetStatusDisplayText(BookingStatus status)
        {
            return status switch
            {
                BookingStatus.PendingProviderReview => "Pending Review",
                BookingStatus.AcceptedByProvider => "Accepted - Awaiting Payment",
                BookingStatus.AwaitingPayment => "Awaiting Payment",
                BookingStatus.PaymentReceived => "Payment Received",
                BookingStatus.InProgress => "In Progress",
                BookingStatus.JobDone => "Job Done - Awaiting Confirmation",
                BookingStatus.Completed => "Completed",
                BookingStatus.Cancelled => "Cancelled",
                BookingStatus.Disputed => "Disputed",
                _ => status.ToString()
            };
        }

        private static string GetStatusBadgeClass(BookingStatus status)
        {
            return status switch
            {
                BookingStatus.PendingProviderReview => "badge-warning",
                BookingStatus.AcceptedByProvider => "badge-info",
                BookingStatus.AwaitingPayment => "badge-info",
                BookingStatus.PaymentReceived => "badge-primary",
                BookingStatus.InProgress => "badge-primary",
                BookingStatus.JobDone => "badge-success",
                BookingStatus.Completed => "badge-success",
                BookingStatus.Cancelled => "badge-danger",
                BookingStatus.Disputed => "badge-danger",
                _ => "badge-secondary"
            };
        }

        private async Task<List<string>> SaveImagesAsync(List<IFormFile> images, string folder)
        {
            var imagePaths = new List<string>();
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", folder);

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            foreach (var image in images)
            {
                if (image != null && image.Length > 0 && image.Length <= MaxImageSizeBytes)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                    var extension = Path.GetExtension(image.FileName).ToLower();

                    if (!allowedExtensions.Contains(extension))
                    {
                        continue;
                    }

                    var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await image.CopyToAsync(fileStream);
                    }

                    imagePaths.Add($"/uploads/{folder}/{uniqueFileName}");
                }
            }

            return imagePaths;
        }

        #endregion
    }
}
