namespace DebtOptimizer.Dtos
{
    public class DebtPayment
    {
        public string Name { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public decimal AnnualInterestRatePercent { get; set; }
        public decimal MinimumPayment { get; set; }
        public decimal PaymentAmount { get; set; }
        public decimal InterestThisMonth { get; set; }
        public bool IsPriority { get; set; }
    }
}
