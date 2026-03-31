using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using System.Globalization;

namespace RelationshipPlanner.Rebuild.Data;

public sealed class SteamOpenIdService
{
    private const string SteamLoginEndpoint = "https://steamcommunity.com/openid/login";
    private const string SteamOpenIdPrefix = "https://steamcommunity.com/openid/id/";
    private readonly HttpClient _httpClient;

    public SteamOpenIdService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public string BuildLoginUrl(string returnTo, string realm)
    {
        var parameters = new Dictionary<string, string?>
        {
            ["openid.ns"] = "http://specs.openid.net/auth/2.0",
            ["openid.mode"] = "checkid_setup",
            ["openid.return_to"] = returnTo,
            ["openid.realm"] = realm,
            ["openid.identity"] = "http://specs.openid.net/auth/2.0/identifier_select",
            ["openid.claimed_id"] = "http://specs.openid.net/auth/2.0/identifier_select"
        };

        return QueryHelpers.AddQueryString(SteamLoginEndpoint, parameters);
    }

    public async Task<SteamOpenIdResult> VerifyCallbackAsync(
        IQueryCollection query,
        string expectedReturnTo,
        CancellationToken cancellationToken = default)
    {
        if (!query.TryGetValue("openid.mode", out var mode) || !string.Equals(mode, "id_res", StringComparison.OrdinalIgnoreCase))
        {
            return SteamOpenIdResult.Failure("Steam login was not completed.");
        }

        if (!query.TryGetValue("openid.op_endpoint", out var opEndpoint) || !string.Equals(opEndpoint, SteamLoginEndpoint, StringComparison.OrdinalIgnoreCase))
        {
            return SteamOpenIdResult.Failure("Steam login response was not recognized.");
        }

        if (query.TryGetValue("openid.return_to", out var returnTo) &&
            !string.Equals(returnTo, expectedReturnTo, StringComparison.OrdinalIgnoreCase))
        {
            return SteamOpenIdResult.Failure("Steam login return URL did not match this app.");
        }

        var validationForm = new Dictionary<string, string>();
        foreach (var pair in query)
        {
            if (pair.Key.StartsWith("openid.", StringComparison.OrdinalIgnoreCase))
            {
                validationForm[pair.Key] = pair.Value.ToString();
            }
        }

        validationForm["openid.mode"] = "check_authentication";

        using var response = await _httpClient.PostAsync(
            SteamLoginEndpoint,
            new FormUrlEncodedContent(validationForm),
            cancellationToken);

        var validationResponse = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode || validationResponse.IndexOf("is_valid:true", StringComparison.OrdinalIgnoreCase) < 0)
        {
            return SteamOpenIdResult.Failure("Steam could not validate the sign-in response.");
        }

        if (!query.TryGetValue("openid.claimed_id", out var claimedId))
        {
            return SteamOpenIdResult.Failure("Steam did not return a user id.");
        }

        var steamId64 = ExtractSteamId64(claimedId.ToString());
        if (steamId64 is null)
        {
            return SteamOpenIdResult.Failure("Steam returned an unexpected account id.");
        }

        return SteamOpenIdResult.Success(steamId64);
    }

    private static string? ExtractSteamId64(string claimedId)
    {
        if (string.IsNullOrWhiteSpace(claimedId) || !claimedId.StartsWith(SteamOpenIdPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var steamId = claimedId[SteamOpenIdPrefix.Length..].Trim('/');
        return ulong.TryParse(steamId, NumberStyles.None, CultureInfo.InvariantCulture, out _) ? steamId : null;
    }
}

public sealed record SteamOpenIdResult(bool IsSuccess, string? SteamId64, string Message)
{
    public static SteamOpenIdResult Success(string steamId64) => new(true, steamId64, "Steam login succeeded.");

    public static SteamOpenIdResult Failure(string message) => new(false, null, message);
}
