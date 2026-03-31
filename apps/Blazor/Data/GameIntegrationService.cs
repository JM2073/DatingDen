using System.Text.Json;
using System.Text.RegularExpressions;
using System.Net.Http.Json;

namespace RelationshipPlanner.Blazor.Data;

public sealed class GameIntegrationService
{
    private static readonly Regex SteamAppIdRegex = new(@"(?:store\.steampowered\.com|steamcommunity\.com).*/app/(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private readonly HttpClient _httpClient;
    private readonly PlannerApiClient _apiClient;

    public GameIntegrationService(HttpClient httpClient, PlannerApiClient apiClient)
    {
        _httpClient = httpClient;
        _apiClient = apiClient;
    }

    public async Task<IReadOnlyList<GameSearchResult>> SearchRawgGamesAsync(string query, CancellationToken cancellationToken = default)
    {
        var apiKey = (await _apiClient.GetSettingsAsync(cancellationToken)).RawgApiKey;
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<GameSearchResult>();
        }

        var url = $"https://api.rawg.io/api/games?search={Uri.EscapeDataString(query)}&page_size=8&search_precise=true&key={Uri.EscapeDataString(apiKey)}";
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<RawgGamesResponse>(cancellationToken: cancellationToken);
        var games = payload?.Results ?? [];
        var mapped = new List<GameSearchResult>(games.Count);
        foreach (var game in games)
        {
            mapped.Add(await MapGameAsync(game, cancellationToken));
        }

        return mapped;
    }

    public async Task<GameSearchResult?> GetRawgGameAsync(int rawgId, CancellationToken cancellationToken = default)
    {
        var apiKey = (await _apiClient.GetSettingsAsync(cancellationToken)).RawgApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return null;
        }

        var url = $"https://api.rawg.io/api/games/{rawgId}?key={Uri.EscapeDataString(apiKey)}";
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<RawgGameDto>(cancellationToken: cancellationToken);
        return payload is null ? null : await MapGameAsync(payload, cancellationToken);
    }

    public async Task<SteamOwnershipCheck> CheckSteamOwnershipAsync(string steamId64, int steamAppId, CancellationToken cancellationToken = default)
    {
        var ownedIds = await GetOwnedSteamAppIdsAsync(steamId64, cancellationToken);
        if (ownedIds.Message.Length > 0 && ownedIds.AppIds.Count == 0)
        {
            return new SteamOwnershipCheck
            {
                Owned = null,
                CheckedAt = DateTimeOffset.UtcNow,
                Message = ownedIds.Message
            };
        }

        var owns = ownedIds.AppIds.Contains(steamAppId);
        return new SteamOwnershipCheck
        {
            Owned = owns,
            CheckedAt = DateTimeOffset.UtcNow,
            Message = owns ? "In Steam library" : "Not in Steam library"
        };
    }

    public async Task<SteamOwnedGamesResult> GetOwnedSteamAppIdsAsync(string steamId64, CancellationToken cancellationToken = default)
    {
        var library = await GetOwnedSteamGamesAsync(steamId64, cancellationToken);
        return new SteamOwnedGamesResult
        {
            AppIds = new HashSet<int>(library.Games.Select(game => game.AppId)),
            Games = library.Games,
            GameCount = library.GameCount,
            Message = library.Message
        };
    }

    public async Task<SteamOwnedGamesResult> GetOwnedSteamGamesAsync(string steamId64, CancellationToken cancellationToken = default)
    {
        var apiKey = (await _apiClient.GetSettingsAsync(cancellationToken)).SteamWebApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new SteamOwnedGamesResult
            {
                Message = "Set the Steam web API key in Settings before checking ownership."
            };
        }

        if (string.IsNullOrWhiteSpace(steamId64))
        {
            return new SteamOwnedGamesResult
            {
                Message = "Add a Steam account to the current user first."
            };
        }

        var url = $"https://api.steampowered.com/IPlayerService/GetOwnedGames/v1/?key={Uri.EscapeDataString(apiKey)}&steamid={Uri.EscapeDataString(steamId64)}&include_appinfo=true&include_played_free_games=true";
        var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return new SteamOwnedGamesResult
            {
                Message = $"Steam ownership lookup failed with {(int)response.StatusCode}."
            };
        }

        var payload = await response.Content.ReadFromJsonAsync<SteamOwnedGamesResponse>(cancellationToken: cancellationToken);
        var games = (payload?.Response?.Games ?? [])
            .Select(game => new SteamOwnedGameSummary
            {
                AppId = game.AppId,
                Name = game.Name ?? $"App {game.AppId}",
                PlaytimeForever = game.PlaytimeForever,
                Playtime2Weeks = game.Playtime2Weeks,
                IconHash = game.IconHash ?? string.Empty
            })
            .OrderByDescending(game => game.PlaytimeForever)
            .ThenBy(game => game.Name)
            .ToList();

        var appIds = new HashSet<int>(games.Select(game => game.AppId));
        return new SteamOwnedGamesResult
        {
            AppIds = appIds,
            Games = games,
            GameCount = payload?.Response?.GameCount ?? games.Count,
            Message = games.Count > 0 ? $"Loaded {games.Count} Steam games." : "Steam library is empty or hidden."
        };
    }

    private async Task<GameSearchResult> MapGameAsync(RawgGameDto game, CancellationToken cancellationToken)
    {
        var steamAppId = TryGetSteamAppId(game);
        if (!steamAppId.HasValue && !string.IsNullOrWhiteSpace(game.Name))
        {
            steamAppId = await TryResolveSteamAppIdAsync(game.Name, game.Released, cancellationToken);
        }

        return new GameSearchResult
        {
            RawgId = game.Id,
            Name = game.Name ?? "Unknown game",
            Slug = game.Slug,
            Released = game.Released,
            CoverUrl = game.BackgroundImage,
            Platforms = string.Join(", ", game.Platforms?
                .Select(platform => platform.Platform?.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct()
                .Take(4) ?? []),
            SteamAppId = steamAppId
        };
    }

    private static int? TryGetSteamAppId(RawgGameDto game)
    {
        foreach (var store in game.Stores ?? [])
        {
            if (!string.IsNullOrWhiteSpace(store.Url))
            {
                var match = SteamAppIdRegex.Match(store.Url);
                if (match.Success && int.TryParse(match.Groups[1].Value, out var appId))
                {
                    return appId;
                }
            }
        }

        return null;
    }

    private async Task<int?> TryResolveSteamAppIdAsync(string gameName, string? released, CancellationToken cancellationToken)
    {
        var searchTerms = BuildSteamSearchTerms(gameName, released);
        foreach (var term in searchTerms)
        {
            var items = await SearchSteamStoreAsync(term, cancellationToken);
            var bestMatch = SelectBestSteamMatch(gameName, items);
            if (bestMatch.HasValue)
            {
                return bestMatch;
            }
        }

        return null;
    }

    private async Task<IReadOnlyList<SteamStoreSearchItem>> SearchSteamStoreAsync(string term, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return Array.Empty<SteamStoreSearchItem>();
        }

        var url = $"https://store.steampowered.com/api/storesearch/?term={Uri.EscapeDataString(term)}&l=english&cc=us";
        var response = await _httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return Array.Empty<SteamStoreSearchItem>();
        }

        var payload = await response.Content.ReadFromJsonAsync<SteamStoreSearchResponse>(cancellationToken: cancellationToken);
        return payload?.Items ?? [];
    }

    private static IEnumerable<string> BuildSteamSearchTerms(string gameName, string? released)
    {
        var terms = new List<string>();
        var normalized = NormalizeSearchName(gameName);
        if (!string.IsNullOrWhiteSpace(normalized))
        {
            terms.Add(normalized);
        }

        var baseName = normalized.Split(new[] { " - ", ":", "(", "[" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(baseName) && !terms.Contains(baseName, StringComparer.OrdinalIgnoreCase))
        {
            terms.Add(baseName);
        }

        if (!string.IsNullOrWhiteSpace(released) && DateTime.TryParse(released, out var date))
        {
            var yearTerm = $"{baseName ?? normalized} {date.Year}".Trim();
            if (!terms.Contains(yearTerm, StringComparer.OrdinalIgnoreCase))
            {
                terms.Add(yearTerm);
            }
        }

        return terms;
    }

    private static string NormalizeSearchName(string name)
    {
        return Regex.Replace(name, @"\s+", " ").Trim();
    }

    private static int? SelectBestSteamMatch(string gameName, IReadOnlyList<SteamStoreSearchItem> items)
    {
        if (items.Count == 0)
        {
            return null;
        }

        var normalizedGameName = NormalizeMatchName(gameName);
        var exact = items.FirstOrDefault(item => NormalizeMatchName(item.Name ?? string.Empty) == normalizedGameName);
        if (exact is not null)
        {
            return exact.Id;
        }

        var contains = items.FirstOrDefault(item =>
        {
            var candidate = NormalizeMatchName(item.Name ?? string.Empty);
            return candidate.Contains(normalizedGameName, StringComparison.OrdinalIgnoreCase) ||
                   normalizedGameName.Contains(candidate, StringComparison.OrdinalIgnoreCase);
        });

        return contains?.Id ?? items[0].Id;
    }

    private static string NormalizeMatchName(string name)
    {
        return Regex.Replace(name.ToLowerInvariant(), @"[^a-z0-9]+", " ").Trim();
    }
}

