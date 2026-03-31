namespace RelationshipPlanner.Blazor.Data;

public sealed class RebuildPlanItem
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Category { get; set; } = "Planner";

    public string Status { get; set; } = "Planned";

    public string Notes { get; set; } = string.Empty;

    public DateOnly? TargetDate { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}

