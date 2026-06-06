namespace VBBSManager.Domain.Entities;

public class FixedExpensePayment : BaseEntity
{
    public Guid FixedExpenseId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public Guid? CashFlowTransactionId { get; set; }

    public FixedExpense FixedExpense { get; set; } = null!;
    public CashFlowTransaction? CashFlowTransaction { get; set; }
}
