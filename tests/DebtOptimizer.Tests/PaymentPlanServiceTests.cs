using DebtOptimizer.Dtos;
using DebtOptimizer.Services;

namespace DebtOptimizer.Tests
{
    public class PaymentPlanServiceTests
    {
        [Fact]
        public void CreatePlan_IncomeCoversAllMinimums_PutsExtraOnHighestRateDebt()
        {
            var service = new PaymentPlanService();
            var request = new CreateProfileRequest
            {
                Name = "Aksel",
                Income = 2000m,
                Expenses = 1600m,
                Debts =
                [
                    new DebtInput { Name = "Credit card", Balance = 3000m, AnnualInterestRatePercent = 20m, MinimumPayment = 90m },
                    new DebtInput { Name = "Car loan", Balance = 10000m, AnnualInterestRatePercent = 5m, MinimumPayment = 200m }
                ]
            };

            var result = service.CreatePlan(request);

            Assert.Equal(400m, result.MoneyAfterExpenses);
            Assert.Equal(290m, result.TotalMinimumPayments);
            Assert.True(result.IsAffordable);

            var card = result.Payments.Single(p => p.Name == "Credit card");
            Assert.Equal(200m, card.PaymentAmount);
            Assert.True(card.IsHighestRate);

            var loan = result.Payments.Single(p => p.Name == "Car loan");
            Assert.Equal(200m, loan.PaymentAmount);
            Assert.False(loan.IsHighestRate);
        }

        [Fact]
        public void CreatePlan_IncomeBelowTotalMinimums_CoversHigherRateDebtFirst()
        {
            var service = new PaymentPlanService();
            var request = new CreateProfileRequest
            {
                Name = "Aksel",
                Income = 2000m,
                Expenses = 1750m,
                Debts =
                [
                    new DebtInput { Name = "Credit card", Balance = 3000m, AnnualInterestRatePercent = 20m, MinimumPayment = 90m },
                    new DebtInput { Name = "Car loan", Balance = 10000m, AnnualInterestRatePercent = 5m, MinimumPayment = 200m }
                ]
            };

            var result = service.CreatePlan(request);

            Assert.Equal(250m, result.MoneyAfterExpenses);
            Assert.False(result.IsAffordable);
            Assert.Equal(90m, result.Payments.Single(p => p.Name == "Credit card").PaymentAmount);
            Assert.Equal(0m, result.Payments.Single(p => p.Name == "Car loan").PaymentAmount);
        }

        [Fact]
        public void CreatePlan_BalanceWithAnnualRate_ReturnsOneTwelfthOfAnnualInterest()
        {
            var service = new PaymentPlanService();
            var request = new CreateProfileRequest
            {
                Name = "Aksel",
                Income = 2000m,
                Expenses = 1600m,
                Debts =
                [
                    new DebtInput { Name = "Credit card", Balance = 3000m, AnnualInterestRatePercent = 20m, MinimumPayment = 90m }
                ]
            };

            var result = service.CreatePlan(request);

            Assert.Equal(50.00m, result.Payments.Single().InterestThisMonth);
        }

        [Fact]
        public void CreatePlan_DebtsInArbitraryOrder_OrdersPaymentsByInterestRateDescending()
        {
            var service = new PaymentPlanService();
            var request = new CreateProfileRequest
            {
                Name = "Aksel",
                Income = 3000m,
                Expenses = 1600m,
                Debts =
                [
                    new DebtInput { Name = "Car loan", Balance = 10000m, AnnualInterestRatePercent = 5m, MinimumPayment = 200m },
                    new DebtInput { Name = "Store card", Balance = 1500m, AnnualInterestRatePercent = 28m, MinimumPayment = 50m },
                    new DebtInput { Name = "Credit card", Balance = 3000m, AnnualInterestRatePercent = 20m, MinimumPayment = 90m }
                ]
            };

            var result = service.CreatePlan(request);

            Assert.Equal(["Store card", "Credit card", "Car loan"], result.Payments.Select(p => p.Name));
        }

        [Fact]
        public void CreatePlan_NoDebts_ReturnsPlanWithoutPayments()
        {
            var service = new PaymentPlanService();
            var request = new CreateProfileRequest
            {
                Name = "Aksel",
                Income = 2000m,
                Expenses = 1600m,
                Debts = []
            };

            var result = service.CreatePlan(request);

            Assert.Empty(result.Payments);
            Assert.Equal(400m, result.MoneyAfterExpenses);
            Assert.Equal(0m, result.TotalMinimumPayments);
            Assert.True(result.IsAffordable);
        }

        [Fact]
        public void CreatePlan_DeadlineReachableWithMoneyLeftOver_ReservesItAndAvalanchesTheRest()
        {
            var service = new PaymentPlanService();
            var today = new DateOnly(2026, 8, 1);
            var deadline = new DateOnly(2027, 2, 1);
            var request = new CreateProfileRequest
            {
                Name = "Aksel",
                Income = 3000m,
                Expenses = 2400m,
                Debts =
                [
                    new DebtInput { Name = "Store card", Balance = 1200m, AnnualInterestRatePercent = 24m, MinimumPayment = 50m, PayoffDeadline = deadline },
                    new DebtInput { Name = "Credit card", Balance = 4000m, AnnualInterestRatePercent = 30m, MinimumPayment = 100m }
                ]
            };

            var result = service.CreatePlan(request, today);

            var store = result.Payments.Single(p => p.Name == "Store card");
            Assert.True(store.DeadlineMet);
            Assert.NotNull(store.ProjectedPayoffDate);
            Assert.True(store.ProjectedPayoffDate <= deadline);
            Assert.InRange(store.PaymentAmount, 185m, 186m);
            Assert.False(store.IsHighestRate);

            var card = result.Payments.Single(p => p.Name == "Credit card");
            Assert.Null(card.DeadlineMet);
            Assert.True(card.IsHighestRate);
            Assert.InRange(card.PaymentAmount, 414m, 415m);
        }

        [Fact]
        public void CreatePlan_DeadlineUnreachable_ReportsTheEarliestAchievablePayoffDate()
        {
            var service = new PaymentPlanService();
            var today = new DateOnly(2026, 8, 1);
            var deadline = new DateOnly(2026, 12, 1);
            var request = new CreateProfileRequest
            {
                Name = "Aksel",
                Income = 2000m,
                Expenses = 1500m,
                Debts =
                [
                    new DebtInput { Name = "Car loan", Balance = 10000m, AnnualInterestRatePercent = 20m, MinimumPayment = 200m, PayoffDeadline = deadline }
                ]
            };

            var result = service.CreatePlan(request, today);

            var loan = result.Payments.Single();
            Assert.True(result.IsAffordable);
            Assert.False(loan.DeadlineMet);
            Assert.Equal(new DateOnly(2028, 8, 1), loan.ProjectedPayoffDate);
            Assert.Equal(500m, loan.PaymentAmount);
        }

        [Fact]
        public void CreatePlan_TwoDebtsWithDeadlines_ReservesForBoth()
        {
            var service = new PaymentPlanService();
            var today = new DateOnly(2026, 8, 1);
            var storeDeadline = new DateOnly(2027, 2, 1);
            var loanDeadline = new DateOnly(2027, 8, 1);
            var request = new CreateProfileRequest
            {
                Name = "Aksel",
                Income = 3000m,
                Expenses = 2400m,
                Debts =
                [
                    new DebtInput { Name = "Store card", Balance = 1200m, AnnualInterestRatePercent = 24m, MinimumPayment = 50m, PayoffDeadline = storeDeadline },
                    new DebtInput { Name = "Personal loan", Balance = 3000m, AnnualInterestRatePercent = 6m, MinimumPayment = 100m, PayoffDeadline = loanDeadline }
                ]
            };

            var result = service.CreatePlan(request, today);

            var store = result.Payments.Single(p => p.Name == "Store card");
            var loan = result.Payments.Single(p => p.Name == "Personal loan");

            Assert.True(store.DeadlineMet);
            Assert.True(store.ProjectedPayoffDate <= storeDeadline);
            Assert.True(loan.DeadlineMet);
            Assert.True(loan.ProjectedPayoffDate <= loanDeadline);
            Assert.InRange(loan.PaymentAmount, 238m, 240m);
            Assert.Equal(600m, store.PaymentAmount + loan.PaymentAmount);
        }

        [Fact]
        public void CreatePlan_DeadlineStarvesHigherRateDebt_ReportsExtraInterestOverTheCheapestPlan()
        {
            var service = new PaymentPlanService();
            var today = new DateOnly(2026, 8, 1);
            var loanDeadline = new DateOnly(2027, 8, 1);
            var request = new CreateProfileRequest
            {
                Name = "Aksel",
                Income = 3500m,
                Expenses = 2500m,
                Debts =
                [
                    new DebtInput { Name = "Credit card", Balance = 5000m, AnnualInterestRatePercent = 30m, MinimumPayment = 100m },
                    new DebtInput { Name = "Car loan", Balance = 8000m, AnnualInterestRatePercent = 4m, MinimumPayment = 150m, PayoffDeadline = loanDeadline }
                ]
            };

            var result = service.CreatePlan(request, today);

            var card = result.Payments.Single(p => p.Name == "Credit card");
            var loan = result.Payments.Single(p => p.Name == "Car loan");

            Assert.True(loan.DeadlineMet);
            Assert.InRange(loan.PaymentAmount, 629m, 630m);
            Assert.True(card.IsHighestRate);
            Assert.InRange(card.PaymentAmount, 370m, 371m);
            Assert.True(result.ExtraInterestFromDeadlines > 0m);
        }

        [Theory]
        [InlineData(2026, 6, 1)]
        [InlineData(2026, 8, 31)]
        public void CreatePlan_DeadlineInThePastOrThisMonth_MissesItWithoutThrowing(int year, int month, int day)
        {
            var service = new PaymentPlanService();
            var today = new DateOnly(2026, 8, 18);
            var deadline = new DateOnly(year, month, day);
            var request = new CreateProfileRequest
            {
                Name = "Aksel",
                Income = 2000m,
                Expenses = 1600m,
                Debts =
                [
                    new DebtInput { Name = "Store card", Balance = 2000m, AnnualInterestRatePercent = 18m, MinimumPayment = 50m, PayoffDeadline = deadline }
                ]
            };

            var result = service.CreatePlan(request, today);

            var store = result.Payments.Single();
            Assert.True(result.IsAffordable);
            Assert.False(store.DeadlineMet);
            Assert.Equal(400m, store.PaymentAmount);
            Assert.Equal(new DateOnly(2027, 1, 18), store.ProjectedPayoffDate);
        }
    }
}
