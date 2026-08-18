using DebtOptimizer.Dtos;

namespace DebtOptimizer.Services
{
    public interface IStrategyClassifier
    {
        Task<StrategyInference> ClassifyAsync(string userText);
    }
}
