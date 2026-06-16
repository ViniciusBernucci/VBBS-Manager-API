using Microsoft.EntityFrameworkCore;
using VBBSManager.Domain.Entities;

namespace VBBSManager.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Integration> Integrations => Set<Integration>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<PlanningGoal> PlanningGoals => Set<PlanningGoal>();
    public DbSet<FinancialConfig> FinancialConfigs => Set<FinancialConfig>();
    public DbSet<CashFlowTransaction> CashFlowTransactions => Set<CashFlowTransaction>();
    public DbSet<CashFlowConfig> CashFlowConfigs => Set<CashFlowConfig>();
    public DbSet<FixedExpense> FixedExpenses => Set<FixedExpense>();
    public DbSet<FixedExpensePayment> FixedExpensePayments => Set<FixedExpensePayment>();
    public DbSet<DailySalesSummary> DailySalesSummaries => Set<DailySalesSummary>();
    public DbSet<DailyAdSpendSummary> DailyAdSpendSummaries => Set<DailyAdSpendSummary>();
    public DbSet<MetaCampaignDailyInsight> MetaCampaignDailyInsights => Set<MetaCampaignDailyInsight>();
    public DbSet<MetaAdSetDailyInsight> MetaAdSetDailyInsights => Set<MetaAdSetDailyInsight>();
    public DbSet<MetaAdDailyInsight> MetaAdDailyInsights => Set<MetaAdDailyInsight>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
