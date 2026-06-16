using Microsoft.EntityFrameworkCore;
using VBBSManager.Api.Common.Results;
using VBBSManager.Domain.Entities;
using VBBSManager.Domain.Enums;
using VBBSManager.Infrastructure.Persistence;

namespace VBBSManager.Api.Features.Financial.DRE;

public interface IDreService
{
    Task<Result<DreResponse>> ExecuteAsync(Guid tenantId, int year, int month, CancellationToken ct);
}

public class DreService(AppDbContext db) : IDreService
{
    public async Task<Result<DreResponse>> ExecuteAsync(Guid tenantId, int year, int month, CancellationToken ct)
    {
        if (year < 2020 || year > DateTime.UtcNow.Year + 1)
            return Result<DreResponse>.Fail("Ano inválido.");
        if (month < 1 || month > 12)
            return Result<DreResponse>.Fail("Mês inválido (1–12).");

        var monthStart = new DateOnly(year, month, 1);
        var monthEnd   = new DateOnly(year, month, DateTime.DaysInMonth(year, month));

        // ── Fontes de dados (sequencial — EF Core não suporta queries paralelas no mesmo contexto) ──
        var sales = await db.DailySalesSummaries
            .Where(x => x.TenantId == tenantId && x.Date >= monthStart && x.Date <= monthEnd)
            .AsNoTracking()
            .ToListAsync(ct);

        var metaSpendSummaries = await db.DailyAdSpendSummaries
            .Where(x => x.TenantId == tenantId && x.Date >= monthStart && x.Date <= monthEnd)
            .AsNoTracking()
            .ToListAsync(ct);

        var transactions = await db.CashFlowTransactions
            .Where(t => t.TenantId == tenantId && t.Date >= monthStart && t.Date <= monthEnd)
            .AsNoTracking()
            .ToListAsync(ct);

        var fixedExpenses = await db.FixedExpenses
            .Where(x => x.TenantId == tenantId && x.IsActive)
            .AsNoTracking()
            .ToListAsync(ct);

        var config = await db.FinancialConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.TenantId == tenantId, ct);

        // ── Receita ──────────────────────────────────────────────────────────
        var hotmartGross  = sales.Sum(r => r.GrossRevenue);  // real — Hotmart sync
        var hotmartFee    = sales.Sum(r => r.HotmartFeeAmount); // real — calculado no sync
        var totalSales    = sales.Sum(r => r.TotalSales);

        var otherIncome   = SumTx(transactions, TransactionType.Income, CashFlowCategory.OtherIncome);
        var grossRevenue  = hotmartGross + otherIncome;

        // ── Deduções ─────────────────────────────────────────────────────────
        // Hotmart fee: real (do sync). Demais: estimados via config.
        var installmentFee = hotmartGross
            * (config?.InstallmentSalesPercent ?? 0.33m)
            * (config?.InstallmentFeePercent   ?? 0.0219m);

        var actualTaxes = SumTx(transactions, TransactionType.Expense, CashFlowCategory.Taxes);
        var federalTax  = actualTaxes > 0
            ? actualTaxes
            : grossRevenue * (config?.FederalTaxPercent ?? 0.06m);

        var refundCost  = grossRevenue * (config?.RefundRatePercent ?? 0.01m);

        var netRevenue  = grossRevenue - hotmartFee - installmentFee - federalTax - refundCost;

        // ── Custos variáveis — Meta Ads ───────────────────────────────────────
        // Prefere resumo diário sincronizado; cai para lançamento manual se não há sync.
        var metaSpendAuto   = metaSpendSummaries.Sum(r => r.TotalSpend);
        var metaSpendManual = SumTx(transactions, TransactionType.Expense, CashFlowCategory.MetaAds);
        var adSpend         = metaSpendAuto > 0 ? metaSpendAuto : metaSpendManual;
        var metaAdsSynced   = metaSpendAuto > 0;

        var metaAdsTax     = adSpend * (config?.MetaAdsTaxPercent ?? 0.10m);
        var adSpendWithTax = adSpend + metaAdsTax;
        var marginAfterTraffic = netRevenue - adSpendWithTax;

        // ── Custos fixos ─────────────────────────────────────────────────────
        // Gastos fixos cadastrados (FixedExpenses) + lançamentos avulsos no CashFlow
        var fixedRegisteredTotal = fixedExpenses.Sum(x => x.Amount);
        var toolsExpense         = SumTx(transactions, TransactionType.Expense, CashFlowCategory.Tools);
        var otherExpenses        = SumTx(transactions, TransactionType.Expense, CashFlowCategory.OtherExpense);
        var totalFixedCosts      = fixedRegisteredTotal + toolsExpense + otherExpenses;

        var operationalProfit = marginAfterTraffic - totalFixedCosts;
        var marginPercent     = grossRevenue > 0 ? operationalProfit / grossRevenue * 100 : 0;

        var hasData          = sales.Count > 0 || transactions.Count > 0 || fixedExpenses.Count > 0;
        var monthProjection  = ComputeProjection(year, month, grossRevenue, operationalProfit);
        var weeklyEvolution  = BuildWeeklyEvolution(
            monthStart, monthEnd, sales, metaSpendSummaries, transactions, metaAdsSynced,
            config?.MetaAdsTaxPercent ?? 0.10m);

        var lines = BuildLines(
            hotmartGross, otherIncome, grossRevenue,
            hotmartFee, installmentFee, federalTax, refundCost, actualTaxes,
            netRevenue, adSpend, metaAdsTax, adSpendWithTax, metaAdsSynced, marginAfterTraffic,
            fixedExpenses, toolsExpense, otherExpenses, operationalProfit);

        return Result<DreResponse>.Ok(new DreResponse(
            year, month, hasData, metaAdsSynced,
            new DreSummary(totalSales, grossRevenue, netRevenue, operationalProfit, marginPercent, monthProjection),
            lines, weeklyEvolution));
    }

    private static decimal SumTx(
        IEnumerable<CashFlowTransaction> txs,
        TransactionType type,
        CashFlowCategory category)
        => txs.Where(t => t.Type == type && t.Category == category).Sum(t => t.Amount);

    private static decimal ComputeProjection(int year, int month, decimal gross, decimal profit)
    {
        var today      = DateOnly.FromDateTime(DateTime.UtcNow);
        var monthStart = new DateOnly(year, month, 1);
        var monthEnd   = monthStart.AddMonths(1).AddDays(-1);

        if (today < monthStart || today > monthEnd) return profit;

        var daysElapsed = today.Day;
        var daysInMonth = monthEnd.Day;
        if (daysElapsed <= 0) return profit;

        return profit / daysElapsed * daysInMonth;
    }

    private static List<DreDataPoint> BuildWeeklyEvolution(
        DateOnly monthStart,
        DateOnly monthEnd,
        List<DailySalesSummary> sales,
        List<DailyAdSpendSummary> metaSummaries,
        List<CashFlowTransaction> transactions,
        bool metaAdsSynced,
        decimal metaAdsTaxPercent)
    {
        var points = new List<DreDataPoint>();
        var cursor = monthStart;

        while (cursor <= monthEnd)
        {
            var weekEnd = cursor.AddDays(6);
            if (weekEnd > monthEnd) weekEnd = monthEnd;

            var revenue = sales
                .Where(s => s.Date >= cursor && s.Date <= weekEnd)
                .Sum(s => s.GrossRevenue);

            var adSpend = metaAdsSynced
                ? metaSummaries.Where(m => m.Date >= cursor && m.Date <= weekEnd).Sum(m => m.TotalSpend)
                : transactions
                    .Where(t => t.Date >= cursor && t.Date <= weekEnd && t.Category == CashFlowCategory.MetaAds)
                    .Sum(t => t.Amount);

            var margin = revenue - adSpend - adSpend * metaAdsTaxPercent;

            points.Add(new DreDataPoint(cursor, revenue, adSpend, margin));
            cursor = weekEnd.AddDays(1);
        }

        return points;
    }

    private static List<DreLineDto> BuildLines(
        decimal hotmartGross, decimal otherIncome, decimal grossRevenue,
        decimal hotmartFee, decimal installmentFee, decimal federalTax, decimal refundCost, decimal actualTaxes,
        decimal netRevenue, decimal adSpend, decimal metaAdsTax, decimal adSpendWithTax, bool metaAdsSynced,
        decimal marginAfterTraffic,
        List<FixedExpense> fixedExpenses, decimal toolsExpense, decimal otherExpenses,
        decimal operationalProfit)
    {
        decimal Pct(decimal v) => grossRevenue > 0 ? Math.Round(v / grossRevenue * 100, 1) : 0;

        var lines = new List<DreLineDto>
        {
            Line("gross",          "Faturamento bruto",       grossRevenue,  "total",    Pct(grossRevenue),  false),
            Line("section-income", "Receitas",                0,             "section",  null,               false),
            Line("hotmart",        "  Vendas Hotmart",        hotmartGross,  "income",   Pct(hotmartGross),  false),
        };

        if (otherIncome > 0)
            lines.Add(Line("other-income", "  Outras entradas", otherIncome, "income", Pct(otherIncome), false));

        lines.AddRange([
            Line("section-deductions", "Deduções",                                0,              "section",   null,               false),
            Line("hotmart-fee",        "  (−) Taxa Hotmart",                      -hotmartFee,    "deduction", Pct(hotmartFee),    false),
            Line("installment-fee",    "  (−) Antecipação cartão (estimado)",     -installmentFee,"deduction", Pct(installmentFee),true),
            Line("federal-tax",        actualTaxes > 0
                                           ? "  (−) Impostos federais"
                                           : "  (−) Impostos federais (estimado)",-federalTax,   "deduction", Pct(federalTax),    actualTaxes == 0),
            Line("refund",             "  (−) Reembolsos (estimado)",             -refundCost,    "deduction", Pct(refundCost),    true),
            Line("net-revenue",        "= Receita líquida",                        netRevenue,    "subtotal",  Pct(netRevenue),    false),

            Line("section-variable",   "Custos variáveis",                         0,             "section",   null,               false),
            Line("ad-spend",           metaAdsSynced
                                           ? "  (−) Meta Ads"
                                           : "  (−) Meta Ads (manual)",           -adSpend,       "expense",   Pct(adSpend),      !metaAdsSynced),
            Line("meta-tax",           "  (−) Imposto Meta (estimado)",            -metaAdsTax,   "expense",   Pct(metaAdsTax),   true),
            Line("margin-traffic",     "= Margem após tráfego",                    marginAfterTraffic,"subtotal",Pct(marginAfterTraffic),false),

            Line("section-fixed",      "Custos fixos",                             0,             "section",   null,               false),
        ]);

        for (var i = 0; i < fixedExpenses.Count; i++)
        {
            var fe = fixedExpenses[i];
            lines.Add(Line($"fixed-{i}", $"  (−) {fe.Name}", -fe.Amount, "expense", Pct(fe.Amount), false));
        }

        if (toolsExpense > 0)
            lines.Add(Line("tools", "  (−) Ferramentas / SaaS", -toolsExpense, "expense", Pct(toolsExpense), false));
        if (otherExpenses > 0)
            lines.Add(Line("other-expense", "  (−) Outras saídas", -otherExpenses, "expense", Pct(otherExpenses), false));

        lines.Add(Line("operational-profit", "= Lucro operacional", operationalProfit, "total", Pct(operationalProfit), false));

        return lines;
    }

    private static DreLineDto Line(string key, string label, decimal amount, string kind, decimal? pct, bool estimated)
        => new(key, label, amount, kind, pct, estimated);
}
