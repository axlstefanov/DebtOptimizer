namespace DebtOptimizer.Dtos
{
    public class DebtDraft
    {
        public string? Name { get; set; }
        public decimal? Balance { get; set; }
        public decimal? AnnualInterestRatePercent { get; set; }
        public decimal? MinimumPayment { get; set; }
        public DateOnly? PayoffDeadline { get; set; }
    }
}
