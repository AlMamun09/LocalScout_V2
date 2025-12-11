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
            var contextText = string.Join("\n", context
                .Where(kvp => !string.IsNullOrWhiteSpace(kvp.Value))
                .Select(kvp => $"- {kvp.Key}: {kvp.Value}"));

            if (type.Equals("provider", StringComparison.OrdinalIgnoreCase))
            {
                return $@"
                    Role: You are the business owner.
                    Task: Write a professional business biography using the data provided.
                    Constraints:
                    - Length: minimum 30 words, maximum 60 words.
                    - Voice: First person plural (We, Our).
                    - Opening: Must begin with one of the following:
                      * We are
                      * We specialize in
                      * We provide
                      * We offer
                    - If Working Days or Working Hours are provided, naturally mention availability (e.g., 'available Monday-Friday from 9:00 AM to 5:00 PM').
                    - Formatting: No Markdown, no code blocks, no commentary.
                    - Output: Only the final biography text.
                Data:
                {contextText}";
            }

            return $@"
                Role: You are the service provider.
                Task: Write a compelling service description using the data provided.
                Constraints:
                - Length: minimum 20 words, maximum 50 words.
                - Voice: First person singular (I, My).
                - Opening: Must begin with one of the following:
                  * I offer
                  * I provide
                  * I deliver
                  * I help clients by
                - Formatting: No Markdown, no code blocks, no commentary.
                - Output: Only the final service description.

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

            // 1. Remove Markdown Code Blocks (The main issue)
            response = response.Replace("```python", "")
                               .Replace("```json", "")
                               .Replace("```", "");

            // 2. Remove "Note:" sections often added at the end
            var noteIndex = response.IndexOf("Note:", StringComparison.OrdinalIgnoreCase);
            if (noteIndex > -1)
            {
                response = response.Substring(0, noteIndex);
            }

            // 3. Remove Quotes and Whitespace
            response = response.Trim().Trim('"', '\'');

            // 4. Remove chatty prefixes
            var artifacts = new[] { "Here is a", "Sure!", "Bio:", "Description:", "Certainly!", "Output:" };
            foreach (var artifact in artifacts)
            {
                if (response.StartsWith(artifact, StringComparison.OrdinalIgnoreCase))
                {
                    // If it says "Here is the bio:", cut everything before the colon
                    var colonIndex = response.IndexOf(':');
                    if (colonIndex > -1 && colonIndex < 50)
                    {
                        response = response.Substring(colonIndex + 1);
                    }
                    else
                    {
                        response = response.Substring(artifact.Length);
                    }
                }
            }

            return response.Trim();
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