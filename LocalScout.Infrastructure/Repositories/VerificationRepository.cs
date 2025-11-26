using LocalScout.Application.DTOs;
using LocalScout.Application.Interfaces;
using LocalScout.Domain.Entities;
using LocalScout.Domain.Enums;
using LocalScout.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace LocalScout.Infrastructure.Repositories
{
    public class VerificationRepository : IVerificationRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public VerificationRepository(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task<string?> ValidateSubmissionAsync(string providerId, IFormFile document)
        {
            // 1. Check if file is empty
            if (document == null || document.Length == 0)
                return "Please upload a valid document.";

            // 2. Check File Extension
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
            var extension = Path.GetExtension(document.FileName)?.ToLower();
            if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
                return "Invalid file type. Only JPG, PNG, and PDF are allowed.";

            // 3. Check File Size (Max 5MB)
            if (document.Length > 5 * 1024 * 1024)
                return "File size exceeds the 5MB limit.";

            // 4. Check for duplicate pending requests
            var hasPending = await _context.VerificationRequests.AnyAsync(v =>
                v.ProviderId == providerId && v.Status == VerificationStatus.Pending
            );

            if (hasPending)
                return "You already have a pending verification request. Please wait for admin review.";

            return null;
        }

        public async Task SubmitRequestAsync(
            string providerId,
            VerificationSubmissionDto dto,
            string webRootPath
        )
        {
            // 1. Create Upload Directory if not exists
            string uploadsFolder = Path.Combine(webRootPath, "uploads", "verification");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            // 2. Generate Unique Filename
            string uniqueFileName = Guid.NewGuid().ToString() + "_" + dto.Document.FileName;
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // 3. Save File to Disk
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await dto.Document.CopyToAsync(fileStream);
            }

            // 4. Save to Database
            var request = new VerificationRequest
            {
                VerificationRequestId = Guid.NewGuid(),
                ProviderId = providerId,
                DocumentType = dto.DocumentType,
                DocumentPath = uniqueFileName, // Store only the filename
                Status = VerificationStatus.Pending,
                SubmittedAt = DateTime.UtcNow,
            };

            _context.VerificationRequests.Add(request);
            await _context.SaveChangesAsync();
        }

        public async Task<List<VerificationRequest>> GetPendingRequestsAsync()
        {
            return await _context
                .VerificationRequests.Where(v => v.Status == VerificationStatus.Pending)
                .OrderByDescending(v => v.SubmittedAt)
                .ToListAsync();
        }

        public async Task<VerificationRequest?> GetRequestByIdAsync(Guid requestId)
        {
            return await _context.VerificationRequests.FindAsync(requestId);
        }

        public async Task<VerificationRequest?> GetLatestRequestByProviderIdAsync(string providerId)
        {
            return await _context
                .VerificationRequests.Where(v => v.ProviderId == providerId)
                .OrderByDescending(v => v.SubmittedAt)
                .FirstOrDefaultAsync();
        }

        public async Task UpdateRequestStatusAsync(
            Guid requestId,
            VerificationStatus status,
            string? adminComments = null
        )
        {
            var request = await _context.VerificationRequests.FindAsync(requestId);
            if (request != null)
            {
                request.Status = status;
                request.ReviewedAt = DateTime.UtcNow;
                request.AdminComments = adminComments;

                _context.VerificationRequests.Update(request);
                await _context.SaveChangesAsync();
            }
        }
    }
}
