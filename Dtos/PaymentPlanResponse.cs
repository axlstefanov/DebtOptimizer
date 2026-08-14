namespace DebtOptimizer.Dtos
{
    public class PaymentPlanResponse
    {
        public string Name { get; set; } = string.Empty;
        public decimal MoneyAfterExpenses { get; set; }
        public decimal TotalMinimumPayments { get; set; }
        public bool IsAffordable { get; set; }
        public List<DebtPayment> Payments { get; set; } = [];
    }
}
