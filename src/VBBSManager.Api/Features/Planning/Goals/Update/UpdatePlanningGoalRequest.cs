namespace VBBSManager.Api.Features.Planning.Goals.Update;

public record UpdatePlanningGoalItem(
    Guid Id,
    decimal TargetValue,
    decimal? CurrentValue
);

public record UpdatePlanningGoalsRequest(IReadOnlyList<UpdatePlanningGoalItem> Goals);
