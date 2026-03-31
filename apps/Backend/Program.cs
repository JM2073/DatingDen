using RelationshipPlanner.Api.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddPolicy("dev", policy =>
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod());
});
builder.Services.AddSingleton<PlannerStore>();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var store = scope.ServiceProvider.GetRequiredService<PlannerStore>();
    await store.InitializeAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("dev");

app.MapGet("/health", () => Results.Ok(new { ok = true, service = "RelationshipPlanner.Api" }));

app.MapGet("/api/users", async (PlannerStore store) => Results.Ok(await store.GetUsersAsync()));
app.MapGet("/api/users/{id:int}", async (PlannerStore store, int id) =>
{
    var user = await store.GetUserAsync(id);
    return user is null ? Results.NotFound() : Results.Ok(user);
});
app.MapPost("/api/users", async (PlannerStore store, PlannerUser user) => Results.Ok(await store.SaveUserAsync(user)));
app.MapPut("/api/users/{id:int}", async (PlannerStore store, int id, PlannerUser user) =>
{
    user.Id = id;
    return Results.Ok(await store.SaveUserAsync(user));
});
app.MapDelete("/api/users/{id:int}", async (PlannerStore store, int id) =>
{
    await store.DeleteUserAsync(id);
    return Results.NoContent();
});

app.MapGet("/api/settings", async (PlannerStore store) => Results.Ok(await store.GetSettingsAsync()));
app.MapPut("/api/settings", async (PlannerStore store, PlannerSettings settings) =>
    Results.Ok(await store.SaveSettingsAsync(settings)));

app.MapGet("/api/entries", async (PlannerStore store, int? userId) =>
    Results.Ok(await store.GetEntriesAsync(userId)));
app.MapPost("/api/entries", async (PlannerStore store, PlannerEntry entry) =>
    Results.Ok(await store.SaveEntryAsync(entry)));
app.MapPut("/api/entries/{id:int}", async (PlannerStore store, int id, PlannerEntry entry) =>
{
    entry.Id = id;
    return Results.Ok(await store.SaveEntryAsync(entry));
});
app.MapDelete("/api/entries/{id:int}", async (PlannerStore store, int id) =>
{
    await store.DeleteEntryAsync(id);
    return Results.NoContent();
});

app.MapGet("/api/summary", async (PlannerStore store) => Results.Ok(await store.GetSummaryAsync()));

app.Run();
