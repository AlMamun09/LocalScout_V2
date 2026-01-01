using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalScout.Domain.Entities
{
    public class Review
    {
        [Key]
        public Guid ReviewId { get; set; }
        public Guid BookingId { get; set; }        // One review per booking
        public Guid ServiceId { get; set; }         // For easy service rating lookup
        public string UserId { get; set; }          // Reviewer
        public string ProviderId { get; set; }      // Provider being reviewed
        public int Rating { get; set; }             // 1-5 stars
        public string? Comment { get; set; }        // Optional text review
        public DateTime CreatedAt { get; set; }
        public bool IsDeleted { get; set; }         // Soft delete
    }
}
