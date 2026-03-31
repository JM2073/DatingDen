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

app.MapGet("/api/finance/items", async (PlannerStore store, int userId, DateOnly month) =>
    Results.Ok(await store.GetFinanceItemsAsync(userId, month)));
app.MapGet("/api/finance/items/{id:int}", async (PlannerStore store, int id) =>
{
    var item = await store.GetFinanceItemAsync(id);
    return item is null ? Results.NotFound() : Results.Ok(item);
});
app.MapPost("/api/finance/items", async (PlannerStore store, FinanceItem item) =>
    Results.Ok(await store.SaveFinanceItemAsync(item)));
app.MapPut("/api/finance/items/{id:int}", async (PlannerStore store, int id, FinanceItem item) =>
{
    item.Id = id;
    return Results.Ok(await store.SaveFinanceItemAsync(item));
});
app.MapDelete("/api/finance/items/{id:int}", async (PlannerStore store, int id) =>
{
    await store.DeleteFinanceItemAsync(id);
    return Results.NoContent();
});

app.MapGet("/api/finance/summary", async (PlannerStore store, int userId, DateOnly month) =>
    Results.Ok(await store.GetFinanceSummaryAsync(userId, month)));

app.MapGet("/api/finance/sections", async (PlannerStore store, int userId) =>
    Results.Ok(await store.GetFinanceSectionsAsync(userId)));
app.MapGet("/api/finance/sections/{id:int}", async (PlannerStore store, int id) =>
{
    var section = await store.GetFinanceSectionAsync(id);
    return section is null ? Results.NotFound() : Results.Ok(section);
});
app.MapPost("/api/finance/sections", async (PlannerStore store, FinanceSection section) =>
    Results.Ok(await store.SaveFinanceSectionAsync(section)));
app.MapPut("/api/finance/sections/{id:int}", async (PlannerStore store, int id, FinanceSection section) =>
{
    section.Id = id;
    return Results.Ok(await store.SaveFinanceSectionAsync(section));
});
app.MapDelete("/api/finance/sections/{id:int}", async (PlannerStore store, int id) =>
{
    await store.DeleteFinanceSectionAsync(id);
    return Results.NoContent();
});
app.MapPost("/api/finance/sections/reassign", async (PlannerStore store, int userId, string fromSectionKey, string toSectionKey) =>
{
    await store.ReassignFinanceItemsAsync(userId, fromSectionKey, toSectionKey);
    return Results.NoContent();
});

app.MapGet("/api/finance/templates", async (PlannerStore store, int userId) =>
    Results.Ok(await store.GetFinanceTemplatesAsync(userId)));
app.MapGet("/api/finance/templates/{id:int}", async (PlannerStore store, int id) =>
{
    var template = await store.GetFinanceTemplateAsync(id);
    return template is null ? Results.NotFound() : Results.Ok(template);
});
app.MapPost("/api/finance/templates", async (PlannerStore store, FinanceTemplateItem template) =>
    Results.Ok(await store.SaveFinanceTemplateAsync(template)));
app.MapPut("/api/finance/templates/{id:int}", async (PlannerStore store, int id, FinanceTemplateItem template) =>
{
    template.Id = id;
    return Results.Ok(await store.SaveFinanceTemplateAsync(template));
});
app.MapDelete("/api/finance/templates/{id:int}", async (PlannerStore store, int id) =>
{
    await store.DeleteFinanceTemplateAsync(id);
    return Results.NoContent();
});

app.MapGet("/api/finance/imports/exists", async (PlannerStore store, int userId, DateOnly month, string source) =>
    Results.Ok(new { exists = await store.HasFinanceImportAsync(userId, month, source) }));
app.MapPost("/api/finance/imports", async (PlannerStore store, int userId, DateOnly month, string source) =>
{
    await store.RecordFinanceImportAsync(userId, month, source);
    return Results.NoContent();
});

app.Run();
