namespace DebtOptimizer.Models
{
    public class Debt
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public decimal InterestRate { get; set; }
        public decimal MinimumPayment { get; set; }
        public int FinancialProfileId { get; set; }
        public FinancialProfile? Profile { get; set; }
    }
}
