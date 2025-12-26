using LocalScout.Domain.Enums;

namespace LocalScout.Application.DTOs.BookingDTOs
{
    /// <summary>
    /// Basic booking DTO for lists and summaries
    /// </summary>
    public class BookingDto
    {
        public Guid BookingId { get; set; }
        public string? CustomerName { get; set; }
        public string? ProviderName { get; set; }
        public string? ServiceName { get; set; }
        public string? CategoryIcon { get; set; }
        public string? Date { get; set; }
        public string? Location { get; set; }
        public string? Status { get; set; }
        public BookingStatus StatusEnum { get; set; }
        public decimal? NegotiatedPrice { get; set; }
    }
}
