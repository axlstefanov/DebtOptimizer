namespace DebtOptimizer.Dtos
{
    public class CreateProfileRequest
    {
        public string Name { get; set; } = string.Empty;
        public decimal Income { get; set; }
        public decimal Expenses { get; set; }
        public List<DebtInput> Debts { get; set; } = [];
    }
}
