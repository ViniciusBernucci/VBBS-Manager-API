namespace VBBSManager.Api.Features.Planning.Goals.Get;

public record PlanningGoalResponse(
    Guid Id,
    string Key,
    string Name,
    string? Description,
    decimal TargetValue,
    decimal? CurrentValue,
    string Unit,
    string Category,
    string ComparisonType,
    string? ActionIfFailed,
    int SortOrder
);

public record PlanningGoalsListResponse(IReadOnlyList<PlanningGoalResponse> Goals);
