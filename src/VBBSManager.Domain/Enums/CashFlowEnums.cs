namespace VBBSManager.Domain.Enums;

public enum TransactionType
{
    Income = 1,
    Expense = 2
}

public enum CashFlowCategory
{
    // Income
    HotmartPix = 1,
    HotmartCard = 2,
    OtherIncome = 3,

    // Expense
    MetaAds = 10,
    Taxes = 11,
    Tools = 12,
    OtherExpense = 13
}
