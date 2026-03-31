namespace RelationshipPlanner.Blazor.Data;

public sealed class PlannerState
{
    public int? SelectedUserId { get; set; }

    public DateOnly SelectedDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public int? EditingEntryId { get; set; }

    public string ThemeMode { get; set; } = "system";

    public event Action? Changed;

    public void NotifyChanged() => Changed?.Invoke();
}

