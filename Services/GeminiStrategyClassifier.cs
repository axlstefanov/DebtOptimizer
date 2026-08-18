using System.Text.Json;
using System.Text.Json.Serialization;
using DebtOptimizer.Dtos;

namespace DebtOptimizer.Services
{
    public class GeminiStrategyClassifier(HttpClient http, IConfiguration configuration) : IStrategyClassifier
    {
        private const string SystemPrompt = """
            You classify which debt payoff strategy a user wants, from what they type about
            their debts. You classify what they said; you do not advise them.

            Return only JSON matching this schema, with no prose and no markdown fence:
            {
              "strategy": "Avalanche" or "Snowball" or "Target" or null,
              "targetDebtName": string or null,
              "reason": string
            }

            Rules:
            - Read intent, not words. The user will not say "avalanche" or "snowball", and a
              user who does say one of those words still has to mean it.
            - Snowball: they want to feel progress, to clear debts one at a time, to have
              fewer accounts open, to get the small ones out of the way first.
            - Avalanche: they want to pay as little as possible, to waste no money on
              interest, to be efficient, to be out of debt as cheaply as possible.
            - Target: they name one particular debt they want gone. Put that debt in
              targetDebtName, worded the way the user worded it.
            - Use null for strategy when the text does not clearly point at one. NEVER guess.
              Wanting to be debt-free, being worried about money, or listing debts without
              saying how to attack them is not a strategy.
            - targetDebtName is null unless strategy is "Target".
            - "reason" is one short sentence written to the user, saying what in their own
              words led to this answer, so they can tell you if you got it wrong. Write a
              reason even when strategy is null.
            """;

        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public async Task<StrategyInference> ClassifyAsync(string userText)
        {
            var modelJson = await GeminiClient.GenerateJsonAsync(http, configuration, SystemPrompt, userText);

            return Deserialize(modelJson);
        }

        private static StrategyInference Deserialize(string modelJson)
        {
            try
            {
                return JsonSerializer.Deserialize<StrategyInference>(modelJson, SerializerOptions)
                    ?? throw new InvalidOperationException(
                        $"Gemini returned no strategy inference: {GeminiClient.Excerpt(modelJson)}");
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException(
                    $"Gemini returned JSON that is not a strategy inference: {GeminiClient.Excerpt(modelJson)}", ex);
            }
        }
    }
}
