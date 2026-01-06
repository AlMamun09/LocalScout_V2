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
                    Role: You are an expert professional writer creating a business biography for a service provider.
                    Task: Write a compelling, trustworthy, and professional business biography using the data provided.
                    Constraints:
                    - Length: Minimum 30 words. Maximum 70 Words. Content should be concise but impactful
                    - Voice: Professional, confident, and welcoming (First person singular 'I' or 'My' based on context).
                    - Style: Clear and direct. Do NOT use flowery or esoteric language (e.g., do not use phrases like 'like Diamond' or 'shining star').
                    - Key Elements to Include:
                      * Years of experience (if mentioned or implied).
                      * Commitment to quality and customer satisfaction.
                      * Specific expertise areas.
                    - Tone: Reliable, skilled, and customer-focused.
                    - Formatting: SINGLE PARAGRAPH ONLY. No empty lines. No Markdown. NO asterisks (*), NO bolding (**), no bullet points. Just plain text.
                Data:
                {contextText}";
            }

            return $@"
                Role: You are an expert marketing copywriter.
                Task: Write a persuasive and attractive service description that converts viewers into customers.
                Constraints:
                - Length: Minimum 40 words. Maximum 90 words.
                - Voice: First person singular (I, My).
                - Price Phrasing: If price is mentioned, ALWAYS phrase it as 'Starting from [Amount] Taka' (e.g., 'Starting from 600 Taka').
                - Mandatory Disclaimer: You MUST explicitly include a note stating: 'Price can vary based on complexity, time, and requirements.'
                - Formatting: SINGLE PARAGRAPH ONLY. Do not use bullet points or lists. NO Markdown formatting. NO asterisks (*), NO bolding (**).
                - Style: Professional, warm, and competent. Avoid over-the-top metaphors.
                - Structure:
                  Start with a strong hook, follow with the key benefits/offers, mention the price/disclaimer, and end with a subtle call to action. All in one cohesive paragraph.
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