using DebtOptimizer.Data;
using DebtOptimizer.Dtos;
using DebtOptimizer.Models;
using Microsoft.EntityFrameworkCore;

namespace DebtOptimizer.Services
{
    public class ProfileService(AppDbContext db)
    {
        public async Task<int> SaveProfileAsync(CreateProfileRequest request)
        {
            var profile = new FinancialProfile
            {
                Name = request.Name,
                Income = request.Income,
                Expenses = request.Expenses,
                PayoffStrategy = request.PayoffStrategy,
                TargetDebtName = request.TargetDebtName,
                Debts = request.Debts.Select(d => new Debt
                {
                    Name = d.Name,
                    Balance = d.Balance,
                    AnnualInterestRatePercent = d.AnnualInterestRatePercent,
                    MinimumPayment = d.MinimumPayment,
                    PayoffDeadline = d.PayoffDeadline
                }).ToList()
            };

            db.FinancialProfiles.Add(profile);
            await db.SaveChangesAsync();

            return profile.Id;
        }

        public async Task<ProfileResponse?> GetProfileAsync(int id)
        {
            var profile = await LoadProfileAsync(id);
            if (profile == null) return null;

            return new ProfileResponse
            {
                Id = profile.Id,
                Name = profile.Name,
                Income = profile.Income,
                Expenses = profile.Expenses,
                PayoffStrategy = profile.PayoffStrategy,
                TargetDebtName = profile.TargetDebtName,
                Debts = profile.Debts.Select(ToDebtInput).ToList()
            };
        }

        public async Task<CreateProfileRequest?> GetProfileRequestAsync(int id)
        {
            var profile = await LoadProfileAsync(id);
            if (profile == null) return null;

            return new CreateProfileRequest
            {
                Name = profile.Name,
                Income = profile.Income,
                Expenses = profile.Expenses,
                PayoffStrategy = profile.PayoffStrategy,
                TargetDebtName = profile.TargetDebtName,
                Debts = profile.Debts.Select(ToDebtInput).ToList()
            };
        }

        private Task<FinancialProfile?> LoadProfileAsync(int id)
            => db.FinancialProfiles
                .AsNoTracking()
                .Include(p => p.Debts)
                .FirstOrDefaultAsync(p => p.Id == id);

        private static DebtInput ToDebtInput(Debt debt)
            => new()
            {
                Name = debt.Name,
                Balance = debt.Balance,
                AnnualInterestRatePercent = debt.AnnualInterestRatePercent,
                MinimumPayment = debt.MinimumPayment,
                PayoffDeadline = debt.PayoffDeadline
            };
    }
}
