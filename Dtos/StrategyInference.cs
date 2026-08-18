using DebtOptimizer.Models;

namespace DebtOptimizer.Dtos
{
    public class StrategyInference
    {
        public PayoffStrategy? Strategy { get; set; }
        public string? TargetDebtName { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
