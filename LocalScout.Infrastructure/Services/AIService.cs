using LocalScout.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace LocalScout.Infrastructure.Services
{
    public class AIService : IAIService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiToken;
        private readonly string _model;
        private readonly ILogger<AIService> _logger;

        private const int TimeoutSeconds = 30;
        private const int MaxRetries = 3;

        public AIService(HttpClient httpClient, IConfiguration configuration, ILogger<AIService> logger)
        {
            _httpClient = httpClient;
            _httpClient.Timeout = TimeSpan.FromSeconds(TimeoutSeconds);

            _apiToken = configuration["HuggingFace:ApiToken"] ?? "";
            _model = configuration["HuggingFace:Model"] ?? "microsoft/Phi-3-mini-4k-instruct";

            _logger = logger;
        }

        public async Task<string> GenerateDescriptionAsync(Dictionary<string, string> context, string type)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_apiToken))
                {
                    _logger.LogError("Hugging Face API Token is missing.");
                    return "Description unavailable (Config Error).";
                }

                var prompt = BuildPrompt(context, type);
                var response = await CallHuggingFaceRouterWithRetryAsync(prompt);

                return CleanResponse(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI Generation Failed");
                return "Description unavailable at this time.";
            }
        }

        private string BuildPrompt(Dictionary<string, string> context, string type)
        {
            // Format context data into a clean list
            var contextText = string.Join("\n", context
                .Where(kvp => !string.IsNullOrWhiteSpace(kvp.Value))
                .Select(kvp => $"- {kvp.Key}: {kvp.Value}"));

            if (type.Equals("provider", StringComparison.OrdinalIgnoreCase))
            {
                return $@"
                    Role: You are the business owner writing a professional biography.
                    Task: Write a sophisticated business bio based on the data below.
                    Constraints: 
                    - Max 60 words.
                    - Write in the FIRST PERSON PLURAL (Use 'We', 'Our', 'Us').
                    - Do NOT use the owner's name as the starting subject. Instead of 'John is...', write 'We are...'.
                    - Start directly with 'We are', 'We provide', or 'Our academy'.
                    - Output ONLY the biography text.

                    Data:
                    {contextText}";
            }

            return $@"
                    Role: You are the service provider.
                    Task: Write a compelling description for your service based on the data below.
                    Constraints:
                    - Max 50 words.
                    - Write in the FIRST PERSON (Use 'We offer', 'Our service').
                    - Focus on benefits and value to the customer.
                    - Output ONLY the description text.

                    Data:
                {contextText}";
        }

        private async Task<string> CallHuggingFaceRouterWithRetryAsync(string prompt)
        {
            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    return await CallHuggingFaceRouterApiAsync(prompt);
                }
                catch (HttpRequestException ex) when (attempt < MaxRetries)
                {
                    _logger.LogWarning("Attempt {Attempt} failed. Retrying... Error: {Error}", attempt, ex.Message);
                    await Task.Delay(2000 * attempt);
                }
            }
            throw new Exception("Failed to generate description after multiple attempts.");
        }

        private async Task<string> CallHuggingFaceRouterApiAsync(string prompt)
        {
            var url = "https://router.huggingface.co/v1/chat/completions";

            var requestBody = new
            {
                model = _model,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                max_tokens = 100,      // Slightly higher to ensure sentence finishes
                temperature = 0.6      // Lower temperature = Less creative/random, more focused
            };

            var json = JsonSerializer.Serialize(requestBody);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var request = new HttpRequestMessage(HttpMethod.Post, url);

            request.Content = content;
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiToken);

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                if (responseContent.Contains("loading", StringComparison.OrdinalIgnoreCase))
                {
                    throw new HttpRequestException("Model is loading");
                }
                throw new Exception($"API Error: {response.StatusCode} - {responseContent}");
            }

            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var chatResponse = JsonSerializer.Deserialize<ChatCompletionResponse>(responseContent, options);
                return chatResponse?.Choices?.FirstOrDefault()?.Message?.Content ?? "";
            }
            catch (JsonException ex)
            {
                _logger.LogError("JSON Parse Error: {Error}", ex.Message);
                return "";
            }
        }

        private string CleanResponse(string response)
        {
            if (string.IsNullOrWhiteSpace(response)) return "";

            response = response.Trim().Trim('"', '\'');

            // Aggressive cleaning of common AI chat prefixes
            var artifacts = new[] {
                "Here is a concise", "Here is a professional", "Here is the", "Sure!", "Certainly!",
                "Bio:", "Description:", "Output:", "Summary:"
            };

            foreach (var artifact in artifacts)
            {
                if (response.StartsWith(artifact, StringComparison.OrdinalIgnoreCase))
                {
                    // If it starts with "Bio:", remove it.
                    // If it is a sentence like "Here is a bio for...", try to find the next newline or colon
                    var index = response.IndexOf(':');
                    if (index > 0 && index < 25)
                    {
                        response = response.Substring(index + 1).Trim();
                    }
                    else
                    {
                        // Fallback: just remove the artifact string
                        response = response.Replace(artifact, "", StringComparison.OrdinalIgnoreCase).Trim();
                    }
                }
            }

            return response;
        }

        // --- OpenAI Response Classes ---
        private class ChatCompletionResponse
        {
            public List<ChatChoice>? Choices { get; set; }
        }
        private class ChatChoice
        {
            public ChatMessage? Message { get; set; }
        }
        private class ChatMessage
        {
            public string? Content { get; set; }
        }
    }
}