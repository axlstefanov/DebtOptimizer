using DebtOptimizer.Dtos;
using DebtOptimizer.Models;

namespace DebtOptimizer.Services
{
    public class PaymentPlanService
    {
        private const int MaxProjectionMonths = 1200;

        public PaymentPlanResponse CreatePlan(CreateProfileRequest request)
            => CreatePlan(request, DateOnly.FromDateTime(DateTime.UtcNow));

        public PaymentPlanResponse CreatePlan(CreateProfileRequest request, DateOnly today)
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
                        ReceivesSurplus = false
                    });
                }

                return response;
            }

            var strategy = request.PayoffStrategy;
            var targetName = strategy == PayoffStrategy.Target ? request.TargetDebtName : null;

            var deadlinePlan = Project(ranked, moneyAfterExpenses, today, true, strategy, targetName);
            var cheapestPlan = Project(ranked, moneyAfterExpenses, today, false, strategy, targetName);

            response.ExtraInterestFromDeadlines =
                Math.Round(deadlinePlan.TotalInterest - cheapestPlan.TotalInterest, 2);

            var surplusTarget = SurplusOrder(deadlinePlan.Debts, strategy, targetName, true).FirstOrDefault();

            foreach (var projected in deadlinePlan.Debts)
            {
                var debt = projected.Debt;
                var payoffDate = projected.PayoffMonth.HasValue
                    ? today.AddMonths(projected.PayoffMonth.Value - 1)
                    : (DateOnly?)null;

                response.Payments.Add(new DebtPayment
                {
                    Name = debt.Name,
                    Balance = debt.Balance,
                    AnnualInterestRatePercent = debt.AnnualInterestRatePercent,
                    MinimumPayment = debt.MinimumPayment,
                    PaymentAmount = Math.Round(projected.FirstPayment, 2),
                    InterestThisMonth = MonthlyInterest(debt.Balance, debt.AnnualInterestRatePercent),
                    ReceivesSurplus = projected == surplusTarget,
                    DeadlineMet = debt.PayoffDeadline.HasValue
                        ? payoffDate.HasValue && payoffDate.Value <= debt.PayoffDeadline.Value
                        : null,
                    ProjectedPayoffDate = payoffDate
                });
            }

            return response;
        }

        private static PlanProjection Project(
            List<DebtInput> ranked, decimal monthlyBudget, DateOnly today, bool respectDeadlines,
            PayoffStrategy strategy, string? targetName)
        {
            var plan = new PlanProjection
            {
                Debts = ranked.Select(d => new DebtProjection { Debt = d, Balance = d.Balance }).ToList()
            };

            for (var month = 1; month <= MaxProjectionMonths; month++)
            {
                var active = plan.Debts.Where(d => d.Balance > 0m).ToList();
                if (active.Count == 0) break;

                var payments = Allocate(
                    active, monthlyBudget, today.AddMonths(month - 1), respectDeadlines, strategy, targetName);
                var stalled = true;

                foreach (var debt in active)
                {
                    var rate = debt.Debt.AnnualInterestRatePercent;
                    var interest = debt.Balance * MonthlyRate(rate);

                    if (MonthsToClearBalanceAt(debt.Balance, rate, payments[debt]).HasValue) stalled = false;

                    plan.TotalInterest += interest;
                    debt.Balance += interest - payments[debt];

                    if (month == 1) debt.FirstPayment = payments[debt];

                    if (debt.Balance <= 0m)
                    {
                        debt.Balance = 0m;
                        debt.PayoffMonth = month;
                    }
                }

                if (stalled) break;
            }

            return plan;
        }

        private static Dictionary<DebtProjection, decimal> Allocate(
            List<DebtProjection> active, decimal monthlyBudget, DateOnly paymentDate, bool respectDeadlines,
            PayoffStrategy strategy, string? targetName)
        {
            var payments = active.ToDictionary(d => d, _ => 0m);
            var owed = active.ToDictionary(
                d => d,
                d => d.Balance * (1m + MonthlyRate(d.Debt.AnnualInterestRatePercent)));
            var remaining = monthlyBudget;

            foreach (var debt in active)
            {
                var payment = Math.Min(Math.Min(debt.Debt.MinimumPayment, owed[debt]), remaining);
                payments[debt] = payment;
                remaining -= payment;
            }

            if (respectDeadlines)
            {
                var byDeadline = active
                    .Where(d => d.Debt.PayoffDeadline.HasValue)
                    .OrderBy(d => d.Debt.PayoffDeadline!.Value);

                foreach (var debt in byDeadline)
                {
                    var months = MonthsAvailableUntil(paymentDate, debt.Debt.PayoffDeadline!.Value);
                    var required = PaymentToClearBalanceIn(
                        debt.Balance, debt.Debt.AnnualInterestRatePercent, months);

                    var reservation = Math.Min(Math.Min(required, owed[debt]) - payments[debt], remaining);
                    if (reservation <= 0m) continue;

                    payments[debt] += reservation;
                    remaining -= reservation;
                }
            }

            foreach (var debt in SurplusOrder(active, strategy, targetName, respectDeadlines))
            {
                if (remaining <= 0m) break;

                var surplus = Math.Min(owed[debt] - payments[debt], remaining);
                if (surplus <= 0m) continue;

                payments[debt] += surplus;
                remaining -= surplus;
            }

            return payments;
        }

        private static IOrderedEnumerable<DebtProjection> SurplusOrder(
            IEnumerable<DebtProjection> debts, PayoffStrategy strategy, string? targetName, bool respectDeadlines)
        {
            var ordered = debts.OrderByDescending(d => IsTarget(d.Debt, targetName));
            if (respectDeadlines) ordered = ordered.ThenBy(d => d.Debt.PayoffDeadline.HasValue);

            return strategy == PayoffStrategy.Snowball
                ? ordered.ThenBy(d => d.Debt.Balance)
                : ordered.ThenByDescending(d => d.Debt.AnnualInterestRatePercent);
        }

        private static bool IsTarget(DebtInput debt, string? targetName)
            => !string.IsNullOrWhiteSpace(targetName)
                && string.Equals(debt.Name, targetName, StringComparison.OrdinalIgnoreCase);

        private static int MonthsAvailableUntil(DateOnly paymentDate, DateOnly deadline)
        {
            var months = ((deadline.Year - paymentDate.Year) * 12) + deadline.Month - paymentDate.Month;
            if (paymentDate.AddMonths(months) > deadline) months--;

            return Math.Max(1, months + 1);
        }

        private static decimal PaymentToClearBalanceIn(decimal balance, decimal annualRatePercent, int months)
        {
            var rate = MonthlyRate(annualRatePercent);
            if (rate == 0m) return balance / months;
            if (months == 1) return balance * (1m + rate);

            var discounted = (decimal)Math.Pow(1d + (double)rate, -months);

            return balance * rate / (1m - discounted);
        }

        private static int? MonthsToClearBalanceAt(decimal balance, decimal annualRatePercent, decimal payment)
        {
            if (balance <= 0m) return 0;
            if (payment <= 0m) return null;

            var rate = MonthlyRate(annualRatePercent);
            if (rate == 0m) return (int)Math.Ceiling(balance / payment);

            var interest = balance * rate;
            if (payment <= interest) return null;

            var months = -Math.Log(1d - (double)(interest / payment)) / Math.Log(1d + (double)rate);

            return (int)Math.Ceiling(months);
        }

        private static decimal MonthlyRate(decimal annualRatePercent)
            => annualRatePercent / 100m / 12m;

        private static decimal MonthlyInterest(decimal balance, decimal annualRatePercent)
            => Math.Round(balance * (annualRatePercent / 100m) / 12m, 2);

        private sealed class DebtProjection
        {
            public DebtInput Debt { get; init; } = new();
            public decimal Balance { get; set; }
            public decimal FirstPayment { get; set; }
            public int? PayoffMonth { get; set; }
        }

        private sealed class PlanProjection
        {
            public List<DebtProjection> Debts { get; init; } = [];
            public decimal TotalInterest { get; set; }
        }
    }
}
