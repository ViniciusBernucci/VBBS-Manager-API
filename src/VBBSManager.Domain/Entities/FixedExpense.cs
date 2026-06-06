using VBBSManager.Domain.Enums;

namespace VBBSManager.Domain.Entities;

public class FixedExpense : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public CashFlowCategory Category { get; set; }
    public bool IsActive { get; set; } = true;
}
