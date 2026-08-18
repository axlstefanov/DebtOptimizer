using System.Text.Json;
using DebtOptimizer.Dtos;

namespace DebtOptimizer.Services
{
    public class GeminiDebtExtractor(HttpClient http, IConfiguration configuration) : IDebtExtractor
    {
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
            - Dates the user writes are European day-first format, so 01.02.2027 and 01/02/2027
              are both 1 February 2027, not 2 January 2027.
            - income and expenses are monthly amounts.
            - Return an empty debts array if no debt is mentioned.
            """;

        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public async Task<ExtractionResult> ExtractAsync(string userText)
        {
            var modelJson = await GeminiClient.GenerateJsonAsync(http, configuration, SystemPrompt, userText);
            var result = Deserialize(modelJson);

            result.Debts ??= [];

            return result;
        }

        private static ExtractionResult Deserialize(string modelJson)
        {
            try
            {
                return JsonSerializer.Deserialize<ExtractionResult>(modelJson, SerializerOptions)
                    ?? throw new InvalidOperationException(
                        $"Gemini returned no extraction result: {GeminiClient.Excerpt(modelJson)}");
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException(
                    $"Gemini returned JSON that is not an extraction result: {GeminiClient.Excerpt(modelJson)}", ex);
            }
        }
    }
}
