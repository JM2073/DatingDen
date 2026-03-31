using System.Text.Json.Serialization;

namespace RelationshipPlanner.Rebuild.Data;

public sealed record GameSearchResult
{
    public int RawgId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Slug { get; init; }
    public string? Released { get; init; }
    public string? CoverUrl { get; init; }
    public string? Platforms { get; init; }
    public int? SteamAppId { get; init; }
}

public sealed record SteamOwnershipCheck
{
    public bool? Owned { get; init; }
    public string Message { get; init; } = string.Empty;
    public DateTimeOffset CheckedAt { get; init; }
}

public sealed record SteamOwnedGamesResult
{
    public IReadOnlySet<int> AppIds { get; init; } = new HashSet<int>();
    public IReadOnlyList<SteamOwnedGameSummary> Games { get; init; } = [];
    public int GameCount { get; init; }
    public string Message { get; init; } = string.Empty;
}

public sealed record SteamOwnedGameSummary
{
    public int AppId { get; init; }
    public string Name { get; init; } = string.Empty;
    public int PlaytimeForever { get; init; }
    public int Playtime2Weeks { get; init; }
    public string IconHash { get; init; } = string.Empty;
    public string HeaderImageUrl => $"https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/{AppId}/header.jpg";
    public string IconUrl => string.IsNullOrWhiteSpace(IconHash)
        ? string.Empty
        : $"https://steamcdn-a.akamaihd.net/steamcommunity/public/images/apps/{AppId}/{IconHash}.jpg";
}

internal sealed class RawgGamesResponse
{
    [JsonPropertyName("results")]
    public List<RawgGameDto>? Results { get; set; }
}

internal sealed class RawgGameDto
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("slug")]
    public string? Slug { get; set; }

    [JsonPropertyName("released")]
    public string? Released { get; set; }

    [JsonPropertyName("background_image")]
    public string? BackgroundImage { get; set; }

    [JsonPropertyName("platforms")]
    public List<RawgPlatformLink>? Platforms { get; set; }

    [JsonPropertyName("stores")]
    public List<RawgStoreLink>? Stores { get; set; }
}

internal sealed class RawgPlatformLink
{
    [JsonPropertyName("platform")]
    public RawgPlatformDto? Platform { get; set; }
}

internal sealed class RawgPlatformDto
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

internal sealed class RawgStoreLink
{
    [JsonPropertyName("store")]
    public RawgStoreDto? Store { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }
}

internal sealed class RawgStoreDto
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

internal sealed class SteamStoreSearchResponse
{
    [JsonPropertyName("items")]
    public List<SteamStoreSearchItem>? Items { get; set; }
}

internal sealed class SteamStoreSearchItem
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

internal sealed class SteamOwnedGamesResponse
{
    [JsonPropertyName("response")]
    public SteamOwnedGamesPayload? Response { get; set; }
}

internal sealed class SteamOwnedGamesPayload
{
    [JsonPropertyName("game_count")]
    public int GameCount { get; set; }

    [JsonPropertyName("games")]
    public List<SteamOwnedGameDto>? Games { get; set; }
}

internal sealed class SteamOwnedGameDto
{
    [JsonPropertyName("appid")]
    public int AppId { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("playtime_forever")]
    public int PlaytimeForever { get; set; }

    [JsonPropertyName("playtime_2weeks")]
    public int Playtime2Weeks { get; set; }

    [JsonPropertyName("img_icon_url")]
    public string? IconHash { get; set; }
}
