namespace DebtOptimizer.Models
{
    public class FinancialProfile
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Income { get; set; }
        public decimal Expenses { get; set; }
        public PayoffStrategy PayoffStrategy { get; set; }
        public string? TargetDebtName { get; set; }
        public List<Debt> Debts { get; set; } = [];
        public decimal AvailableForDebt => Income - Expenses;
    }
}
