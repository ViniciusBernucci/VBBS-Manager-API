using Microsoft.EntityFrameworkCore;
using VBBSManager.Api.Common.Results;
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
        var monthStart = new DateOnly(year, month, 1);
        var monthEnd = monthStart.AddMonths(1);

        var transactions = await db.CashFlowTransactions
            .Where(t => t.TenantId == tenantId && t.Date >= monthStart && t.Date < monthEnd)
            .ToListAsync(ct);

        var config = await db.FinancialConfigs
            .FirstOrDefaultAsync(c => c.TenantId == tenantId, ct);

        var hotmartPix = Sum(transactions, TransactionType.Income, CashFlowCategory.HotmartPix);
        var hotmartCard = Sum(transactions, TransactionType.Income, CashFlowCategory.HotmartCard);
        var otherIncome = Sum(transactions, TransactionType.Income, CashFlowCategory.OtherIncome);
        var grossRevenue = hotmartPix + hotmartCard + otherIncome;

        var hotmartRevenue = hotmartPix + hotmartCard;
        var adSpend = Sum(transactions, TransactionType.Expense, CashFlowCategory.MetaAds);
        var actualTaxes = Sum(transactions, TransactionType.Expense, CashFlowCategory.Taxes);
        var toolsExpense = Sum(transactions, TransactionType.Expense, CashFlowCategory.Tools);
        var otherExpenses = Sum(transactions, TransactionType.Expense, CashFlowCategory.OtherExpense);

        var hotmartFee = hotmartRevenue * (config?.HotmartFeePercent ?? 0.09m);
        var installmentFee = hotmartRevenue * (config?.InstallmentSalesPercent ?? 0.33m) * (config?.InstallmentFeePercent ?? 0.0219m);
        var federalTax = actualTaxes > 0
            ? actualTaxes
            : grossRevenue * (config?.FederalTaxPercent ?? 0.06m);
        var refundCost = grossRevenue * (config?.RefundRatePercent ?? 0.01m);
        var metaAdsTax = adSpend * (config?.MetaAdsTaxPercent ?? 0.10m);

        var configFixed = config is null
            ? 0m
            : config.AccountingCost + config.InvoicingCost + config.ManychatCost + config.HotmartPlayerCost;
        var fixedCosts = toolsExpense + otherExpenses + (toolsExpense + otherExpenses == 0 ? configFixed : 0m);

        var totalDeductions = hotmartFee + installmentFee + federalTax + refundCost;
        var netRevenue = grossRevenue - totalDeductions;
        var adSpendWithTax = adSpend + metaAdsTax;
        var marginAfterTraffic = netRevenue - adSpendWithTax;
        var operationalProfit = marginAfterTraffic - fixedCosts;
        var marginPercent = grossRevenue > 0 ? operationalProfit / grossRevenue * 100 : 0;

        var hasData = transactions.Count > 0;
        var monthProjection = ComputeProjection(year, month, grossRevenue, operationalProfit);
        var weeklyEvolution = BuildWeeklyEvolution(transactions, monthStart, monthEnd, config?.MetaAdsTaxPercent ?? 0.10m);

        var lines = BuildLines(
            grossRevenue, hotmartPix, hotmartCard, otherIncome,
            hotmartFee, installmentFee, federalTax, refundCost, actualTaxes,
            netRevenue, adSpend, metaAdsTax, adSpendWithTax, marginAfterTraffic,
            toolsExpense, otherExpenses, configFixed, fixedCosts, operationalProfit);

        return Result<DreResponse>.Ok(new DreResponse(
            year,
            month,
            hasData,
            new DreSummary(grossRevenue, netRevenue, operationalProfit, marginPercent, monthProjection),
            lines,
            weeklyEvolution
        ));
    }

    private static decimal Sum(
        IEnumerable<Domain.Entities.CashFlowTransaction> txs,
        TransactionType type,
        CashFlowCategory category)
        => txs.Where(t => t.Type == type && t.Category == category).Sum(t => t.Amount);

    private static decimal ComputeProjection(int year, int month, decimal gross, decimal profit)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var monthStart = new DateOnly(year, month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        if (today < monthStart || today > monthEnd)
            return profit;

        var daysElapsed = today.Day;
        var daysInMonth = monthEnd.Day;
        if (daysElapsed <= 0) return profit;

        return profit / daysElapsed * daysInMonth;
    }

    private static List<DreDataPoint> BuildWeeklyEvolution(
        List<Domain.Entities.CashFlowTransaction> txs,
        DateOnly monthStart,
        DateOnly monthEnd,
        decimal metaAdsTaxPercent)
    {
        var points = new List<DreDataPoint>();
        var cursor = monthStart;

        while (cursor <= monthEnd)
        {
            var weekEnd = cursor.AddDays(6);
            if (weekEnd > monthEnd) weekEnd = monthEnd;

            var weekTxs = txs.Where(t => t.Date >= cursor && t.Date <= weekEnd).ToList();
            var revenue = weekTxs.Where(t => t.Type == TransactionType.Income).Sum(t => t.Amount);
            var adSpend = weekTxs.Where(t => t.Category == CashFlowCategory.MetaAds).Sum(t => t.Amount);
            var expenses = weekTxs.Where(t => t.Type == TransactionType.Expense).Sum(t => t.Amount)
                         + adSpend * metaAdsTaxPercent;
            var margin = revenue - expenses;

            points.Add(new DreDataPoint(cursor, revenue, adSpend, margin));
            cursor = weekEnd.AddDays(1);
        }

        return points;
    }

    private static List<DreLineDto> BuildLines(
        decimal gross, decimal hotmartPix, decimal hotmartCard, decimal otherIncome,
        decimal hotmartFee, decimal installmentFee, decimal federalTax, decimal refundCost, decimal actualTaxes,
        decimal netRevenue, decimal adSpend, decimal metaAdsTax, decimal adSpendWithTax, decimal marginAfterTraffic,
        decimal toolsExpense, decimal otherExpenses, decimal configFixed, decimal fixedCosts, decimal operationalProfit)
    {
        decimal Pct(decimal v) => gross > 0 ? Math.Round(v / gross * 100, 1) : 0;

        var lines = new List<DreLineDto>
        {
            Line("gross", "Faturamento bruto", gross, "total", Pct(gross), false),
            Line("section-income", "Receitas", 0, "section", null, false),
            Line("hotmart-pix", "  Vendas Hotmart (Pix)", hotmartPix, "income", Pct(hotmartPix), false),
            Line("hotmart-card", "  Vendas Hotmart (Cartão)", hotmartCard, "income", Pct(hotmartCard), false),
            Line("other-income", "  Outras entradas", otherIncome, "income", Pct(otherIncome), false),
            Line("section-deductions", "Deduções da plataforma", 0, "section", null, false),
            Line("hotmart-fee", "  (−) Taxa Hotmart", -hotmartFee, "deduction", Pct(hotmartFee), true),
            Line("installment-fee", "  (−) Antecipação cartão", -installmentFee, "deduction", Pct(installmentFee), true),
            Line("federal-tax", actualTaxes > 0 ? "  (−) Impostos federais" : "  (−) Impostos federais (estimado)", -federalTax, "deduction", Pct(federalTax), actualTaxes == 0),
            Line("refund", "  (−) Reembolsos (estimado)", -refundCost, "deduction", Pct(refundCost), true),
            Line("net-revenue", "= Receita líquida", netRevenue, "subtotal", Pct(netRevenue), false),
            Line("section-variable", "Custos variáveis", 0, "section", null, false),
            Line("ad-spend", "  (−) Tráfego pago (Meta Ads)", -adSpend, "expense", Pct(adSpend), false),
            Line("meta-tax", "  (−) Imposto Meta (estimado)", -metaAdsTax, "expense", Pct(metaAdsTax), true),
            Line("margin-traffic", "= Margem após tráfego", marginAfterTraffic, "subtotal", Pct(marginAfterTraffic), false),
            Line("section-fixed", "Custos fixos", 0, "section", null, false),
        };

        if (toolsExpense > 0)
            lines.Add(Line("tools", "  (−) Ferramentas / SaaS", -toolsExpense, "expense", Pct(toolsExpense), false));
        if (otherExpenses > 0)
            lines.Add(Line("other-expense", "  (−) Outras saídas", -otherExpenses, "expense", Pct(otherExpenses), false));
        if (toolsExpense + otherExpenses == 0 && configFixed > 0)
            lines.Add(Line("config-fixed", "  (−) Custos fixos (Planejamento)", -configFixed, "expense", Pct(configFixed), true));

        lines.Add(Line("operational-profit", "= Lucro operacional", operationalProfit, "total", Pct(operationalProfit), false));
        return lines;
    }

    private static DreLineDto Line(string key, string label, decimal amount, string kind, decimal? pct, bool estimated)
        => new(key, label, amount, kind, pct, estimated);
}
