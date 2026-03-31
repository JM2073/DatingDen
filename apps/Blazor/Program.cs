using RelationshipPlanner.Blazor.Components;
using RelationshipPlanner.Blazor.Data;
using MudBlazor.Services;
using Microsoft.AspNetCore.WebUtilities;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddMudServices();
builder.Services.AddScoped<PlannerRepository>();
builder.Services.AddScoped<PlannerState>();
builder.Services.AddScoped<UserSessionService>();
builder.Services.AddHttpClient<GameIntegrationService>();
builder.Services.AddHttpClient<SteamOpenIdService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapGet("/auth/steam/start", (HttpContext context, int? userId, PlannerRepository repository, SteamOpenIdService steamOpenId) =>
{
    return StartSteamLogin(context, userId, repository, steamOpenId);
});
app.MapGet("/auth/steam/callback", async (HttpContext context, int? userId, PlannerRepository repository, SteamOpenIdService steamOpenId) =>
{
    return await CompleteSteamLogin(context, userId, repository, steamOpenId);
});
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await using (var scope = app.Services.CreateAsyncScope())
{
    var repository = scope.ServiceProvider.GetRequiredService<PlannerRepository>();
    await repository.InitializeAsync();
}

app.Run();

static async Task<IResult> StartSteamLogin(HttpContext context, int? userId, PlannerRepository repository, SteamOpenIdService steamOpenId)
{
    var settings = await repository.GetSettingsAsync();
    var targetUserId = userId ?? settings.ActiveUserId;
    if (targetUserId <= 0)
    {
        return Results.Redirect("/settings?steam=error&message=Pick+an+active+user+before+connecting+Steam.");
    }

    var returnTo = QueryHelpers.AddQueryString(
        $"{context.Request.Scheme}://{context.Request.Host}/auth/steam/callback",
        "userId",
        targetUserId.ToString(CultureInfo.InvariantCulture));

    var realm = $"{context.Request.Scheme}://{context.Request.Host}/";
    var loginUrl = steamOpenId.BuildLoginUrl(returnTo, realm);
    return Results.Redirect(loginUrl);
}

static async Task<IResult> CompleteSteamLogin(HttpContext context, int? userId, PlannerRepository repository, SteamOpenIdService steamOpenId)
{
    var settings = await repository.GetSettingsAsync();
    var targetUserId = userId ?? settings.ActiveUserId;
    if (targetUserId <= 0)
    {
        return Results.Redirect("/settings?steam=error&message=Pick+an+active+user+before+connecting+Steam.");
    }

    var expectedReturnTo = QueryHelpers.AddQueryString(
        $"{context.Request.Scheme}://{context.Request.Host}/auth/steam/callback",
        "userId",
        targetUserId.ToString(CultureInfo.InvariantCulture));

    var result = await steamOpenId.VerifyCallbackAsync(context.Request.Query, expectedReturnTo, context.RequestAborted);
    if (!result.IsSuccess || string.IsNullOrWhiteSpace(result.SteamId64))
    {
        return Results.Redirect(QueryHelpers.AddQueryString("/settings", new Dictionary<string, string?>
        {
            ["steam"] = "error",
            ["message"] = result.Message
        }));
    }

    var users = await repository.GetUsersAsync();
    var duplicate = users.FirstOrDefault(user =>
        user.Id != targetUserId &&
        !string.IsNullOrWhiteSpace(user.SteamId64) &&
        string.Equals(user.SteamId64, result.SteamId64, StringComparison.OrdinalIgnoreCase));

    if (duplicate is not null)
    {
        return Results.Redirect(QueryHelpers.AddQueryString("/settings", new Dictionary<string, string?>
        {
            ["steam"] = "error",
            ["message"] = $"That Steam account is already linked to {duplicate.Name}."
        }));
    }

    var user = await repository.GetUserAsync(targetUserId);
    if (user is null)
    {
        return Results.Redirect(QueryHelpers.AddQueryString("/settings", new Dictionary<string, string?>
        {
            ["steam"] = "error",
            ["message"] = "Could not find the selected user."
        }));
    }

    user.SteamId64 = result.SteamId64;
    await repository.SaveUserAsync(user);

    return Results.Redirect(QueryHelpers.AddQueryString("/settings", new Dictionary<string, string?>
    {
        ["steam"] = "connected",
        ["userId"] = targetUserId.ToString(CultureInfo.InvariantCulture)
    }));
}

