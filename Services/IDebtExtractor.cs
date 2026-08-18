using DebtOptimizer.Dtos;

namespace DebtOptimizer.Services
{
    public interface IDebtExtractor
    {
        Task<ExtractionResult> ExtractAsync(string userText);
    }
}
