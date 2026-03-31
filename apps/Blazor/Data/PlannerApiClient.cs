using System.Net.Http.Json;

namespace RelationshipPlanner.Blazor.Data;

public sealed class PlannerApiClient
{
    private readonly HttpClient _httpClient;

    public PlannerApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<PlannerUser>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<List<PlannerUser>>("api/users", cancellationToken)
               ?? [];
    }

    public async Task<PlannerUser?> GetUserAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<PlannerUser>($"api/users/{id}", cancellationToken);
    }

    public async Task<PlannerUser> SaveUserAsync(PlannerUser user, CancellationToken cancellationToken = default)
    {
        using var response = user.Id <= 0
            ? await _httpClient.PostAsJsonAsync("api/users", user, cancellationToken)
            : await _httpClient.PutAsJsonAsync($"api/users/{user.Id}", user, cancellationToken);

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PlannerUser>(cancellationToken: cancellationToken)) ?? user;
    }

    public async Task DeleteUserAsync(int id, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.DeleteAsync($"api/users/{id}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<PlannerSettings> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<PlannerSettings>("api/settings", cancellationToken) ?? new PlannerSettings();
    }

    public async Task<PlannerSettings> SaveSettingsAsync(PlannerSettings settings, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PutAsJsonAsync("api/settings", settings, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PlannerSettings>(cancellationToken: cancellationToken)) ?? settings;
    }

    public async Task<IReadOnlyList<PlannerEntry>> GetEntriesAsync(CancellationToken cancellationToken = default)
    {
        return await GetEntriesAsync(null, cancellationToken);
    }

    public async Task<IReadOnlyList<PlannerEntry>> GetEntriesAsync(int? userId, CancellationToken cancellationToken = default)
    {
        var url = userId.HasValue ? $"api/entries?userId={userId.Value}" : "api/entries";
        return await _httpClient.GetFromJsonAsync<List<PlannerEntry>>(url, cancellationToken) ?? [];
    }

    public async Task<PlannerEntry?> GetEntryAsync(int id, CancellationToken cancellationToken = default)
    {
        var entries = await GetEntriesAsync(cancellationToken);
        return entries.FirstOrDefault(entry => entry.Id == id);
    }

    public async Task<PlannerEntry> SaveEntryAsync(PlannerEntry entry, CancellationToken cancellationToken = default)
    {
        using var response = entry.Id <= 0
            ? await _httpClient.PostAsJsonAsync("api/entries", entry, cancellationToken)
            : await _httpClient.PutAsJsonAsync($"api/entries/{entry.Id}", entry, cancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PlannerEntry>(cancellationToken: cancellationToken)) ?? entry;
    }

    public async Task DeleteEntryAsync(int id, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.DeleteAsync($"api/entries/{id}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<PlannerSummary> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<PlannerSummary>("api/summary", cancellationToken) ?? new PlannerSummary();
    }

    public async Task<IReadOnlyList<PlannerEntry>> GetGameEntriesAsync(int? userId = null, CancellationToken cancellationToken = default)
    {
        var entries = await GetEntriesAsync(userId, cancellationToken);
        return entries
            .Where(entry => entry.Kind is "game_plan" or "game_status")
            .OrderByDescending(entry => entry.UpdatedAt)
            .ThenByDescending(entry => entry.Id)
            .ToList();
    }

    public async Task<PlannerSummary> GetGameSummaryAsync(int? userId = null, CancellationToken cancellationToken = default)
    {
        var entries = await GetGameEntriesAsync(userId, cancellationToken);

        return new PlannerSummary
        {
            GameEntries = entries.Count,
            WantToPlayGames = entries.Count(entry => entry.GameStatus == "want_to_play"),
            PlayingGames = entries.Count(entry => entry.GameStatus == "playing"),
            PlayedGames = entries.Count(entry => entry.GameStatus == "played"),
            FinishedGames = entries.Count(entry => entry.GameStatus == "finished"),
            LastUpdated = entries.Count == 0 ? null : entries.Max(entry => entry.UpdatedAt)
        };
    }
}
