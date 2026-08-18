using DebtOptimizer.Dtos;
using DebtOptimizer.Services;

namespace DebtOptimizer.Tests
{
    public class ExtractionGapServiceTests
    {
        [Fact]
        public void FindFollowUpQuestions_EverythingStated_AsksNothing()
        {
            var extraction = new ExtractionResult
            {
                Income = 2000m,
                Expenses = 1600m,
                Debts =
                [
                    new DebtDraft { Name = "credit card", Balance = 3000m, AnnualInterestRatePercent = 20m, MinimumPayment = 90m }
                ]
            };

            var questions = ExtractionGapService.FindFollowUpQuestions(extraction);

            Assert.Empty(questions);
        }

        [Fact]
        public void FindFollowUpQuestions_DeadlineMissing_DoesNotCountAsMissing()
        {
            var extraction = new ExtractionResult
            {
                Income = 2000m,
                Expenses = 1600m,
                Debts =
                [
                    new DebtDraft { Name = "car loan", Balance = 10000m, AnnualInterestRatePercent = 5m, MinimumPayment = 200m, PayoffDeadline = null }
                ]
            };

            var questions = ExtractionGapService.FindFollowUpQuestions(extraction);

            Assert.Empty(questions);
        }

        [Fact]
        public void FindFollowUpQuestions_RateMissing_AsksForTheRateByDebtName()
        {
            var extraction = new ExtractionResult
            {
                Income = 2000m,
                Expenses = 1600m,
                Debts =
                [
                    new DebtDraft { Name = "credit card", Balance = 3000m, AnnualInterestRatePercent = null, MinimumPayment = 90m }
                ]
            };

            var questions = ExtractionGapService.FindFollowUpQuestions(extraction);

            Assert.Equal("What is the interest rate on the credit card?", Assert.Single(questions));
        }

        [Fact]
        public void FindFollowUpQuestions_DebtWithNothingStated_AsksForEveryRequiredField()
        {
            var extraction = new ExtractionResult
            {
                Income = 2000m,
                Expenses = 1600m,
                Debts = [new DebtDraft()]
            };

            var questions = ExtractionGapService.FindFollowUpQuestions(extraction);

            Assert.Equal(4, questions.Count);
            Assert.Contains(questions, q => q.Contains("kind of debt"));
            Assert.Contains(questions, q => q.Contains("balance"));
            Assert.Contains(questions, q => q.Contains("interest rate"));
            Assert.Contains(questions, q => q.Contains("minimum monthly payment"));
            Assert.All(questions, q => Assert.Contains("debt 1", q));
        }

        [Fact]
        public void FindFollowUpQuestions_ZeroRateAndZeroBalance_TreatsThemAsStated()
        {
            var extraction = new ExtractionResult
            {
                Income = 2000m,
                Expenses = 1600m,
                Debts =
                [
                    new DebtDraft { Name = "furniture plan", Balance = 0m, AnnualInterestRatePercent = 0m, MinimumPayment = 0m }
                ]
            };

            var questions = ExtractionGapService.FindFollowUpQuestions(extraction);

            Assert.Empty(questions);
        }

        [Fact]
        public void FindFollowUpQuestions_IncomeMissing_AsksForIncomeOnly()
        {
            var extraction = new ExtractionResult
            {
                Income = null,
                Expenses = 1600m,
                Debts =
                [
                    new DebtDraft { Name = "credit card", Balance = 3000m, AnnualInterestRatePercent = 20m, MinimumPayment = 90m }
                ]
            };

            var questions = ExtractionGapService.FindFollowUpQuestions(extraction);

            Assert.Equal("What is your monthly income?", Assert.Single(questions));
        }

        [Fact]
        public void FindFollowUpQuestions_ExpensesMissing_AsksForExpensesOnly()
        {
            var extraction = new ExtractionResult
            {
                Income = 2000m,
                Expenses = null,
                Debts =
                [
                    new DebtDraft { Name = "credit card", Balance = 3000m, AnnualInterestRatePercent = 20m, MinimumPayment = 90m }
                ]
            };

            var questions = ExtractionGapService.FindFollowUpQuestions(extraction);

            var question = Assert.Single(questions);
            Assert.StartsWith("What are your monthly expenses", question);
        }

        [Fact]
        public void FindFollowUpQuestions_NoIncomeNoExpensesNoDebts_AsksForIncomeAndExpenses()
        {
            var extraction = new ExtractionResult();

            var questions = ExtractionGapService.FindFollowUpQuestions(extraction);

            Assert.Equal(2, questions.Count);
        }

        [Fact]
        public void FindFollowUpQuestions_SeveralIncompleteDebts_NamesEachOneSeparately()
        {
            var extraction = new ExtractionResult
            {
                Income = 2000m,
                Expenses = 1600m,
                Debts =
                [
                    new DebtDraft { Name = "credit card", Balance = 3000m, AnnualInterestRatePercent = null, MinimumPayment = null },
                    new DebtDraft { Name = "car loan", Balance = null, AnnualInterestRatePercent = 5m, MinimumPayment = 200m }
                ]
            };

            var questions = ExtractionGapService.FindFollowUpQuestions(extraction);

            Assert.Equal(3, questions.Count);
            Assert.Contains("What is the interest rate on the credit card?", questions);
            Assert.Contains("What is the minimum monthly payment on the credit card?", questions);
            Assert.Contains("What is the outstanding balance on the car loan?", questions);
        }
    }
}
