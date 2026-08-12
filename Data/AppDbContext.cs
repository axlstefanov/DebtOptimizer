using DebtOptimizer.Models;
using Microsoft.EntityFrameworkCore;

namespace DebtOptimizer.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<FinancialProfile> FinancialProfiles { get; set; }
        public DbSet<Debt> Debts { get; set; }
    }
}
