using LocalScout.Application.DTOs.ReportDTOs;

namespace LocalScout.Application.Interfaces
{
    /// <summary>
    /// Service for generating PDF reports for different user roles
    /// </summary>
    public interface IReportPdfService
    {
        /// <summary>
        /// Generate PDF report for user's activity
        /// </summary>
        byte[] GenerateUserReport(UserReportDto report, string userName);

        /// <summary>
        /// Generate PDF report for service provider's earnings and bookings
        /// </summary>
        byte[] GenerateProviderReport(ProviderReportDto report, string providerName);

        /// <summary>
        /// Generate PDF report for admin system statistics
        /// </summary>
        byte[] GenerateAdminReport(AdminReportDto report);
    }
}
