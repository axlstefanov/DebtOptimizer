using System.Text;
using System.Text.Json;

namespace DebtOptimizer.Services
{
    public static class GeminiClient
    {
        private const string Model = "gemini-2.5-flash";

        public static async Task<string> GenerateJsonAsync(
            HttpClient http, IConfiguration configuration, string systemPrompt, string userText)
        {
            var apiKey = configuration["Gemini:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException(
                    "Gemini API key is not configured. Set Gemini:ApiKey (Gemini__ApiKey).");
            }

            var payload = new
            {
                system_instruction = new { parts = new[] { new { text = systemPrompt } } },
                contents = new[] { new { parts = new[] { new { text = userText } } } },
                generationConfig = new { responseMimeType = "application/json" }
            };

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{Model}:generateContent?key={apiKey}";
            using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            using var response = await http.PostAsync(url, content);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Gemini returned {(int)response.StatusCode} {response.ReasonPhrase}: {Excerpt(body)}");
            }

            return ReadModelText(body);
        }

        public static string Excerpt(string body)
            => body.Length <= 500 ? body : string.Concat(body.AsSpan(0, 500), "...");

        private static string ReadModelText(string body)
        {
            try
            {
                using var envelope = JsonDocument.Parse(body);

                if (!envelope.RootElement.TryGetProperty("candidates", out var candidates)
                    || candidates.ValueKind != JsonValueKind.Array
                    || candidates.GetArrayLength() == 0)
                {
                    throw new InvalidOperationException(
                        $"Gemini response contained no candidates: {Excerpt(body)}");
                }

                var parts = candidates[0].GetProperty("content").GetProperty("parts");

                foreach (var part in parts.EnumerateArray())
                {
                    if (part.TryGetProperty("thought", out var thought) && thought.ValueKind == JsonValueKind.True) continue;
                    if (!part.TryGetProperty("text", out var text)) continue;

                    var value = text.GetString();
                    if (!string.IsNullOrWhiteSpace(value)) return value;
                }

                throw new InvalidOperationException(
                    $"Gemini response contained no text part: {Excerpt(body)}");
            }
            catch (Exception ex) when (ex is JsonException or KeyNotFoundException)
            {
                throw new InvalidOperationException(
                    $"Gemini response was not in the expected shape: {Excerpt(body)}", ex);
            }
        }
    }
}
