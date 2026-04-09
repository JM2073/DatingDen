namespace RelationshipPlanner.Android.Data;

public sealed class PlannerHealthResponse
{
    public bool Ok { get; set; }
    public string? Service { get; set; }
}

public sealed class PlannerUser
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AccentColor { get; set; } = "#c3252d";
    public string? SteamId64 { get; set; }
    public bool IsDefault { get; set; }
}

public sealed class PlannerEntry
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public DateOnly? TargetDate { get; set; }
    public string Notes { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? GameStatus { get; set; }
}

public sealed class FinanceSummary
{
    public int ItemCount { get; set; }
    public decimal BudgetedIncome { get; set; }
    public decimal ActualIncome { get; set; }
    public decimal BudgetedOutflow { get; set; }
    public decimal ActualOutflow { get; set; }
    public decimal SavingsBudget { get; set; }
    public decimal CashBudget { get; set; }
    public decimal DebtTotalAmount { get; set; }
    public decimal InitialPaidAmount { get; set; }
    public decimal DebtPaidThisMonthAmount { get; set; }
    public decimal DebtRemainingAmount => Math.Max(0m, DebtTotalAmount - InitialPaidAmount - DebtPaidThisMonthAmount);
    public decimal NetBudget { get; set; }
    public decimal NetActual { get; set; }
}

public sealed class FinanceItem
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string MonthKey { get; set; } = string.Empty;
    public string Bucket { get; set; } = "income";
    public string Name { get; set; } = string.Empty;
    public decimal BudgetAmount { get; set; }
    public decimal ActualAmount { get; set; }
    public decimal DebtTotalAmount { get; set; }
    public decimal InitialPaidAmount { get; set; }
    public DateOnly? DueDate { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsShared { get; set; }
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public decimal DebtRemainingAmount => Math.Max(0m, DebtTotalAmount - InitialPaidAmount - ActualAmount);
}
