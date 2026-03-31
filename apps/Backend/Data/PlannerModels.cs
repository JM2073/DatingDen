namespace RelationshipPlanner.Api.Data;

public sealed class PlannerUser
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AccentColor { get; set; } = "#c3252d";
    public string? SteamId64 { get; set; }
    public bool IsDefault { get; set; }
}

public sealed class PlannerSettings
{
    public string CoupleName { get; set; } = "Us";
    public DateOnly? RelationshipStart { get; set; }
    public int DefaultReminderDays { get; set; } = 7;
    public string Theme { get; set; } = "system";
    public int ActiveUserId { get; set; } = 1;
    public string? RawgApiKey { get; set; }
    public string? SteamWebApiKey { get; set; }
    public string VisualTheme { get; set; } = "persona";
    public string DateIdeaColor { get; set; } = "#c3252d";
    public string ReminderColor { get; set; } = "#8d1117";
    public string MemoryColor { get; set; } = "#8f5c24";
    public string CountdownColor { get; set; } = "#6f3db8";
    public string GotTogetherColor { get; set; } = "#d14b75";
    public string WantToPlayColor { get; set; } = "#3b6ea8";
    public string PlayingColor { get; set; } = "#b56b16";
    public string PlayedColor { get; set; } = "#3f7a46";
    public string FinishedColor { get; set; } = "#6c4f95";
}

public sealed class PlannerEntry
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Kind { get; set; } = "date";
    public DateOnly? TargetDate { get; set; }
    public string Notes { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string Status { get; set; } = "planned";
    public string? GameTitle { get; set; }
    public string? GameCoverUrl { get; set; }
    public string? GameStatus { get; set; }
    public int? GameRating { get; set; }
    public int? GameRawgId { get; set; }
    public string? GameRawgSlug { get; set; }
    public string? GamePlatforms { get; set; }
    public int? GameSteamAppId { get; set; }
    public bool? SteamOwned { get; set; }
    public DateTimeOffset? SteamCheckedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class PlannerSummary
{
    public int TotalEntries { get; init; }
    public int DateIdeas { get; init; }
    public int Reminders { get; init; }
    public int Memories { get; init; }
    public int Countdowns { get; init; }
    public int GotTogetherItems { get; init; }
    public int GameEntries { get; init; }
    public int WantToPlayGames { get; init; }
    public int PlayingGames { get; init; }
    public int PlayedGames { get; init; }
    public int FinishedGames { get; init; }
    public DateTimeOffset? LastUpdated { get; init; }
}
