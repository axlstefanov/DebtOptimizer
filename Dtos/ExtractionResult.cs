namespace DebtOptimizer.Dtos
{
    public class ExtractionResult
    {
        public List<DebtDraft> Debts { get; set; } = [];
        public decimal? Income { get; set; }
        public decimal? Expenses { get; set; }
    }
}
