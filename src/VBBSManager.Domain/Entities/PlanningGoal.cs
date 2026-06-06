using VBBSManager.Domain.Enums;

namespace VBBSManager.Domain.Entities;

public class PlanningGoal : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal TargetValue { get; set; }
    public decimal? CurrentValue { get; set; }
    public string Unit { get; set; } = string.Empty;
    public PlanningGoalCategory Category { get; set; }
    public PlanningGoalComparison ComparisonType { get; set; }
    public string? ActionIfFailed { get; set; }
    public int SortOrder { get; set; }
}
