using Microsoft.EntityFrameworkCore;
using VBBSManager.Api.Common.Results;
using VBBSManager.Infrastructure.Persistence;

namespace VBBSManager.Api.Features.Planning.Goals.Update;

public interface IUpdatePlanningGoalsService
{
    Task<Result> ExecuteAsync(Guid tenantId, UpdatePlanningGoalsRequest request, CancellationToken ct);
}

public class UpdatePlanningGoalsService(AppDbContext db, ILogger<UpdatePlanningGoalsService> logger)
    : IUpdatePlanningGoalsService
{
    public async Task<Result> ExecuteAsync(Guid tenantId, UpdatePlanningGoalsRequest request, CancellationToken ct)
    {
        var ids = request.Goals.Select(g => g.Id).ToHashSet();

        var goals = await db.PlanningGoals
            .Where(g => g.TenantId == tenantId && ids.Contains(g.Id))
            .ToListAsync(ct);

        foreach (var goal in goals)
        {
            var update = request.Goals.FirstOrDefault(g => g.Id == goal.Id);
            if (update is null) continue;

            goal.TargetValue = update.TargetValue;
            goal.CurrentValue = update.CurrentValue;
            goal.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(ct);
        logger.LogInformation("Updated {Count} planning goals for tenant {TenantId}", goals.Count, tenantId);

        return Result.Ok();
    }
}
