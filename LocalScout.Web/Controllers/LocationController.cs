using LocalScout.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LocalScout.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LocationController : ControllerBase
    {
        private readonly ILocationService _locationService;

        public LocationController(ILocationService locationService)
        {
            _locationService = locationService;
        }

        [HttpPost("reverse-geocode")]
        public async Task<IActionResult> ReverseGeocode([FromBody] ReverseGeocodeRequest request)
        {
            if (
                request.Latitude < -90
                || request.Latitude > 90
                || request.Longitude < -180
                || request.Longitude > 180
            )
            {
                return BadRequest(new { error = "Invalid coordinates" });
            }

            var result = await _locationService.ReverseGeocodeAsync(
                request.Latitude,
                request.Longitude
            );

            if (result == null)
            {
                return NotFound(new { error = "Unable to find address for given coordinates" });
            }

            return Ok(result);
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchAddress([FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest(new { error = "Query is required" });
            }

            var results = await _locationService.SearchAddressAsync(query);
            return Ok(results);
        }
    }

    public class ReverseGeocodeRequest
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
