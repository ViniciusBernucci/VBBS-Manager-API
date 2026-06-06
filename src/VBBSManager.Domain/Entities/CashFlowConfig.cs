namespace VBBSManager.Domain.Entities;

public class CashFlowConfig : BaseEntity
{
    public int InitialYear { get; set; }
    public int InitialMonth { get; set; }
    public decimal InitialBalance { get; set; }
}
