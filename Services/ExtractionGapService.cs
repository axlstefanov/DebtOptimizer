using DebtOptimizer.Dtos;

namespace DebtOptimizer.Services
{
    public static class ExtractionGapService
    {
        public static List<string> FindFollowUpQuestions(ExtractionResult extraction)
        {
            var questions = new List<string>();

            for (var i = 0; i < extraction.Debts.Count; i++)
            {
                var debt = extraction.Debts[i];
                var subject = Describe(debt, i + 1);

                if (string.IsNullOrWhiteSpace(debt.Name))
                    questions.Add($"What kind of debt is {subject}? For example a credit card or a car loan.");

                if (debt.Balance == null)
                    questions.Add($"What is the outstanding balance on {subject}?");

                if (debt.AnnualInterestRatePercent == null)
                    questions.Add($"What is the interest rate on {subject}?");

                if (debt.MinimumPayment == null)
                    questions.Add($"What is the minimum monthly payment on {subject}?");
            }

            if (extraction.Income == null)
                questions.Add("What is your monthly income?");

            if (extraction.Expenses == null)
                questions.Add("What are your monthly expenses, not counting debt payments?");

            return questions;
        }

        private static string Describe(DebtDraft debt, int position)
            => string.IsNullOrWhiteSpace(debt.Name) ? $"debt {position}" : $"the {debt.Name}";
    }
}
