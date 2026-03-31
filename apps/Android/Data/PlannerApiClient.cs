using System.Net.Http.Json;

namespace RelationshipPlanner.Android.Data;

public sealed class PlannerApiClient
{
    private readonly HttpClient _httpClient;

    public PlannerApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<PlannerHealthResponse?> GetHealthAsync(CancellationToken cancellationToken = default)
    {
        return _httpClient.GetFromJsonAsync<PlannerHealthResponse>("health", cancellationToken);
    }

    public async Task<IReadOnlyList<PlannerUser>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        return await _httpClient.GetFromJsonAsync<List<PlannerUser>>("api/users", cancellationToken) ?? [];
    }

    public async Task<IReadOnlyList<PlannerEntry>> GetEntriesAsync(int? userId = null, CancellationToken cancellationToken = default)
    {
        var url = userId.HasValue ? $"api/entries?userId={userId.Value}" : "api/entries";
        return await _httpClient.GetFromJsonAsync<List<PlannerEntry>>(url, cancellationToken) ?? [];
    }

    public async Task<FinanceSummary> GetFinanceSummaryAsync(int userId, DateOnly month, CancellationToken cancellationToken = default)
    {
        var url = $"api/finance/summary?userId={userId}&month={month:yyyy-MM-dd}";
        return await _httpClient.GetFromJsonAsync<FinanceSummary>(url, cancellationToken) ?? new FinanceSummary();
    }
}
