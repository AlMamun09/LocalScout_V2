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
        private readonly IReviewRepository _reviewRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<BookingController> _logger;
        private readonly IAuditService _auditService;
        private readonly ISchedulingService _schedulingService;
        private readonly IProviderTimeSlotRepository _timeSlotRepository;
        private readonly IServiceBlockRepository _serviceBlockRepository;

        private const long MaxImageSizeBytes = 5 * 1024 * 1024; // 5MB
        private const int MaxImagesPerBooking = 5;

        public BookingController(
            IBookingRepository bookingRepository,
            IServiceRepository serviceRepository,
            INotificationRepository notificationRepository,
            IReviewRepository reviewRepository,
            UserManager<ApplicationUser> userManager,
            IWebHostEnvironment environment,
            ILogger<BookingController> logger,
            IAuditService auditService,
            ISchedulingService schedulingService,
            IProviderTimeSlotRepository timeSlotRepository,
            IServiceBlockRepository serviceBlockRepository)
        {
            _bookingRepository = bookingRepository;
            _serviceRepository = serviceRepository;
            _notificationRepository = notificationRepository;
            _reviewRepository = reviewRepository;
            _userManager = userManager;
            _environment = environment;
            _logger = logger;
            _auditService = auditService;
            _schedulingService = schedulingService;
            _timeSlotRepository = timeSlotRepository;
            _serviceBlockRepository = serviceBlockRepository;
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
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                // Check if user is blocked
                var currentUser = await _userManager.FindByIdAsync(userId);
                if (currentUser != null && !currentUser.IsActive)
                {
                    return BadRequest(new { 
                        message = $"Your account has been blocked. Reason: {currentUser.BlockReason ?? "No reason provided"}. You cannot book services. Please contact support for assistance.",
                        isBlocked = true
                    });
                }

                // Check if user has pending completion
                var hasPendingCompletion = await _bookingRepository.HasPendingCompletionAsync(userId);
                if (hasPendingCompletion)
                {
                    return BadRequest(new { 
                        message = "You have a booking awaiting completion confirmation. Please confirm the previous service completion before booking a new service.",
                        hasPendingCompletion = true
                    });
                }
            }

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

            var userForAddress = await _userManager.GetUserAsync(User);

            // Get provider duty hours for display
            var dutyHours = await _schedulingService.GetProviderDutyHoursAsync(service.Id ?? "");

            var model = new CreateBookingModalDto
            {
                ServiceId = service.ServiceId,
                ServiceName = service.ServiceName ?? "Service",
                ServiceMinPrice = service.MinPrice,
                ServicePricingUnit = service.PricingUnit ?? "Fixed",
                ProviderId = service.Id ?? "",
                ProviderName = provider.FullName ?? "Provider",
                ProviderBusinessName = provider.BusinessName,
                UserAddress = userForAddress?.Address,
                // Add duty hours info for display
                ProviderWorkingHours = dutyHours.RawValue
            };

            return PartialView("_CreateBookingModal", model);
        }

        /// <summary>
        /// AJAX endpoint to validate time slot before booking submission
        /// </summary>
        [Authorize(Roles = RoleNames.User)]
        [HttpPost]
        public async Task<IActionResult> ValidateTimeSlot([FromBody] TimeValidationRequestDto request)
        {
            try
            {
                var response = new TimeValidationResponseDto();

                // Check minimum lead time (2 hours)
                response.HasMinimumLeadTime = _schedulingService.ValidateMinimumLeadTime(
                    request.RequestedDate, request.StartTime);
                
                if (!response.HasMinimumLeadTime)
                {
                    response.IsValid = false;
                    response.ErrorMessage = "Bookings must be made at least 2 hours in advance.";
                    return Json(response);
                }

                // Check duty hours
                var dutyCheck = await _schedulingService.IsWithinProviderDutyHoursAsync(
                    request.ProviderId, request.StartTime, request.EndTime);
                
                response.IsWithinDutyHours = dutyCheck.IsWithinHours;
                if (!response.IsWithinDutyHours && dutyCheck.DutyStart.HasValue && dutyCheck.DutyEnd.HasValue)
                {
                    response.DutyHoursMessage = $"Provider works from {DateTime.Today.Add(dutyCheck.DutyStart.Value):h:mm tt} to {DateTime.Today.Add(dutyCheck.DutyEnd.Value):h:mm tt}.";
                }

                // Check provider availability
                var startDateTime = _schedulingService.CombineDateAndTime(request.RequestedDate, request.StartTime);
                var endDateTime = _schedulingService.CombineDateAndTime(request.RequestedDate, request.EndTime);
                
                var availabilityCheck = await _schedulingService.CheckProviderAvailabilityAsync(
                    request.ProviderId, startDateTime, endDateTime);
                
                response.IsProviderAvailable = availabilityCheck.IsAvailable;

                // Set overall validity
                response.IsValid = response.HasMinimumLeadTime && 
                                  response.IsWithinDutyHours && 
                                  response.IsProviderAvailable;
                
                if (!response.IsValid && string.IsNullOrEmpty(response.ErrorMessage))
                {
                    if (!response.IsWithinDutyHours)
                    {
                        response.ErrorMessage = response.DutyHoursMessage ?? "Selected time is outside provider's working hours.";
                    }
                    else if (!response.IsProviderAvailable)
                    {
                        response.ErrorMessage = availabilityCheck.Message ?? "Provider is not available at the selected time.";
                    }
                }

                return Json(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating time slot");
                return Json(new TimeValidationResponseDto { IsValid = false, ErrorMessage = "Failed to validate time slot." });
            }
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

                // Check if user is blocked
                if (!user.IsActive)
                {
                    return BadRequest(new { 
                        message = $"Your account has been blocked. Reason: {user.BlockReason ?? "No reason provided"}. You cannot book services.",
                        isBlocked = true
                    });
                }

                // Get service and provider
                var service = await _serviceRepository.GetServiceByIdAsync(dto.ServiceId);
                if (service == null || !service.IsActive || service.IsDeleted)
                {
                    return BadRequest(new { message = "Service not available." });
                }

                // Check if service is blocked
                var isBlocked = await _serviceBlockRepository.IsServiceBlockedAsync(dto.ServiceId);
                if (isBlocked)
                {
                    return BadRequest(new { message = "This service is temporarily unavailable. Please try again later." });
                }

                var provider = await _userManager.FindByIdAsync(service.Id ?? "");
                if (provider == null || !provider.IsActive || !provider.IsVerified)
                {
                    return BadRequest(new { message = "Provider not available." });
                }

                // Check user restriction: cannot re-request same service while previous is pending/accepted
                var hasActiveBooking = await _bookingRepository.HasActiveBookingForServiceAsync(userId, dto.ServiceId);
                if (hasActiveBooking)
                {
                    return BadRequest(new { message = "You already have an active booking for this service. Please wait until it is completed or cancelled." });
                }

                // Validate time slot
                var timeValidation = await _schedulingService.ValidateBookingTimeAsync(
                    service.Id ?? "",
                    dto.RequestedDate,
                    dto.RequestedStartTime,
                    dto.RequestedEndTime);

                if (!timeValidation.IsValid)
                {
                    return BadRequest(new { message = timeValidation.ErrorMessage });
                }

                // Handle image uploads
                var imagePaths = new List<string>();
                if (images != null && images.Any())
                {
                    imagePaths = await SaveImagesAsync(images.Take(MaxImagesPerBooking).ToList(), "bookings");
                }

                // Create booking with time information
                var booking = new Booking
                {
                    ServiceId = dto.ServiceId,
                    UserId = userId,
                    ProviderId = service.Id ?? "",
                    Description = dto.Description,
                    AddressArea = dto.AddressArea ?? user.Address,
                    ImagePaths = imagePaths.Any() ? JsonSerializer.Serialize(imagePaths) : null,
                    Status = BookingStatus.PendingProviderReview,
                    // Store requested time
                    RequestedDate = dto.RequestedDate,
                    RequestedStartTime = dto.RequestedStartTime,
                    RequestedEndTime = dto.RequestedEndTime
                };

                await _bookingRepository.CreateAsync(booking);

                // Notify provider with time information
                var startTimeStr = DateTime.Today.Add(dto.RequestedStartTime).ToString("h:mm tt");
                var endTimeStr = DateTime.Today.Add(dto.RequestedEndTime).ToString("h:mm tt");
                await _notificationRepository.CreateNotificationAsync(
                    provider.Id,
                    "New Booking Request",
                    $"You have a new booking request from {user.FullName} for '{service.ServiceName}' on {dto.RequestedDate:MMM dd, yyyy} from {startTimeStr} to {endTimeStr}. Check your bookings to review and respond.",
                    null
                );

                // Audit Log: Booking Created
                await _auditService.LogAsync(
                    user.Id,
                    user.FullName,
                    user.Email,
                    "BookingCreated",
                    "Booking",
                    "Booking",
                    booking.BookingId.ToString(),
                    $"User created booking request for service '{service.ServiceName}' on {dto.RequestedDate:yyyy-MM-dd} {startTimeStr}-{endTimeStr}"
                );

                return Json(new { success = true, message = "Booking request sent successfully! The provider will review and respond within 3 hours.", bookingId = booking.BookingId });
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

                // Allow cancellation for pending, accepted, or need rescheduling bookings
                if (booking.Status != BookingStatus.PendingProviderReview &&
                    booking.Status != BookingStatus.AcceptedByProvider &&
                    booking.Status != BookingStatus.NeedRescheduling)
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

                // Audit Log: Booking Cancelled by User
                await _auditService.LogAsync(
                    userId,
                    user?.FullName,
                    user?.Email,
                    "BookingCancelled",
                    "Booking",
                    "Booking",
                    booking.BookingId.ToString(),
                    $"User cancelled booking. Reason: {dto.Reason ?? "None provided"}"
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
        /// User reschedules a booking by submitting a new requested time
        /// </summary>
        [Authorize(Roles = RoleNames.User)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UserReschedule(RescheduleBookingDto dto)
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

                // Only allow rescheduling for NeedRescheduling status
                if (booking.Status != BookingStatus.NeedRescheduling)
                {
                    return BadRequest(new { message = "This booking cannot be rescheduled at current status." });
                }

                // Validate new time slot
                var newStartDateTime = _schedulingService.CombineDateAndTime(dto.RequestedDate, dto.RequestedStartTime);
                var newEndDateTime = _schedulingService.CombineDateAndTime(dto.RequestedDate, dto.RequestedEndTime);

                if (newEndDateTime <= newStartDateTime)
                {
                    return BadRequest(new { message = "End time must be after start time." });
                }

                // Validate minimum lead time (2 hours)
                var isLeadTimeValid = _schedulingService.ValidateMinimumLeadTime(dto.RequestedDate, dto.RequestedStartTime);
                if (!isLeadTimeValid)
                {
                    return BadRequest(new { message = "Booking must be at least 2 hours in advance." });
                }

                // Update booking with new requested time and reset to pending
                booking.RequestedDate = dto.RequestedDate;
                booking.RequestedStartTime = dto.RequestedStartTime;
                booking.RequestedEndTime = dto.RequestedEndTime;
                booking.Status = BookingStatus.PendingProviderReview;
                booking.UpdatedAt = DateTime.Now;

                await _bookingRepository.UpdateAsync(booking);

                // Notify provider about the new proposed time
                var user = await _userManager.FindByIdAsync(userId);
                await _notificationRepository.CreateNotificationAsync(
                    booking.ProviderId,
                    "Booking Rescheduled",
                    $"{user?.FullName ?? "A user"} has proposed a new time for their booking. Please review and accept the request.",
                    null
                );

                // Audit Log
                await _auditService.LogAsync(
                    userId,
                    user?.FullName,
                    user?.Email,
                    "BookingRescheduled",
                    "Booking",
                    "Booking",
                    booking.BookingId.ToString(),
                    $"User rescheduled booking to {dto.RequestedDate:d} at {dto.RequestedStartTime} - {dto.RequestedEndTime}",
                    true
                );

                return Ok(new { success = true, message = "Your booking has been rescheduled. Waiting for provider confirmation." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rescheduling booking");
                return StatusCode(500, new { message = "Failed to reschedule booking." });
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

                // Audit Log: Payment Confirmed
                await _auditService.LogAsync(
                    userId,
                    user?.FullName,
                    user?.Email,
                    "PaymentConfirmed",
                    "Payment",
                    "Booking",
                    booking.BookingId.ToString(),
                    $"User confirmed payment for booking."
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
        /// User confirms job completion (with optional review)
        /// </summary>
        [Authorize(Roles = RoleNames.User)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmCompletion(Guid bookingId, int? rating = null, string? comment = null)
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

                // Mark booking as completed
                var result = await _bookingRepository.MarkCompletedAsync(bookingId);
                if (!result)
                {
                    return BadRequest(new { message = "Failed to confirm completion." });
                }

                var user = await _userManager.FindByIdAsync(userId);
                var service = await _serviceRepository.GetServiceByIdAsync(booking.ServiceId);

                // Create review if rating provided
                if (rating.HasValue && rating.Value >= 1 && rating.Value <= 5)
                {
                    var review = new Review
                    {
                        BookingId = bookingId,
                        ServiceId = booking.ServiceId,
                        UserId = userId,
                        ProviderId = booking.ProviderId,
                        Rating = rating.Value,
                        Comment = comment?.Trim()
                    };

                    var reviewCreated = await _reviewRepository.CreateReviewAsync(review);
                    
                    if (reviewCreated)
                    {
                        // Audit Log: Review Created
                        await _auditService.LogAsync(
                            userId,
                            user?.FullName,
                            user?.Email,
                            "ReviewCreated",
                            "Review",
                            "Review",
                            review.ReviewId.ToString(),
                            $"User submitted {rating.Value}-star review for service '{service?.ServiceName ?? "Service"}'. Comment: {(string.IsNullOrEmpty(comment) ? "None" : comment.Substring(0, Math.Min(100, comment.Length)))}"
                        );
                    }
                }

                // Notify provider
                var reviewText = rating.HasValue ? $" with a {rating}-star rating" : "";
                await _notificationRepository.CreateNotificationAsync(
                    booking.ProviderId,
                    "Job Completed",
                    $"{user?.FullName ?? "User"} has confirmed the job completion{reviewText}. Thank you for your service!",
                    null
                );

                // Audit Log: Completion Confirmed
                await _auditService.LogAsync(
                    userId,
                    user?.FullName,
                    user?.Email,
                    "CompletionConfirmed",
                    "Booking",
                    "Booking",
                    booking.BookingId.ToString(),
                    $"User confirmed job completion for service '{service?.ServiceName ?? "Service"}'{reviewText}."
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
        /// Provider accepts booking and sets price with confirmed time (AJAX)
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

                // Allow accept for PendingProviderReview or rescheduling proposal for NeedRescheduling
                if (booking.Status != BookingStatus.PendingProviderReview && 
                    booking.Status != BookingStatus.NeedRescheduling)
                {
                    return BadRequest(new { message = "This booking has already been processed." });
                }

                if (dto.NegotiatedPrice <= 0)
                {
                    return BadRequest(new { message = "Please enter a valid price." });
                }

                // Combine confirmed date and time
                var confirmedStartDateTime = _schedulingService.CombineDateAndTime(dto.ConfirmedDate, dto.ConfirmedStartTime);
                var confirmedEndDateTime = _schedulingService.CombineDateAndTime(dto.ConfirmedDate, dto.ConfirmedEndTime);

                // Validate confirmed time
                if (confirmedEndDateTime <= confirmedStartDateTime)
                {
                    return BadRequest(new { message = "End time must be after start time." });
                }

                // Check for overlapping accepted bookings
                var availabilityCheck = await _schedulingService.CheckProviderAvailabilityAsync(
                    providerId, confirmedStartDateTime, confirmedEndDateTime, dto.BookingId);
                
                if (!availabilityCheck.IsAvailable)
                {
                    return BadRequest(new { message = availabilityCheck.Message });
                }

                // Validate duty hours
                var dutyCheck = await _schedulingService.IsWithinProviderDutyHoursAsync(
                    providerId, dto.ConfirmedStartTime, dto.ConfirmedEndTime);
                
                if (!dutyCheck.IsWithinHours)
                {
                    var dutyMsg = dutyCheck.DutyStart.HasValue && dutyCheck.DutyEnd.HasValue
                        ? $"Time must be within your working hours ({DateTime.Today.Add(dutyCheck.DutyStart.Value):h:mm tt} - {DateTime.Today.Add(dutyCheck.DutyEnd.Value):h:mm tt})."
                        : "Time is outside your working hours.";
                    return BadRequest(new { message = dutyMsg });
                }

                // Accept booking with confirmed time
                var result = await _bookingRepository.AcceptBookingAsync(
                    dto.BookingId, dto.NegotiatedPrice, dto.ProviderNotes,
                    confirmedStartDateTime, confirmedEndDateTime);
                    
                if (!result)
                {
                    return BadRequest(new { message = "Failed to accept booking." });
                }

                // Create provider time slot to lock availability
                var timeSlot = new ProviderTimeSlot
                {
                    ProviderId = providerId,
                    BookingId = dto.BookingId,
                    StartDateTime = confirmedStartDateTime,
                    EndDateTime = confirmedEndDateTime,
                    IsActive = true
                };
                await _timeSlotRepository.CreateAsync(timeSlot);

                // Process overlapping pending requests (move them to NeedRescheduling)
                var overlapsProcessed = await _schedulingService.ProcessOverlappingRequestsAsync(
                    providerId, confirmedStartDateTime, confirmedEndDateTime, dto.BookingId);

                // Notify user with time confirmation
                var provider = await _userManager.FindByIdAsync(providerId);
                var service = await _serviceRepository.GetServiceByIdAsync(booking.ServiceId);
                var startTimeStr = confirmedStartDateTime.ToString("h:mm tt");
                var endTimeStr = confirmedEndDateTime.ToString("h:mm tt");
                
                await _notificationRepository.CreateNotificationAsync(
                    booking.UserId,
                    "Booking Accepted!",
                    $"{provider?.FullName ?? "Provider"} has accepted your booking for '{service?.ServiceName}' on {dto.ConfirmedDate:MMM dd, yyyy} from {startTimeStr} to {endTimeStr}. Price: Tk {dto.NegotiatedPrice:N0}. Proceed to payment.",
                    null
                );

                // Audit Log: Booking Accepted
                await _auditService.LogAsync(
                    providerId,
                    provider?.FullName,
                    provider?.Email,
                    "BookingAccepted",
                    "Booking",
                    "Booking",
                    booking.BookingId.ToString(),
                    $"Provider accepted booking with price Tk {dto.NegotiatedPrice}, scheduled {dto.ConfirmedDate:yyyy-MM-dd} {startTimeStr}-{endTimeStr}. {overlapsProcessed} overlapping requests moved to NeedRescheduling."
                );

                var successMsg = "Booking accepted! The user has been notified and can now proceed with payment.";
                if (overlapsProcessed > 0)
                {
                    successMsg += $" {overlapsProcessed} overlapping request(s) have been moved to 'Need Rescheduling'.";
                }

                return Json(new { success = true, message = successMsg });
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

                // Allow cancellation for pending, accepted, or need rescheduling bookings
                if (booking.Status != BookingStatus.PendingProviderReview &&
                    booking.Status != BookingStatus.AcceptedByProvider &&
                    booking.Status != BookingStatus.NeedRescheduling)
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

                // Audit Log: Booking Declined/Cancelled by Provider
                await _auditService.LogAsync(
                    providerId,
                    provider?.FullName,
                    provider?.Email,
                    "BookingDeclined",
                    "Booking",
                    "Booking",
                    booking.BookingId.ToString(),
                    $"Provider declined booking. Reason: {dto.Reason ?? "None provided"}"
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
        /// Provider adjusts booking time (AJAX)
        /// </summary>
        [Authorize(Roles = RoleNames.ServiceProvider)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdjustBookingTime(AdjustBookingTimeDto dto)
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

                // Only allow adjustment for accepted or in-progress bookings
                if (booking.Status != BookingStatus.AcceptedByProvider &&
                    booking.Status != BookingStatus.AwaitingPayment &&
                    booking.Status != BookingStatus.PaymentReceived &&
                    booking.Status != BookingStatus.InProgress)
                {
                    return BadRequest(new { message = "Cannot adjust time for this booking status." });
                }

                // Validate new time
                var newStartDateTime = _schedulingService.CombineDateAndTime(dto.NewDate, dto.NewStartTime);
                var newEndDateTime = _schedulingService.CombineDateAndTime(dto.NewDate, dto.NewEndTime);

                if (newEndDateTime <= newStartDateTime)
                {
                    return BadRequest(new { message = "End time must be after start time." });
                }

                // Check for overlapping bookings
                var availabilityCheck = await _schedulingService.CheckProviderAvailabilityAsync(
                    providerId, newStartDateTime, newEndDateTime, dto.BookingId);
                    
                if (!availabilityCheck.IsAvailable)
                {
                    return BadRequest(new { message = availabilityCheck.Message });
                }

                // Update booking time
                var result = await _bookingRepository.UpdateBookingTimeAsync(
                    dto.BookingId, newStartDateTime, newEndDateTime);
                    
                if (!result)
                {
                    return BadRequest(new { message = "Failed to update booking time." });
                }

                // Notify user about the time change
                var provider = await _userManager.FindByIdAsync(providerId);
                await _notificationRepository.CreateNotificationAsync(
                    booking.UserId,
                    "Booking Time Adjusted",
                    $"{provider?.FullName ?? "The provider"} has adjusted your booking time to {newStartDateTime:MMM dd, yyyy} at {dto.NewStartTime:hh\\:mm} - {dto.NewEndTime:hh\\:mm}." +
                    (!string.IsNullOrEmpty(dto.Reason) ? $" Reason: {dto.Reason}" : ""),
                    null
                );

                return Json(new { success = true, message = "Booking time updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adjusting booking time");
                return StatusCode(500, new { message = "Failed to adjust booking time." });
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

                // Audit Log: Job Marked Done
                await _auditService.LogAsync(
                    providerId,
                    provider?.FullName,
                    provider?.Email,
                    "JobMarkedDone",
                    "Booking",
                    "Booking",
                    booking.BookingId.ToString(),
                    "Provider marked job as completed, awaiting user confirmation."
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

                // Time schedule fields
                RequestedDate = booking.RequestedDate,
                RequestedStartTime = booking.RequestedStartTime,
                RequestedEndTime = booking.RequestedEndTime,
                ConfirmedStartDateTime = booking.ConfirmedStartDateTime,
                ConfirmedEndDateTime = booking.ConfirmedEndDateTime,

                Status = booking.Status,
                StatusDisplay = GetStatusDisplayText(booking.Status),
                StatusBadgeClass = GetStatusBadgeClass(booking.Status),

                CreatedAt = booking.CreatedAt,
                AcceptedAt = booking.AcceptedAt,
                CanPay = booking.Status == BookingStatus.JobDone,
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

                // Time schedule fields
                RequestedDate = booking.RequestedDate,
                RequestedStartTime = booking.RequestedStartTime,
                RequestedEndTime = booking.RequestedEndTime,
                ConfirmedStartDateTime = booking.ConfirmedStartDateTime,
                ConfirmedEndDateTime = booking.ConfirmedEndDateTime,

                Status = booking.Status,
                StatusDisplay = GetStatusDisplayText(booking.Status),
                StatusBadgeClass = GetStatusBadgeClass(booking.Status),

                CreatedAt = booking.CreatedAt,
                AcceptedAt = booking.AcceptedAt,

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
                BookingStatus.AcceptedByProvider => "Accepted - Awaiting Service",
                BookingStatus.AwaitingPayment => "Awaiting Payment",
                BookingStatus.PaymentReceived => "Payment Received",
                BookingStatus.InProgress => "In Progress",
                BookingStatus.JobDone => "Job Done - Awaiting Payment & Confirmation",
                BookingStatus.Completed => "Completed",
                BookingStatus.Cancelled => "Cancelled",
                BookingStatus.Disputed => "Disputed",
                BookingStatus.NeedRescheduling => "Needs Rescheduling",
                BookingStatus.AutoCancelled => "Auto-Cancelled",
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
                BookingStatus.NeedRescheduling => "badge-warning",
                BookingStatus.AutoCancelled => "badge-secondary",
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
