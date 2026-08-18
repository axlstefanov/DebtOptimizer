using DebtOptimizer.Models;

namespace DebtOptimizer.Dtos
{
    public class StrategyInferenceResponse
    {
        public StrategyInference Inference { get; set; } = new();
        public PayoffStrategy EffectiveStrategy { get; set; }
        public bool IsClear { get; set; }
    }
}
