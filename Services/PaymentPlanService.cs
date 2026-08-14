using DebtOptimizer.Dtos;

namespace DebtOptimizer.Services
{
    public class PaymentPlanService
    {
        public PaymentPlanResponse CreatePlan(CreateProfileRequest request)
        {
            var moneyAfterExpenses = request.Income - request.Expenses;
            var totalMinimums = request.Debts.Sum(d => d.MinimumPayment);

            var response = new PaymentPlanResponse
            {
                Name = request.Name,
                MoneyAfterExpenses = moneyAfterExpenses,
                TotalMinimumPayments = totalMinimums,
                IsAffordable = moneyAfterExpenses >= totalMinimums
            };

            var ranked = request.Debts
                .OrderByDescending(d => d.AnnualInterestRatePercent)
                .ToList();

            if (!response.IsAffordable)
            {
                var remaining = moneyAfterExpenses;

                foreach (var debt in ranked)
                {
                    var covered = remaining >= debt.MinimumPayment;
                    if (covered) remaining -= debt.MinimumPayment;

                    response.Payments.Add(new DebtPayment
                    {
                        Name = debt.Name,
                        Balance = debt.Balance,
                        AnnualInterestRatePercent = debt.AnnualInterestRatePercent,
                        MinimumPayment = debt.MinimumPayment,
                        PaymentAmount = covered ? debt.MinimumPayment : 0,
                        InterestThisMonth = MonthlyInterest(debt.Balance, debt.AnnualInterestRatePercent),
                        IsPriority = false
                    });
                }

                return response;
            }

            var extra = moneyAfterExpenses - totalMinimums;

            for (var i = 0; i < ranked.Count; i++)
            {
                var debt = ranked[i];
                var isPriority = i == 0;

                response.Payments.Add(new DebtPayment
                {
                    Name = debt.Name,
                    Balance = debt.Balance,
                    AnnualInterestRatePercent = debt.AnnualInterestRatePercent,
                    MinimumPayment = debt.MinimumPayment,
                    PaymentAmount = isPriority ? debt.MinimumPayment + extra : debt.MinimumPayment,
                    InterestThisMonth = MonthlyInterest(debt.Balance, debt.AnnualInterestRatePercent),
                    IsPriority = isPriority
                });
            }

            return response;
        }

        private static decimal MonthlyInterest(decimal balance, decimal annualRatePercent)
            => Math.Round(balance * (annualRatePercent / 100m) / 12m, 2);
    }
}
