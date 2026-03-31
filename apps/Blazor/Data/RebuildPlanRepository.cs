using Microsoft.Data.Sqlite;

namespace RelationshipPlanner.Blazor.Data;

public sealed class RebuildPlanRepository
{
    private readonly string _connectionString;

    public RebuildPlanRepository(IWebHostEnvironment environment)
    {
        var dataDirectory = Path.Combine(environment.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dataDirectory);

        var databasePath = Path.Combine(dataDirectory, "rebuild-plan.db");
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath
        }.ToString();
    }

    public async Task InitializeAsync()
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = """
            create table if not exists rebuild_plan_items (
                id integer primary key autoincrement,
                title text not null,
                category text not null,
                status text not null,
                notes text not null,
                target_date text null,
                created_at text not null,
                updated_at text not null
            );
            """;

        await command.ExecuteNonQueryAsync();
    }

    public async Task<IReadOnlyList<RebuildPlanItem>> GetItemsAsync()
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = """
            select id, title, category, status, notes, target_date, created_at, updated_at
            from rebuild_plan_items
            order by datetime(created_at) desc, id desc;
            """;

        var items = new List<RebuildPlanItem>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            items.Add(ReadItem(reader));
        }

        return items;
    }

    public async Task<RebuildPlanSummary> GetSummaryAsync()
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = """
            select
                count(*) as total_items,
                sum(case when status = 'Planned' then 1 else 0 end) as planned_items,
                sum(case when status = 'In progress' then 1 else 0 end) as in_progress_items,
                sum(case when status = 'Done' then 1 else 0 end) as done_items,
                max(updated_at) as last_updated
            from rebuild_plan_items;
            """;

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return new RebuildPlanSummary();
        }

        return new RebuildPlanSummary
        {
            TotalItems = reader.GetInt32(0),
            PlannedItems = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
            InProgressItems = reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
            DoneItems = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
            LastUpdated = reader.IsDBNull(4)
                ? null
                : DateTimeOffset.Parse(reader.GetString(4))
        };
    }

    public async Task<RebuildPlanItem> AddItemAsync(RebuildPlanItem item)
    {
        var now = DateTimeOffset.UtcNow;

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = """
            insert into rebuild_plan_items (title, category, status, notes, target_date, created_at, updated_at)
            values ($title, $category, $status, $notes, $target_date, $created_at, $updated_at);
            select last_insert_rowid();
            """;
        command.Parameters.AddWithValue("$title", item.Title);
        command.Parameters.AddWithValue("$category", item.Category);
        command.Parameters.AddWithValue("$status", item.Status);
        command.Parameters.AddWithValue("$notes", item.Notes);
        command.Parameters.AddWithValue("$target_date", item.TargetDate.HasValue ? item.TargetDate.Value.ToString("yyyy-MM-dd") : DBNull.Value);
        command.Parameters.AddWithValue("$created_at", now.ToString("O"));
        command.Parameters.AddWithValue("$updated_at", now.ToString("O"));

        var insertedId = Convert.ToInt32(await command.ExecuteScalarAsync());
        item.Id = insertedId;
        item.CreatedAt = now;
        item.UpdatedAt = now;
        return item;
    }

    public async Task DeleteItemAsync(int id)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "delete from rebuild_plan_items where id = $id;";
        command.Parameters.AddWithValue("$id", id);
        await command.ExecuteNonQueryAsync();
    }

    private static RebuildPlanItem ReadItem(SqliteDataReader reader)
    {
        return new RebuildPlanItem
        {
            Id = reader.GetInt32(0),
            Title = reader.GetString(1),
            Category = reader.GetString(2),
            Status = reader.GetString(3),
            Notes = reader.GetString(4),
            TargetDate = reader.IsDBNull(5) ? null : DateOnly.Parse(reader.GetString(5)),
            CreatedAt = DateTimeOffset.Parse(reader.GetString(6)),
            UpdatedAt = DateTimeOffset.Parse(reader.GetString(7))
        };
    }
}

