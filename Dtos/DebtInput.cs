namespace DebtOptimizer.Dtos
{
    public class DebtInput
    {
        public string Name { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public decimal AnnualInterestRatePercent { get; set; }
        public decimal MinimumPayment { get; set; }
    }
}
