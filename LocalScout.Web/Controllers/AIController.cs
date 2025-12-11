using LocalScout.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LocalScout.Web.Controllers
{
    public class AIController : Controller
    {
        private readonly IAIService _aiService;
        private readonly ILogger<AIController> _logger;

        public AIController(IAIService aiService, ILogger<AIController> logger)
        {
            _aiService = aiService;
            _logger = logger;
        }

        [HttpPost]
        [AllowAnonymous]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> GenerateDescription([FromBody] GenerateDescriptionRequest request)
        {
            try
            {
                _logger.LogInformation("AI GenerateDescription called with type: {Type}", request?.Type);

                if (request?.Context == null || !request.Context.Any())
                {
                    _logger.LogWarning("No context data provided");
                    return BadRequest(new { success = false, message = "No context data provided" });
                }

                _logger.LogInformation("Calling AI service with {Count} context items", request.Context.Count);

                var description = await _aiService.GenerateDescriptionAsync(request.Context, request.Type ?? "service");

                _logger.LogInformation("AI generation successful, response length: {Length}", description?.Length ?? 0);

                return Ok(new { success = true, description });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate AI description");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }

    public class GenerateDescriptionRequest
    {
        public Dictionary<string, string> Context { get; set; } = new();
        public string? Type { get; set; }
    }
}
