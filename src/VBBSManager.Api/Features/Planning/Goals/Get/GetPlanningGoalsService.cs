using Microsoft.EntityFrameworkCore;
using VBBSManager.Api.Common.Results;
using VBBSManager.Domain.Entities;
using VBBSManager.Domain.Enums;
using VBBSManager.Infrastructure.Persistence;

namespace VBBSManager.Api.Features.Planning.Goals.Get;

public interface IGetPlanningGoalsService
{
    Task<Result<PlanningGoalsListResponse>> ExecuteAsync(Guid tenantId, CancellationToken ct);
}

public class GetPlanningGoalsService(AppDbContext db, ILogger<GetPlanningGoalsService> logger)
    : IGetPlanningGoalsService
{
    public async Task<Result<PlanningGoalsListResponse>> ExecuteAsync(Guid tenantId, CancellationToken ct)
    {
        var goals = await db.PlanningGoals
            .Where(g => g.TenantId == tenantId)
            .OrderBy(g => g.Category)
            .ThenBy(g => g.SortOrder)
            .ToListAsync(ct);

        if (goals.Count == 0)
        {
            goals = BuildDefaults(tenantId);
            db.PlanningGoals.AddRange(goals);
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Seeded default planning goals for tenant {TenantId}", tenantId);
        }

        var response = new PlanningGoalsListResponse(
            goals.Select(g => new PlanningGoalResponse(
                g.Id, g.Key, g.Name, g.Description,
                g.TargetValue, g.CurrentValue, g.Unit,
                g.Category.ToString(), g.ComparisonType.ToString(),
                g.ActionIfFailed, g.SortOrder
            )).ToList()
        );

        return Result<PlanningGoalsListResponse>.Ok(response);
    }

    private static List<PlanningGoal> BuildDefaults(Guid tenantId)
    {
        return
        [
            Goal(tenantId, "weekly_revenue", "Faturamento semanal", 2500, "BRL",
                PlanningGoalCategory.WeeklyAlert, PlanningGoalComparison.GreaterThan,
                "Se < R$2.500: revisar criativos e CPA imediatamente", 1),
            Goal(tenantId, "weekly_sales", "Vendas semanais (piso Hotmart)", 47, "count",
                PlanningGoalCategory.WeeklyAlert, PlanningGoalComparison.GreaterThan,
                "Se < 47: realoque tráfego 100% para Reaper Total", 2),
            Goal(tenantId, "cpa_general", "CPA geral (todas campanhas)", 42, "BRL",
                PlanningGoalCategory.WeeklyAlert, PlanningGoalComparison.LessThan,
                "Se > R$50: matar criativos fracos, escalar campeões", 3),
            Goal(tenantId, "refund_rate", "Taxa de reembolso", 2, "percent",
                PlanningGoalCategory.WeeklyAlert, PlanningGoalComparison.LessThan,
                "Se > 3%: checar página de vendas e promessas", 4),
            Goal(tenantId, "cash_reserve", "Saldo de reserva", 5000, "BRL",
                PlanningGoalCategory.WeeklyAlert, PlanningGoalComparison.GreaterThan,
                "Se < R$3k: reduzir tráfego em 15% até recompor", 5),
            Goal(tenantId, "cpm", "CPM (custo por mil impressões)", 30, "BRL",
                PlanningGoalCategory.DailyTraffic, PlanningGoalComparison.LessThan,
                "Alto = público muito concorrido, ampliar segmentação", 1),
            Goal(tenantId, "cpc", "CPC (custo por clique)", 1.5m, "BRL",
                PlanningGoalCategory.DailyTraffic, PlanningGoalComparison.LessThan,
                "Alto = hook do criativo fraco, trocar vídeo/imagem", 2),
            Goal(tenantId, "ctr", "CTR (taxa de clique)", 1.5m, "percent",
                PlanningGoalCategory.DailyTraffic, PlanningGoalComparison.GreaterThan,
                "Alarme se < 1%: criativo não está chamando atenção", 3),
            Goal(tenantId, "frequency", "Frequência do anúncio", 3, "count",
                PlanningGoalCategory.DailyTraffic, PlanningGoalComparison.LessThan,
                "> 3x: fadiga criativa — adicionar novo criativo urgente", 4),
            Goal(tenantId, "hook_rate", "Hook rate (vídeo — primeiros 3s)", 30, "percent",
                PlanningGoalCategory.DailyTraffic, PlanningGoalComparison.GreaterThan,
                "< 20%: refilmar abertura do vídeo", 5),
            Goal(tenantId, "roas", "ROAS bruto", 2, "multiplier",
                PlanningGoalCategory.WeeklyFinancial, PlanningGoalComparison.GreaterThan,
                "Escalar tráfego só acima de 2,0x por 2 semanas seguidas", 1),
            Goal(tenantId, "traffic_percent", "% tráfego / faturamento", 50, "percent",
                PlanningGoalCategory.WeeklyFinancial, PlanningGoalComparison.LessThan,
                "Se > 60%: reduzir tráfego ou aumentar ticket com urgência", 2),
            Goal(tenantId, "bump_conversion", "Taxa conversão bumps (média)", 22, "percent",
                PlanningGoalCategory.WeeklyFinancial, PlanningGoalComparison.GreaterThan,
                "< 15%: revisar copy e posicionamento dos bumps", 3),
            Goal(tenantId, "upsell_conversion", "Taxa conversão upsell", 13, "percent",
                PlanningGoalCategory.WeeklyFinancial, PlanningGoalComparison.GreaterThan,
                "< 10%: revisar VSL ou oferta do upsell", 4),
            Goal(tenantId, "monthly_leads", "Leads capturados / mês (isca)", 300, "count",
                PlanningGoalCategory.MonthlyGrowth, PlanningGoalComparison.GreaterThan,
                "Meta: 300-500 leads/mês no mês 3", 1),
            Goal(tenantId, "email_sales", "Vendas por email / mês", 20, "count",
                PlanningGoalCategory.MonthlyGrowth, PlanningGoalComparison.GreaterThan,
                "Meta: 20-25 vendas/mês no mês 3 (CAC zero)", 2),
        ];
    }

    private static PlanningGoal Goal(
        Guid tenantId, string key, string name, decimal target, string unit,
        PlanningGoalCategory category, PlanningGoalComparison comparison,
        string? action, int order) => new()
    {
        TenantId = tenantId,
        Key = key,
        Name = name,
        TargetValue = target,
        Unit = unit,
        Category = category,
        ComparisonType = comparison,
        ActionIfFailed = action,
        SortOrder = order,
    };
}
