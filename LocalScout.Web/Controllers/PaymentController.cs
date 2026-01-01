using LocalScout.Application.DTOs.PaymentDTOs;
using LocalScout.Application.Interfaces;
using LocalScout.Domain.Entities;
using LocalScout.Infrastructure.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LocalScout.Web.Controllers
{
    /// <summary>
    /// Handles SSLCommerz payment gateway callbacks and payment initiation
    /// </summary>
    public class PaymentController : Controller
    {
        private readonly ISSLCommerzService _sslCommerzService;
        private readonly IBookingRepository _bookingRepository;
        private readonly IServiceRepository _serviceRepository;
        private readonly INotificationRepository _notificationRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(
            ISSLCommerzService sslCommerzService,
            IBookingRepository bookingRepository,
            IServiceRepository serviceRepository,
            INotificationRepository notificationRepository,
            UserManager<ApplicationUser> userManager,
            ILogger<PaymentController> logger)
        {
            _sslCommerzService = sslCommerzService;
            _bookingRepository = bookingRepository;
            _serviceRepository = serviceRepository;
            _notificationRepository = notificationRepository;
            _userManager = userManager;
            _logger = logger;
        }

        /// <summary>
        /// Initiates payment for a booking - redirects to SSLCommerz
        /// </summary>
        [Authorize(Roles = RoleNames.User)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Initiate(Guid bookingId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login", "Account", new { area = "Identity" });
                }

                var booking = await _bookingRepository.GetByIdAsync(bookingId);
                if (booking == null || booking.UserId != user.Id)
                {
                    TempData["Error"] = "Booking not found.";
                    return RedirectToAction("MyBookings", "Booking");
                }

                if (booking.Status != Domain.Enums.BookingStatus.AcceptedByProvider &&
                    booking.Status != Domain.Enums.BookingStatus.AwaitingPayment)
                {
                    TempData["Error"] = "Payment is not available for this booking.";
                    return RedirectToAction("MyBookings", "Booking");
                }

                if (!booking.NegotiatedPrice.HasValue || booking.NegotiatedPrice <= 0)
                {
                    TempData["Error"] = "Invalid booking price.";
                    return RedirectToAction("MyBookings", "Booking");
                }

                var service = await _serviceRepository.GetServiceByIdAsync(booking.ServiceId);
                if (service == null)
                {
                    TempData["Error"] = "Service not found.";
                    return RedirectToAction("MyBookings", "Booking");
                }

                // Generate unique transaction ID
                var transactionId = _sslCommerzService.GenerateTransactionId(bookingId);
                
                // Store transaction ID in booking for later validation
                await _bookingRepository.SetTransactionIdAsync(bookingId, transactionId);

                // Build callback URLs
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                
                var paymentDto = new PaymentInitiateDto
                {
                    BookingId = bookingId,
                    TransactionId = transactionId,
                    Amount = booking.NegotiatedPrice.Value,
                    CustomerName = user.FullName ?? "Customer",
                    CustomerEmail = user.Email ?? "customer@example.com",
                    CustomerPhone = user.PhoneNumber ?? "01700000000",
                    CustomerAddress = user.Address ?? "Dhaka",
                    ProductName = service.ServiceName ?? "Service Booking",
                    SuccessUrl = $"{baseUrl}/Payment/Success",
                    FailUrl = $"{baseUrl}/Payment/Fail",
                    CancelUrl = $"{baseUrl}/Payment/Cancel",
                    IpnUrl = $"{baseUrl}/Payment/IPN"
                };

                var response = await _sslCommerzService.InitiatePaymentAsync(paymentDto);

                if (response.Success && !string.IsNullOrEmpty(response.GatewayPageUrl))
                {
                    _logger.LogInformation("SSLCommerz session created for booking {BookingId}, redirecting to gateway", bookingId);
                    return Redirect(response.GatewayPageUrl);
                }
                else
                {
                    _logger.LogError("SSLCommerz session failed: {Reason}", response.FailedReason);
                    TempData["Error"] = $"Payment initialization failed: {response.FailedReason}";
                    return RedirectToAction("MyBookings", "Booking");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating payment for booking {BookingId}", bookingId);
                TempData["Error"] = "Failed to initiate payment. Please try again.";
                return RedirectToAction("MyBookings", "Booking");
            }
        }

        /// <summary>
        /// SSLCommerz success callback - validates and confirms payment
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Success([FromForm] SSLCommerzCallbackDto callback)
        {
            _logger.LogInformation("SSLCommerz Success callback received. TranId: {TranId}, Status: {Status}", 
                callback.tran_id, callback.status);

            try
            {
                // Validate the transaction using SSLCommerz API
                if (string.IsNullOrEmpty(callback.val_id))
                {
                    return await HandlePaymentResult(callback.tran_id, false, "Invalid validation ID");
                }

                var validationResult = await _sslCommerzService.ValidateTransactionAsync(callback.val_id);
                
                if (validationResult == null || !validationResult.IsValid)
                {
                    _logger.LogWarning("SSLCommerz validation failed for TranId: {TranId}", callback.tran_id);
                    return await HandlePaymentResult(callback.tran_id, false, "Payment validation failed");
                }

                // Find booking by transaction ID
                var booking = await _bookingRepository.GetByTransactionIdAsync(callback.tran_id ?? "");
                if (booking == null)
                {
                    _logger.LogError("Booking not found for TranId: {TranId}", callback.tran_id);
                    return await HandlePaymentResult(callback.tran_id, false, "Booking not found");
                }

                // Verify amount matches
                if (decimal.TryParse(validationResult.amount, out var paidAmount))
                {
                    if (paidAmount != booking.NegotiatedPrice)
                    {
                        _logger.LogWarning("Amount mismatch for TranId: {TranId}. Expected: {Expected}, Got: {Got}", 
                            callback.tran_id, booking.NegotiatedPrice, paidAmount);
                    }
                }

                // Mark payment as received
                var result = await _bookingRepository.MarkPaymentReceivedAsync(
                    booking.BookingId,
                    callback.tran_id ?? "",
                    callback.val_id ?? "",
                    callback.card_type ?? "Unknown",
                    callback.bank_tran_id
                );

                if (result)
                {
                    // Notify provider
                    var user = await _userManager.FindByIdAsync(booking.UserId);
                    await _notificationRepository.CreateNotificationAsync(
                        booking.ProviderId,
                        "Payment Received",
                        $"{user?.FullName ?? "User"} has completed payment via {callback.card_type ?? "online payment"}. You can now proceed with the job.",
                        null
                    );

                    return await HandlePaymentResult(callback.tran_id, true, "Payment successful!", booking.BookingId);
                }
                else
                {
                    return await HandlePaymentResult(callback.tran_id, false, "Failed to update booking status");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing SSLCommerz success callback");
                return await HandlePaymentResult(callback.tran_id, false, "An error occurred while processing payment");
            }
        }

        /// <summary>
        /// SSLCommerz failed callback
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Fail([FromForm] SSLCommerzCallbackDto callback)
        {
            _logger.LogWarning("SSLCommerz Fail callback received. TranId: {TranId}, Error: {Error}", 
                callback.tran_id, callback.error);

            return await HandlePaymentResult(callback.tran_id, false, callback.error ?? "Payment failed");
        }

        /// <summary>
        /// SSLCommerz cancel callback
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Cancel([FromForm] SSLCommerzCallbackDto callback)
        {
            _logger.LogInformation("SSLCommerz Cancel callback received. TranId: {TranId}", callback.tran_id);

            return await HandlePaymentResult(callback.tran_id, false, "Payment was cancelled", isCancelled: true);
        }

        /// <summary>
        /// SSLCommerz IPN (Instant Payment Notification) - server-to-server notification
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> IPN([FromForm] SSLCommerzCallbackDto callback)
        {
            _logger.LogInformation("SSLCommerz IPN received. TranId: {TranId}, Status: {Status}", 
                callback.tran_id, callback.status);

            try
            {
                if (callback.status != "VALID" && callback.status != "VALIDATED")
                {
                    _logger.LogWarning("IPN status not valid: {Status}", callback.status);
                    return Ok();
                }

                // Validate the transaction
                if (string.IsNullOrEmpty(callback.val_id))
                {
                    return Ok();
                }

                var validationResult = await _sslCommerzService.ValidateTransactionAsync(callback.val_id);
                if (validationResult == null || !validationResult.IsValid)
                {
                    return Ok();
                }

                // Find booking
                var booking = await _bookingRepository.GetByTransactionIdAsync(callback.tran_id ?? "");
                if (booking == null || booking.Status == Domain.Enums.BookingStatus.PaymentReceived)
                {
                    // Already processed or not found
                    return Ok();
                }

                // Mark payment as received (backup in case success callback failed)
                await _bookingRepository.MarkPaymentReceivedAsync(
                    booking.BookingId,
                    callback.tran_id ?? "",
                    callback.val_id ?? "",
                    callback.card_type ?? "Unknown",
                    callback.bank_tran_id
                );

                _logger.LogInformation("IPN processed successfully for TranId: {TranId}", callback.tran_id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing IPN");
            }

            return Ok();
        }

        /// <summary>
        /// Payment result display page
        /// </summary>
        [AllowAnonymous]
        public async Task<IActionResult> Result(string? tranId, bool success, string? message, Guid? bookingId, bool cancelled = false)
        {
            var model = new PaymentResultDto
            {
                TransactionId = tranId,
                Status = cancelled ? "Cancelled" : (success ? "Success" : "Failed"),
                Message = message
            };

            if (bookingId.HasValue)
            {
                var booking = await _bookingRepository.GetByIdAsync(bookingId.Value);
                if (booking != null)
                {
                    model.BookingId = booking.BookingId;
                    model.Amount = booking.NegotiatedPrice ?? 0;
                    model.PaymentMethod = booking.PaymentMethod;
                    model.TransactionDate = booking.PaymentReceivedAt;

                    var service = await _serviceRepository.GetServiceByIdAsync(booking.ServiceId);
                    model.ServiceName = service?.ServiceName;

                    var provider = await _userManager.FindByIdAsync(booking.ProviderId);
                    model.ProviderName = provider?.FullName;
                }
            }
            else if (!string.IsNullOrEmpty(tranId))
            {
                var booking = await _bookingRepository.GetByTransactionIdAsync(tranId);
                if (booking != null)
                {
                    model.BookingId = booking.BookingId;
                    model.Amount = booking.NegotiatedPrice ?? 0;
                }
            }

            if (cancelled)
            {
                return View("Cancelled", model);
            }
            
            return View(success ? "Success" : "Failed", model);
        }

        private async Task<IActionResult> HandlePaymentResult(string? tranId, bool success, string message, Guid? bookingId = null, bool isCancelled = false)
        {
            // For POST callbacks, we need to redirect to a GET endpoint
            return RedirectToAction("Result", new 
            { 
                tranId = tranId, 
                success = success, 
                message = message, 
                bookingId = bookingId,
                cancelled = isCancelled
            });
        }

        /// <summary>
        /// Download payment receipt as PDF
        /// </summary>
        [Authorize]
        public async Task<IActionResult> DownloadReceipt(Guid bookingId)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return RedirectToAction("Login", "Account", new { area = "Identity" });
                }

                var booking = await _bookingRepository.GetByIdAsync(bookingId);
                if (booking == null)
                {
                    TempData["Error"] = "Booking not found.";
                    return RedirectToAction("MyBookings", "Booking");
                }

                // User can only download their own receipts
                if (booking.UserId != user.Id)
                {
                    TempData["Error"] = "Unauthorized access.";
                    return RedirectToAction("MyBookings", "Booking");
                }

                // Only allow download if payment was received
                if (booking.Status < Domain.Enums.BookingStatus.PaymentReceived)
                {
                    TempData["Error"] = "Receipt is only available after payment.";
                    return RedirectToAction("MyBookings", "Booking");
                }

                var service = await _serviceRepository.GetServiceByIdAsync(booking.ServiceId);
                var provider = await _userManager.FindByIdAsync(booking.ProviderId);

                var receiptDto = new PaymentReceiptDto
                {
                    ReceiptNumber = $"LS-{booking.BookingId.ToString("N").Substring(0, 8).ToUpper()}",
                    BookingId = booking.BookingId,
                    TransactionId = booking.TransactionId ?? "N/A",
                    ValidationId = booking.ValidationId,
                    BankTransactionId = booking.BankTransactionId,
                    Amount = booking.NegotiatedPrice ?? 0,
                    PaymentMethod = booking.PaymentMethod,
                    PaymentDate = booking.PaymentReceivedAt ?? DateTime.UtcNow,
                    ServiceName = service?.ServiceName ?? "Service",
                    ProviderName = provider?.FullName ?? "Provider",
                    ProviderPhone = provider?.PhoneNumber,
                    ProviderEmail = provider?.Email,
                    CustomerName = user.FullName ?? "Customer",
                    CustomerEmail = user.Email,
                    CustomerPhone = user.PhoneNumber
                };

                var pdfService = new LocalScout.Infrastructure.Services.ReceiptPdfService();
                var pdfBytes = pdfService.GenerateReceipt(receiptDto);

                var fileName = $"Receipt_{receiptDto.ReceiptNumber}_{DateTime.Now:yyyyMMdd}.pdf";
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating receipt for booking {BookingId}", bookingId);
                TempData["Error"] = "Failed to generate receipt.";
                return RedirectToAction("MyBookings", "Booking");
            }
        }
    }
}

