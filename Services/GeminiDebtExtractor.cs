using System.Text;
using System.Text.Json;
using DebtOptimizer.Dtos;

namespace DebtOptimizer.Services
{
    public class GeminiDebtExtractor(HttpClient http, IConfiguration configuration) : IDebtExtractor
    {
        private const string Model = "gemini-2.5-flash";

        private const string SystemPrompt = """
            You extract debt and budget information from what a user types about their finances.

            Return only JSON matching this schema, with no prose and no markdown fence:
            {
              "debts": [
                {
                  "name": string or null,
                  "balance": number or null,
                  "annualInterestRatePercent": number or null,
                  "minimumPayment": number or null,
                  "payoffDeadline": "YYYY-MM-DD" or null
                }
              ],
              "income": number or null,
              "expenses": number or null
            }

            Rules:
            - Use null for anything the user did not state. NEVER guess, infer or fill in a
              typical value. null means "not stated"; 0 means the user stated zero.
            - A debt mentioned without any details still gets its own entry with null fields.
            - Expand spoken amounts: "3k" is 3000, "10 grand" is 10000, "1.5k" is 1500.
            - Rates are annual percentages: "5%" is 5, not 0.05. A monthly rate must be
              converted to an annual percentage.
            - income and expenses are monthly amounts.
            - Return an empty debts array if no debt is mentioned.
            """;

        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public async Task<ExtractionResult> ExtractAsync(string userText)
        {
            var apiKey = configuration["Gemini:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException(
                    "Gemini API key is not configured. Set Gemini:ApiKey (Gemini__ApiKey).");
            }

            var payload = new
            {
                system_instruction = new { parts = new[] { new { text = SystemPrompt } } },
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

            var modelJson = ReadModelText(body);
            var result = Deserialize(modelJson);

            result.Debts ??= [];

            return result;
        }

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

        private static ExtractionResult Deserialize(string modelJson)
        {
            try
            {
                return JsonSerializer.Deserialize<ExtractionResult>(modelJson, SerializerOptions)
                    ?? throw new InvalidOperationException(
                        $"Gemini returned no extraction result: {Excerpt(modelJson)}");
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException(
                    $"Gemini returned JSON that is not an extraction result: {Excerpt(modelJson)}", ex);
            }
        }

        private static string Excerpt(string body)
            => body.Length <= 500 ? body : string.Concat(body.AsSpan(0, 500), "...");
    }
}
