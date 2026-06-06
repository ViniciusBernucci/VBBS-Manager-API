using VBBSManager.Domain.Enums;

namespace VBBSManager.Domain.Entities;

public class CashFlowTransaction : BaseEntity
{
    public DateOnly Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public CashFlowCategory Category { get; set; }
}
