namespace RelationshipPlanner.Rebuild.Data;

public sealed class RebuildPlanSummary
{
    public int TotalItems { get; init; }

    public int PlannedItems { get; init; }

    public int InProgressItems { get; init; }

    public int DoneItems { get; init; }

    public DateTimeOffset? LastUpdated { get; init; }
}
