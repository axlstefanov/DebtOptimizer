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
            Assert.True(card.IsPriority);

            var loan = result.Payments.Single(p => p.Name == "Car loan");
            Assert.Equal(200m, loan.PaymentAmount);
            Assert.False(loan.IsPriority);
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
    }
}
