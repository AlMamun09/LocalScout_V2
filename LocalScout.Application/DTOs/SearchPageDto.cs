using System.Linq;
using LocalScout.Domain.Entities;

namespace LocalScout.Application.DTOs
{
    public class SearchPageDto
    {
        public IEnumerable<ServiceCategory> Categories { get; set; } = Enumerable.Empty<ServiceCategory>();
        public string? Query { get; set; }
        public Guid? SelectedCategoryId { get; set; }
        public string? SelectedCategoryName { get; set; }
    }
}
