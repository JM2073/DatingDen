using Microsoft.Data.Sqlite;

namespace RelationshipPlanner.Rebuild.Data;

public sealed class PlannerRepository
{
    private readonly string _connectionString;

    public PlannerRepository(IWebHostEnvironment environment)
    {
        var dataDirectory = Path.Combine(environment.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dataDirectory);

        var databasePath = Path.Combine(dataDirectory, "planner.db");
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
            create table if not exists planner_users (
                id integer primary key autoincrement,
                name text not null,
                accent_color text not null,
                steam_id64 text null,
                is_default integer not null default 0
            );

            create table if not exists planner_settings (
                id integer primary key check (id = 1),
                couple_name text not null,
                relationship_start text null,
                default_reminder_days integer not null,
                theme text not null,
                visual_theme text not null default 'persona',
                active_user_id integer not null,
                rawg_api_key text null,
                steam_web_api_key text null,
                date_idea_color text not null default '#c3252d',
                reminder_color text not null default '#8d1117',
                memory_color text not null default '#8f5c24',
                countdown_color text not null default '#6f3db8',
                got_together_color text not null default '#d14b75',
                want_to_play_color text not null default '#3b6ea8',
                playing_color text not null default '#b56b16',
                played_color text not null default '#3f7a46',
                finished_color text not null default '#6c4f95'
            );

            create table if not exists planner_entries (
                id integer primary key autoincrement,
                title text not null,
                kind text not null,
                target_date text null,
                notes text not null,
                user_id integer not null,
                status text not null,
                game_title text null,
                game_cover_url text null,
                game_status text null,
                game_rating integer null,
                game_rawg_id integer null,
                game_rawg_slug text null,
                game_platforms text null,
                game_steam_app_id integer null,
                steam_owned integer null,
                steam_checked_at text null,
                created_at text not null,
                updated_at text not null
            );

            create table if not exists planner_finance_items (
                id integer primary key autoincrement,
                user_id integer not null,
                month_key text not null,
                bucket text not null,
                name text not null,
                budget_amount real not null default 0,
                actual_amount real not null default 0,
                due_date text null,
                notes text not null default '',
                is_shared integer not null default 0,
                sort_order integer not null default 0,
                created_at text not null,
                updated_at text not null
            );

            create table if not exists planner_finance_sections (
                id integer primary key autoincrement,
                user_id integer not null,
                section_key text not null,
                name text not null,
                color text not null,
                sort_order integer not null default 0,
                is_builtin integer not null default 0
            );

            create table if not exists planner_finance_imports (
                id integer primary key autoincrement,
                user_id integer not null,
                month_key text not null,
                source text not null,
                imported_at text not null
            );

            create table if not exists planner_finance_templates (
                id integer primary key autoincrement,
                user_id integer not null,
                section_key text not null,
                name text not null,
                budget_amount real not null default 0,
                actual_amount real not null default 0,
                notes text not null default '',
                is_shared integer not null default 0,
                sort_order integer not null default 0
            );
            """;

        await command.ExecuteNonQueryAsync();
        await EnsureColumnAsync(connection, "planner_users", "steam_id64", "text null");
        await EnsureColumnAsync(connection, "planner_entries", "game_title", "text null");
        await EnsureColumnAsync(connection, "planner_entries", "game_cover_url", "text null");
        await EnsureColumnAsync(connection, "planner_entries", "game_status", "text null");
        await EnsureColumnAsync(connection, "planner_entries", "game_rating", "integer null");
        await EnsureColumnAsync(connection, "planner_entries", "game_rawg_id", "integer null");
        await EnsureColumnAsync(connection, "planner_entries", "game_rawg_slug", "text null");
        await EnsureColumnAsync(connection, "planner_entries", "game_platforms", "text null");
        await EnsureColumnAsync(connection, "planner_entries", "game_steam_app_id", "integer null");
        await EnsureColumnAsync(connection, "planner_entries", "steam_owned", "integer null");
        await EnsureColumnAsync(connection, "planner_entries", "steam_checked_at", "text null");

        command = connection.CreateCommand();
        command.CommandText = "select count(*) from planner_users;";
        var userCount = Convert.ToInt32(await command.ExecuteScalarAsync());
        if (userCount == 0)
        {
            await InsertDefaultUserAsync(connection);
        }

        command = connection.CreateCommand();
        command.CommandText = "select count(*) from planner_settings;";
        var settingsCount = Convert.ToInt32(await command.ExecuteScalarAsync());
        if (settingsCount == 0)
        {
            await InsertDefaultSettingsAsync(connection);
        }

        await EnsureColumnAsync(connection, "planner_settings", "rawg_api_key", "text null");
        await EnsureColumnAsync(connection, "planner_settings", "steam_web_api_key", "text null");
        await EnsureColumnAsync(connection, "planner_settings", "visual_theme", "text not null default 'persona'");
        await EnsureColumnAsync(connection, "planner_settings", "date_idea_color", "text not null default '#c3252d'");
        await EnsureColumnAsync(connection, "planner_settings", "reminder_color", "text not null default '#8d1117'");
        await EnsureColumnAsync(connection, "planner_settings", "memory_color", "text not null default '#8f5c24'");
        await EnsureColumnAsync(connection, "planner_settings", "countdown_color", "text not null default '#6f3db8'");
        await EnsureColumnAsync(connection, "planner_settings", "got_together_color", "text not null default '#d14b75'");
        await EnsureColumnAsync(connection, "planner_settings", "want_to_play_color", "text not null default '#3b6ea8'");
        await EnsureColumnAsync(connection, "planner_settings", "playing_color", "text not null default '#b56b16'");
        await EnsureColumnAsync(connection, "planner_settings", "played_color", "text not null default '#3f7a46'");
        await EnsureColumnAsync(connection, "planner_settings", "finished_color", "text not null default '#6c4f95'");

        await EnsureColumnAsync(connection, "planner_finance_items", "month_key", "text not null default '0001-01-01'");
        await EnsureColumnAsync(connection, "planner_finance_items", "bucket", "text not null default 'income'");
        await EnsureColumnAsync(connection, "planner_finance_items", "name", "text not null default ''");
        await EnsureColumnAsync(connection, "planner_finance_items", "budget_amount", "real not null default 0");
        await EnsureColumnAsync(connection, "planner_finance_items", "actual_amount", "real not null default 0");
        await EnsureColumnAsync(connection, "planner_finance_items", "due_date", "text null");
        await EnsureColumnAsync(connection, "planner_finance_items", "notes", "text not null default ''");
        await EnsureColumnAsync(connection, "planner_finance_items", "is_shared", "integer not null default 0");
        await EnsureColumnAsync(connection, "planner_finance_items", "sort_order", "integer not null default 0");
        await EnsureColumnAsync(connection, "planner_finance_items", "created_at", "text not null default ''");
        await EnsureColumnAsync(connection, "planner_finance_items", "updated_at", "text not null default ''");

        await EnsureColumnAsync(connection, "planner_finance_sections", "section_key", "text not null default ''");
        await EnsureColumnAsync(connection, "planner_finance_sections", "name", "text not null default ''");
        await EnsureColumnAsync(connection, "planner_finance_sections", "color", "text not null default '#c3252d'");
        await EnsureColumnAsync(connection, "planner_finance_sections", "sort_order", "integer not null default 0");
        await EnsureColumnAsync(connection, "planner_finance_sections", "is_builtin", "integer not null default 0");

        await EnsureColumnAsync(connection, "planner_finance_templates", "section_key", "text not null default 'fixed'");
        await EnsureColumnAsync(connection, "planner_finance_templates", "name", "text not null default ''");
        await EnsureColumnAsync(connection, "planner_finance_templates", "budget_amount", "real not null default 0");
        await EnsureColumnAsync(connection, "planner_finance_templates", "actual_amount", "real not null default 0");
        await EnsureColumnAsync(connection, "planner_finance_templates", "notes", "text not null default ''");
        await EnsureColumnAsync(connection, "planner_finance_templates", "is_shared", "integer not null default 0");
        await EnsureColumnAsync(connection, "planner_finance_templates", "sort_order", "integer not null default 0");

        await EnsureDefaultFinanceSectionsAsync(connection);
    }

    public async Task<IReadOnlyList<PlannerUser>> GetUsersAsync()
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = """
            select id, name, accent_color, steam_id64, is_default
            from planner_users
            order by is_default desc, name asc;
            """;

        var users = new List<PlannerUser>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            users.Add(new PlannerUser
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                AccentColor = reader.GetString(2),
                SteamId64 = reader.IsDBNull(3) ? null : reader.GetString(3),
                IsDefault = reader.GetInt32(4) == 1
            });
        }

        return users;
    }

    public async Task<PlannerUser?> GetUserAsync(int id)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = """
            select id, name, accent_color, steam_id64, is_default
            from planner_users
            where id = $id;
            """;
        command.Parameters.AddWithValue("$id", id);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new PlannerUser
        {
            Id = reader.GetInt32(0),
            Name = reader.GetString(1),
            AccentColor = reader.GetString(2),
            SteamId64 = reader.IsDBNull(3) ? null : reader.GetString(3),
            IsDefault = reader.GetInt32(4) == 1
        };
    }

    public async Task<PlannerUser> SaveUserAsync(PlannerUser user)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        if (user.IsDefault)
        {
            var clearDefaults = connection.CreateCommand();
            clearDefaults.CommandText = "update planner_users set is_default = 0;";
            await clearDefaults.ExecuteNonQueryAsync();
        }

        var command = connection.CreateCommand();
        if (user.Id == 0)
        {
            command.CommandText = """
                insert into planner_users (name, accent_color, steam_id64, is_default)
                values ($name, $accent_color, $steam_id64, $is_default);
                select last_insert_rowid();
                """;
        }
        else
        {
            command.CommandText = """
                update planner_users
                set name = $name,
                    accent_color = $accent_color,
                    steam_id64 = $steam_id64,
                    is_default = $is_default
                where id = $id;
                select $id;
                """;
            command.Parameters.AddWithValue("$id", user.Id);
        }

        command.Parameters.AddWithValue("$name", user.Name);
        command.Parameters.AddWithValue("$accent_color", user.AccentColor);
        command.Parameters.AddWithValue("$steam_id64", string.IsNullOrWhiteSpace(user.SteamId64) ? DBNull.Value : user.SteamId64);
        command.Parameters.AddWithValue("$is_default", user.IsDefault ? 1 : 0);

        user.Id = Convert.ToInt32(await command.ExecuteScalarAsync());
        return user;
    }

    public async Task DeleteUserAsync(int id)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var countCommand = connection.CreateCommand();
        countCommand.CommandText = "select count(*) from planner_users;";
        var userCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync());
        if (userCount <= 1)
        {
            return;
        }

        var deleteCommand = connection.CreateCommand();
        deleteCommand.CommandText = "delete from planner_users where id = $id;";
        deleteCommand.Parameters.AddWithValue("$id", id);
        await deleteCommand.ExecuteNonQueryAsync();

        var settings = await GetSettingsAsync();
        if (settings.ActiveUserId == id)
        {
            var nextDefault = await GetDefaultUserAsync(connection);
            settings.ActiveUserId = nextDefault?.Id ?? 1;
            await SaveSettingsAsync(settings);
        }
    }

    public async Task<PlannerSettings> GetSettingsAsync()
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = """
            select couple_name, relationship_start, default_reminder_days, theme, visual_theme, active_user_id, rawg_api_key, steam_web_api_key,
                   date_idea_color, reminder_color, memory_color, countdown_color, got_together_color, want_to_play_color, playing_color, played_color, finished_color
            from planner_settings
            where id = 1;
            """;

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return new PlannerSettings();
        }

        return new PlannerSettings
        {
            CoupleName = reader.GetString(0),
            RelationshipStart = reader.IsDBNull(1) ? null : DateOnly.Parse(reader.GetString(1)),
            DefaultReminderDays = reader.GetInt32(2),
            Theme = reader.GetString(3),
            VisualTheme = GetStringOrDefault(reader, 4, "persona"),
            ActiveUserId = reader.GetInt32(5),
            RawgApiKey = reader.IsDBNull(6) ? null : reader.GetString(6),
            SteamWebApiKey = reader.IsDBNull(7) ? null : reader.GetString(7),
            DateIdeaColor = GetStringOrDefault(reader, 8, "#c3252d"),
            ReminderColor = GetStringOrDefault(reader, 9, "#8d1117"),
            MemoryColor = GetStringOrDefault(reader, 10, "#8f5c24"),
            CountdownColor = GetStringOrDefault(reader, 11, "#6f3db8"),
            GotTogetherColor = GetStringOrDefault(reader, 12, "#d14b75"),
            WantToPlayColor = GetStringOrDefault(reader, 13, "#3b6ea8"),
            PlayingColor = GetStringOrDefault(reader, 14, "#b56b16"),
            PlayedColor = GetStringOrDefault(reader, 15, "#3f7a46"),
            FinishedColor = GetStringOrDefault(reader, 16, "#6c4f95")
        };
    }

    public async Task SaveSettingsAsync(PlannerSettings settings)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = """
            insert into planner_settings (id, couple_name, relationship_start, default_reminder_days, theme, visual_theme, active_user_id, rawg_api_key, steam_web_api_key, date_idea_color, reminder_color, memory_color, countdown_color, got_together_color, want_to_play_color, playing_color, played_color, finished_color)
            values (1, $couple_name, $relationship_start, $default_reminder_days, $theme, $visual_theme, $active_user_id, $rawg_api_key, $steam_web_api_key, $date_idea_color, $reminder_color, $memory_color, $countdown_color, $got_together_color, $want_to_play_color, $playing_color, $played_color, $finished_color)
            on conflict(id) do update set
                couple_name = excluded.couple_name,
                relationship_start = excluded.relationship_start,
                default_reminder_days = excluded.default_reminder_days,
                theme = excluded.theme,
                visual_theme = excluded.visual_theme,
                active_user_id = excluded.active_user_id,
                rawg_api_key = excluded.rawg_api_key,
                steam_web_api_key = excluded.steam_web_api_key,
                date_idea_color = excluded.date_idea_color,
                reminder_color = excluded.reminder_color,
                memory_color = excluded.memory_color,
                countdown_color = excluded.countdown_color,
                got_together_color = excluded.got_together_color,
                want_to_play_color = excluded.want_to_play_color,
                playing_color = excluded.playing_color,
                played_color = excluded.played_color,
                finished_color = excluded.finished_color;
            """;
        command.Parameters.AddWithValue("$couple_name", settings.CoupleName);
        command.Parameters.AddWithValue("$relationship_start", settings.RelationshipStart.HasValue ? settings.RelationshipStart.Value.ToString("yyyy-MM-dd") : DBNull.Value);
        command.Parameters.AddWithValue("$default_reminder_days", settings.DefaultReminderDays);
        command.Parameters.AddWithValue("$theme", settings.Theme);
        command.Parameters.AddWithValue("$visual_theme", settings.VisualTheme);
        command.Parameters.AddWithValue("$active_user_id", settings.ActiveUserId);
        command.Parameters.AddWithValue("$rawg_api_key", string.IsNullOrWhiteSpace(settings.RawgApiKey) ? DBNull.Value : settings.RawgApiKey);
        command.Parameters.AddWithValue("$steam_web_api_key", string.IsNullOrWhiteSpace(settings.SteamWebApiKey) ? DBNull.Value : settings.SteamWebApiKey);
        command.Parameters.AddWithValue("$date_idea_color", settings.DateIdeaColor);
        command.Parameters.AddWithValue("$reminder_color", settings.ReminderColor);
        command.Parameters.AddWithValue("$memory_color", settings.MemoryColor);
        command.Parameters.AddWithValue("$countdown_color", settings.CountdownColor);
        command.Parameters.AddWithValue("$got_together_color", settings.GotTogetherColor);
        command.Parameters.AddWithValue("$want_to_play_color", settings.WantToPlayColor);
        command.Parameters.AddWithValue("$playing_color", settings.PlayingColor);
        command.Parameters.AddWithValue("$played_color", settings.PlayedColor);
        command.Parameters.AddWithValue("$finished_color", settings.FinishedColor);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<IReadOnlyList<PlannerEntry>> GetEntriesAsync()
    {
        return await GetEntriesAsync(null);
    }

    public async Task<IReadOnlyList<PlannerEntry>> GetEntriesAsync(int? userId)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = """
            select id, title, kind, target_date, notes, user_id, status,
                   game_title, game_cover_url, game_status, game_rating, game_rawg_id, game_rawg_slug, game_platforms, game_steam_app_id, steam_owned, steam_checked_at,
                   created_at, updated_at
            from planner_entries
            where ($user_id is null or user_id = $user_id)
            order by coalesce(target_date, created_at) desc, id desc;
            """;
        command.Parameters.AddWithValue("$user_id", userId.HasValue ? userId.Value : DBNull.Value);

        var entries = new List<PlannerEntry>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            entries.Add(ReadEntry(reader));
        }

        return entries;
    }

    public async Task<PlannerEntry?> GetEntryAsync(int id)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = """
            select id, title, kind, target_date, notes, user_id, status,
                   game_title, game_cover_url, game_status, game_rating, game_rawg_id, game_rawg_slug, game_platforms, game_steam_app_id, steam_owned, steam_checked_at,
                   created_at, updated_at
            from planner_entries
            where id = $id;
            """;
        command.Parameters.AddWithValue("$id", id);

        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? ReadEntry(reader) : null;
    }

    public async Task<PlannerEntry> SaveEntryAsync(PlannerEntry entry)
    {
        var now = DateTimeOffset.UtcNow;

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        if (entry.Id != 0)
        {
            var existing = await GetEntryAsync(entry.Id);
            if (existing is not null)
            {
                entry.GameTitle ??= existing.GameTitle;
                entry.GameCoverUrl ??= existing.GameCoverUrl;
                entry.GameStatus ??= existing.GameStatus;
                entry.GameRating ??= existing.GameRating;
                entry.GameRawgId ??= existing.GameRawgId;
                entry.GameRawgSlug ??= existing.GameRawgSlug;
                entry.GamePlatforms ??= existing.GamePlatforms;
                entry.GameSteamAppId ??= existing.GameSteamAppId;
                entry.SteamOwned ??= existing.SteamOwned;
                entry.SteamCheckedAt ??= existing.SteamCheckedAt;
            }
        }

        var command = connection.CreateCommand();
        if (entry.Id == 0)
        {
            command.CommandText = """
                insert into planner_entries (title, kind, target_date, notes, user_id, status, game_title, game_cover_url, game_status, game_rating, game_rawg_id, game_rawg_slug, game_platforms, game_steam_app_id, steam_owned, steam_checked_at, created_at, updated_at)
                values ($title, $kind, $target_date, $notes, $user_id, $status, $game_title, $game_cover_url, $game_status, $game_rating, $game_rawg_id, $game_rawg_slug, $game_platforms, $game_steam_app_id, $steam_owned, $steam_checked_at, $created_at, $updated_at);
                select last_insert_rowid();
                """;
            command.Parameters.AddWithValue("$created_at", now.ToString("O"));
        }
        else
        {
            command.CommandText = """
                update planner_entries
                set title = $title,
                    kind = $kind,
                    target_date = $target_date,
                    notes = $notes,
                    user_id = $user_id,
                    status = $status,
                    game_title = $game_title,
                    game_cover_url = $game_cover_url,
                    game_status = $game_status,
                    game_rating = $game_rating,
                    game_rawg_id = $game_rawg_id,
                    game_rawg_slug = $game_rawg_slug,
                    game_platforms = $game_platforms,
                    game_steam_app_id = $game_steam_app_id,
                    steam_owned = $steam_owned,
                    steam_checked_at = $steam_checked_at,
                    updated_at = $updated_at
                where id = $id;
                select $id;
                """;
            command.Parameters.AddWithValue("$id", entry.Id);
            command.Parameters.AddWithValue("$created_at", now.ToString("O"));
        }

        command.Parameters.AddWithValue("$title", entry.Title);
        command.Parameters.AddWithValue("$kind", entry.Kind);
        command.Parameters.AddWithValue("$target_date", entry.TargetDate.HasValue ? entry.TargetDate.Value.ToString("yyyy-MM-dd") : DBNull.Value);
        command.Parameters.AddWithValue("$notes", entry.Notes);
        command.Parameters.AddWithValue("$user_id", entry.UserId);
        command.Parameters.AddWithValue("$status", entry.Status);
        command.Parameters.AddWithValue("$game_title", entry.GameTitle ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$game_cover_url", entry.GameCoverUrl ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$game_status", entry.GameStatus ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$game_rating", entry.GameRating.HasValue ? entry.GameRating.Value : DBNull.Value);
        command.Parameters.AddWithValue("$game_rawg_id", entry.GameRawgId.HasValue ? entry.GameRawgId.Value : DBNull.Value);
        command.Parameters.AddWithValue("$game_rawg_slug", entry.GameRawgSlug ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$game_platforms", entry.GamePlatforms ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$game_steam_app_id", entry.GameSteamAppId.HasValue ? entry.GameSteamAppId.Value : DBNull.Value);
        command.Parameters.AddWithValue("$steam_owned", entry.SteamOwned.HasValue ? (entry.SteamOwned.Value ? 1 : 0) : DBNull.Value);
        command.Parameters.AddWithValue("$steam_checked_at", entry.SteamCheckedAt.HasValue ? entry.SteamCheckedAt.Value.ToString("O") : DBNull.Value);
        command.Parameters.AddWithValue("$updated_at", now.ToString("O"));

        var result = Convert.ToInt32(await command.ExecuteScalarAsync());
        entry.Id = result;
        entry.CreatedAt = entry.Id == 0 ? now : entry.CreatedAt == default ? now : entry.CreatedAt;
        entry.UpdatedAt = now;
        return entry;
    }

    public async Task DeleteEntryAsync(int id)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "delete from planner_entries where id = $id;";
        command.Parameters.AddWithValue("$id", id);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<PlannerSummary> GetSummaryAsync()
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = """
            select
                count(*) as total_entries,
                sum(case when kind = 'date' then 1 else 0 end) as date_ideas,
                sum(case when kind = 'reminder' then 1 else 0 end) as reminders,
                sum(case when kind = 'memory' then 1 else 0 end) as memories,
                sum(case when kind = 'countdown' then 1 else 0 end) as countdowns,
                sum(case when kind = 'got_together' then 1 else 0 end) as got_together_items,
                sum(case when kind in ('game_plan', 'game_status') then 1 else 0 end) as game_entries,
                sum(case when game_status = 'want_to_play' then 1 else 0 end) as want_to_play_games,
                sum(case when game_status = 'playing' then 1 else 0 end) as playing_games,
                sum(case when game_status = 'played' then 1 else 0 end) as played_games,
                sum(case when game_status = 'finished' then 1 else 0 end) as finished_games,
                max(updated_at) as last_updated
            from planner_entries;
            """;

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return new PlannerSummary();
        }

        return new PlannerSummary
        {
            TotalEntries = reader.GetInt32(0),
            DateIdeas = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
            Reminders = reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
            Memories = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
            Countdowns = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
            GotTogetherItems = reader.IsDBNull(5) ? 0 : reader.GetInt32(5),
            GameEntries = reader.IsDBNull(6) ? 0 : reader.GetInt32(6),
            WantToPlayGames = reader.IsDBNull(7) ? 0 : reader.GetInt32(7),
            PlayingGames = reader.IsDBNull(8) ? 0 : reader.GetInt32(8),
            PlayedGames = reader.IsDBNull(9) ? 0 : reader.GetInt32(9),
            FinishedGames = reader.IsDBNull(10) ? 0 : reader.GetInt32(10),
            LastUpdated = reader.IsDBNull(11) ? null : DateTimeOffset.Parse(reader.GetString(11))
        };
    }

    public async Task<IReadOnlyList<PlannerEntry>> GetGameEntriesAsync(int? userId = null)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = """
            select id, title, kind, target_date, notes, user_id, status,
                   game_title, game_cover_url, game_status, game_rating, game_rawg_id, game_rawg_slug, game_platforms, game_steam_app_id, steam_owned, steam_checked_at,
                   created_at, updated_at
            from planner_entries
            where kind in ('game_plan', 'game_status')
              and ($user_id is null or user_id = $user_id)
            order by datetime(updated_at) desc, id desc;
            """;
        command.Parameters.AddWithValue("$user_id", userId.HasValue ? userId.Value : DBNull.Value);

        var items = new List<PlannerEntry>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            items.Add(ReadEntry(reader));
        }

        return items;
    }

    public async Task<PlannerSummary> GetGameSummaryAsync(int? userId = null)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = """
            select
                count(*) as game_entries,
                sum(case when game_status = 'want_to_play' then 1 else 0 end) as want_to_play_games,
                sum(case when game_status = 'playing' then 1 else 0 end) as playing_games,
                sum(case when game_status = 'played' then 1 else 0 end) as played_games,
                sum(case when game_status = 'finished' then 1 else 0 end) as finished_games,
                max(updated_at) as last_updated
            from planner_entries
            where kind in ('game_plan', 'game_status')
              and ($user_id is null or user_id = $user_id);
            """;
        command.Parameters.AddWithValue("$user_id", userId.HasValue ? userId.Value : DBNull.Value);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return new PlannerSummary();
        }

        return new PlannerSummary
        {
            GameEntries = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
            WantToPlayGames = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
            PlayingGames = reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
            PlayedGames = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
            FinishedGames = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
            LastUpdated = reader.IsDBNull(5) ? null : DateTimeOffset.Parse(reader.GetString(5))
        };
    }

    public async Task<IReadOnlyList<FinanceItem>> GetFinanceItemsAsync(int userId, DateOnly month)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = """
            select id, user_id, month_key, bucket, name, budget_amount, actual_amount, due_date, notes, is_shared, sort_order, created_at, updated_at
            from planner_finance_items
            where user_id = $user_id and month_key = $month_key
            order by sort_order asc, bucket asc, name asc, id asc;
            """;
        command.Parameters.AddWithValue("$user_id", userId);
        command.Parameters.AddWithValue("$month_key", GetMonthKey(month));

        var items = new List<FinanceItem>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            items.Add(ReadFinanceItem(reader));
        }

        return items;
    }

    public async Task<FinanceItem?> GetFinanceItemAsync(int id)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = """
            select id, user_id, month_key, bucket, name, budget_amount, actual_amount, due_date, notes, is_shared, sort_order, created_at, updated_at
            from planner_finance_items
            where id = $id;
            """;
        command.Parameters.AddWithValue("$id", id);

        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? ReadFinanceItem(reader) : null;
    }

    public async Task<FinanceItem> SaveFinanceItemAsync(FinanceItem item)
    {
        var now = DateTimeOffset.UtcNow;

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        if (item.Id != 0)
        {
            var existing = await GetFinanceItemAsync(item.Id);
            if (existing is not null)
            {
                item.CreatedAt = existing.CreatedAt;
            }
        }

        var command = connection.CreateCommand();
        if (item.Id == 0)
        {
            command.CommandText = """
                insert into planner_finance_items (user_id, month_key, bucket, name, budget_amount, actual_amount, due_date, notes, is_shared, sort_order, created_at, updated_at)
                values ($user_id, $month_key, $bucket, $name, $budget_amount, $actual_amount, $due_date, $notes, $is_shared, $sort_order, $created_at, $updated_at);
                select last_insert_rowid();
                """;
            command.Parameters.AddWithValue("$created_at", now.ToString("O"));
        }
        else
        {
            command.CommandText = """
                update planner_finance_items
                set user_id = $user_id,
                    month_key = $month_key,
                    bucket = $bucket,
                    name = $name,
                    budget_amount = $budget_amount,
                    actual_amount = $actual_amount,
                    due_date = $due_date,
                    notes = $notes,
                    is_shared = $is_shared,
                    sort_order = $sort_order,
                    updated_at = $updated_at
                where id = $id;
                select $id;
                """;
            command.Parameters.AddWithValue("$id", item.Id);
            command.Parameters.AddWithValue("$created_at", item.CreatedAt == default ? now.ToString("O") : item.CreatedAt.ToString("O"));
        }

        command.Parameters.AddWithValue("$user_id", item.UserId);
        command.Parameters.AddWithValue("$month_key", string.IsNullOrWhiteSpace(item.MonthKey) ? GetMonthKey(DateOnly.FromDateTime(DateTime.Today)) : item.MonthKey);
        command.Parameters.AddWithValue("$bucket", string.IsNullOrWhiteSpace(item.Bucket) ? "income" : item.Bucket);
        command.Parameters.AddWithValue("$name", item.Name);
        command.Parameters.AddWithValue("$budget_amount", item.BudgetAmount);
        command.Parameters.AddWithValue("$actual_amount", item.ActualAmount);
        command.Parameters.AddWithValue("$due_date", item.DueDate.HasValue ? item.DueDate.Value.ToString("yyyy-MM-dd") : DBNull.Value);
        command.Parameters.AddWithValue("$notes", item.Notes);
        command.Parameters.AddWithValue("$is_shared", item.IsShared ? 1 : 0);
        command.Parameters.AddWithValue("$sort_order", item.SortOrder);
        command.Parameters.AddWithValue("$updated_at", now.ToString("O"));

        item.Id = Convert.ToInt32(await command.ExecuteScalarAsync());
        if (item.CreatedAt == default)
        {
            item.CreatedAt = now;
        }

        item.UpdatedAt = now;
        item.MonthKey = string.IsNullOrWhiteSpace(item.MonthKey) ? GetMonthKey(DateOnly.FromDateTime(DateTime.Today)) : item.MonthKey;
        return item;
    }

    public async Task DeleteFinanceItemAsync(int id)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "delete from planner_finance_items where id = $id;";
        command.Parameters.AddWithValue("$id", id);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<FinanceSummary> GetFinanceSummaryAsync(int userId, DateOnly month)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = """
            select
                count(*) as item_count,
                sum(case when bucket = 'income' then budget_amount else 0 end) as budgeted_income,
                sum(case when bucket = 'income' then actual_amount else 0 end) as actual_income,
                sum(case when bucket <> 'income' then budget_amount else 0 end) as budgeted_outflow,
                sum(case when bucket <> 'income' then actual_amount else 0 end) as actual_outflow,
                sum(case when bucket = 'savings' then budget_amount else 0 end) as savings_budget,
                sum(case when bucket = 'cash' then budget_amount else 0 end) as cash_budget,
                max(updated_at) as last_updated
            from planner_finance_items
            where user_id = $user_id and month_key = $month_key;
            """;
        command.Parameters.AddWithValue("$user_id", userId);
        command.Parameters.AddWithValue("$month_key", GetMonthKey(month));

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return new FinanceSummary();
        }

        return new FinanceSummary
        {
            ItemCount = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
            BudgetedIncome = GetDecimalOrZero(reader, 1),
            ActualIncome = GetDecimalOrZero(reader, 2),
            BudgetedOutflow = GetDecimalOrZero(reader, 3),
            ActualOutflow = GetDecimalOrZero(reader, 4),
            SavingsBudget = GetDecimalOrZero(reader, 5),
            CashBudget = GetDecimalOrZero(reader, 6),
            LastUpdated = reader.IsDBNull(7) ? null : DateTimeOffset.Parse(reader.GetString(7))
        };
    }

    public async Task<IReadOnlyList<FinanceSection>> GetFinanceSectionsAsync(int userId)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        await EnsureDefaultFinanceSectionsForUserAsync(connection, userId);

        var command = connection.CreateCommand();
        command.CommandText = """
            select id, user_id, section_key, name, color, sort_order, is_builtin
            from planner_finance_sections
            where user_id = $user_id
            order by is_builtin desc, sort_order asc, name asc;
            """;
        command.Parameters.AddWithValue("$user_id", userId);

        var sections = new List<FinanceSection>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            sections.Add(new FinanceSection
            {
                Id = reader.GetInt32(0),
                UserId = reader.GetInt32(1),
                SectionKey = reader.GetString(2),
                Name = reader.GetString(3),
                Color = reader.GetString(4),
                SortOrder = reader.GetInt32(5),
                IsBuiltin = reader.GetInt32(6) == 1
            });
        }

        return sections;
    }

    public async Task<FinanceSection?> GetFinanceSectionAsync(int id)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = """
            select id, user_id, section_key, name, color, sort_order, is_builtin
            from planner_finance_sections
            where id = $id;
            """;
        command.Parameters.AddWithValue("$id", id);

        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? new FinanceSection
        {
            Id = reader.GetInt32(0),
            UserId = reader.GetInt32(1),
            SectionKey = reader.GetString(2),
            Name = reader.GetString(3),
            Color = reader.GetString(4),
            SortOrder = reader.GetInt32(5),
            IsBuiltin = reader.GetInt32(6) == 1
        } : null;
    }

    public async Task<FinanceSection> SaveFinanceSectionAsync(FinanceSection section)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        if (section.Id != 0)
        {
            var existing = await GetFinanceSectionAsync(section.Id);
            if (existing is not null)
            {
                section.IsBuiltin = existing.IsBuiltin;
                section.SectionKey = existing.SectionKey;
            }
        }

        var command = connection.CreateCommand();
        if (section.Id == 0)
        {
            command.CommandText = """
                insert into planner_finance_sections (user_id, section_key, name, color, sort_order, is_builtin)
                values ($user_id, $section_key, $name, $color, $sort_order, $is_builtin);
                select last_insert_rowid();
                """;
        }
        else
        {
            command.CommandText = """
                update planner_finance_sections
                set user_id = $user_id,
                    section_key = $section_key,
                    name = $name,
                    color = $color,
                    sort_order = $sort_order,
                    is_builtin = $is_builtin
                where id = $id;
                select $id;
                """;
            command.Parameters.AddWithValue("$id", section.Id);
        }

        command.Parameters.AddWithValue("$user_id", section.UserId);
        command.Parameters.AddWithValue("$section_key", section.SectionKey);
        command.Parameters.AddWithValue("$name", section.Name);
        command.Parameters.AddWithValue("$color", section.Color);
        command.Parameters.AddWithValue("$sort_order", section.SortOrder);
        command.Parameters.AddWithValue("$is_builtin", section.IsBuiltin ? 1 : 0);

        section.Id = Convert.ToInt32(await command.ExecuteScalarAsync());
        return section;
    }

    public async Task DeleteFinanceSectionAsync(int id)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "delete from planner_finance_sections where id = $id and is_builtin = 0;";
        command.Parameters.AddWithValue("$id", id);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<IReadOnlyList<FinanceTemplateItem>> GetFinanceTemplatesAsync(int userId)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        await EnsureDefaultFinanceTemplatesForUserAsync(connection, userId);

        var command = connection.CreateCommand();
        command.CommandText = """
            select id, user_id, section_key, name, budget_amount, actual_amount, notes, is_shared, sort_order
            from planner_finance_templates
            where user_id = $user_id
            order by sort_order asc, section_key asc, name asc, id asc;
            """;
        command.Parameters.AddWithValue("$user_id", userId);

        var templates = new List<FinanceTemplateItem>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            templates.Add(new FinanceTemplateItem
            {
                Id = reader.GetInt32(0),
                UserId = reader.GetInt32(1),
                SectionKey = reader.GetString(2),
                Name = reader.GetString(3),
                BudgetAmount = GetDecimalOrZero(reader, 4),
                ActualAmount = GetDecimalOrZero(reader, 5),
                Notes = reader.GetString(6),
                IsShared = reader.GetInt32(7) == 1,
                SortOrder = reader.GetInt32(8)
            });
        }

        return templates;
    }

    public async Task<FinanceTemplateItem?> GetFinanceTemplateAsync(int id)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = """
            select id, user_id, section_key, name, budget_amount, actual_amount, notes, is_shared, sort_order
            from planner_finance_templates
            where id = $id;
            """;
        command.Parameters.AddWithValue("$id", id);

        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? new FinanceTemplateItem
        {
            Id = reader.GetInt32(0),
            UserId = reader.GetInt32(1),
            SectionKey = reader.GetString(2),
            Name = reader.GetString(3),
            BudgetAmount = GetDecimalOrZero(reader, 4),
            ActualAmount = GetDecimalOrZero(reader, 5),
            Notes = reader.GetString(6),
            IsShared = reader.GetInt32(7) == 1,
            SortOrder = reader.GetInt32(8)
        } : null;
    }

    public async Task<FinanceTemplateItem> SaveFinanceTemplateAsync(FinanceTemplateItem template)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        if (template.Id == 0)
        {
            command.CommandText = """
                insert into planner_finance_templates (user_id, section_key, name, budget_amount, actual_amount, notes, is_shared, sort_order)
                values ($user_id, $section_key, $name, $budget_amount, $actual_amount, $notes, $is_shared, $sort_order);
                select last_insert_rowid();
                """;
        }
        else
        {
            command.CommandText = """
                update planner_finance_templates
                set user_id = $user_id,
                    section_key = $section_key,
                    name = $name,
                    budget_amount = $budget_amount,
                    actual_amount = $actual_amount,
                    notes = $notes,
                    is_shared = $is_shared,
                    sort_order = $sort_order
                where id = $id;
                select $id;
                """;
            command.Parameters.AddWithValue("$id", template.Id);
        }

        command.Parameters.AddWithValue("$user_id", template.UserId);
        command.Parameters.AddWithValue("$section_key", template.SectionKey);
        command.Parameters.AddWithValue("$name", template.Name);
        command.Parameters.AddWithValue("$budget_amount", template.BudgetAmount);
        command.Parameters.AddWithValue("$actual_amount", template.ActualAmount);
        command.Parameters.AddWithValue("$notes", template.Notes);
        command.Parameters.AddWithValue("$is_shared", template.IsShared ? 1 : 0);
        command.Parameters.AddWithValue("$sort_order", template.SortOrder);

        template.Id = Convert.ToInt32(await command.ExecuteScalarAsync());
        return template;
    }

    public async Task DeleteFinanceTemplateAsync(int id)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = "delete from planner_finance_templates where id = $id;";
        command.Parameters.AddWithValue("$id", id);
        await command.ExecuteNonQueryAsync();
    }

    public async Task ReassignFinanceItemsAsync(int userId, string fromSectionKey, string toSectionKey)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = """
            update planner_finance_items
            set bucket = $to_section_key,
                updated_at = $updated_at
            where user_id = $user_id and bucket = $from_section_key;
            """;
        command.Parameters.AddWithValue("$user_id", userId);
        command.Parameters.AddWithValue("$from_section_key", fromSectionKey);
        command.Parameters.AddWithValue("$to_section_key", toSectionKey);
        command.Parameters.AddWithValue("$updated_at", DateTimeOffset.UtcNow.ToString("O"));
        await command.ExecuteNonQueryAsync();
    }

    public async Task<bool> HasFinanceImportAsync(int userId, DateOnly month, string source)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = """
            select count(*)
            from planner_finance_imports
            where user_id = $user_id and month_key = $month_key and source = $source;
            """;
        command.Parameters.AddWithValue("$user_id", userId);
        command.Parameters.AddWithValue("$month_key", GetMonthKey(month));
        command.Parameters.AddWithValue("$source", source);

        var count = Convert.ToInt32(await command.ExecuteScalarAsync());
        return count > 0;
    }

    public async Task RecordFinanceImportAsync(int userId, DateOnly month, string source)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = """
            insert into planner_finance_imports (user_id, month_key, source, imported_at)
            values ($user_id, $month_key, $source, $imported_at);
            """;
        command.Parameters.AddWithValue("$user_id", userId);
        command.Parameters.AddWithValue("$month_key", GetMonthKey(month));
        command.Parameters.AddWithValue("$source", source);
        command.Parameters.AddWithValue("$imported_at", DateTimeOffset.UtcNow.ToString("O"));
        await command.ExecuteNonQueryAsync();
    }

    private static async Task EnsureDefaultFinanceSectionsAsync(SqliteConnection connection)
    {
        var command = connection.CreateCommand();
        command.CommandText = "select count(*) from planner_finance_sections;";
        var count = Convert.ToInt32(await command.ExecuteScalarAsync());
        if (count > 0)
        {
            return;
        }

        var insert = connection.CreateCommand();
        insert.CommandText = """
            insert into planner_finance_sections (user_id, section_key, name, color, sort_order, is_builtin)
            values
                (1, 'income', 'Income', '#3f7a46', 0, 1),
                (1, 'fixed', 'Fixed costs', '#c3252d', 1, 1),
                (1, 'flexible', 'Flexible spending', '#8f5c24', 2, 1),
                (1, 'savings', 'Savings', '#6f3db8', 3, 1),
                (1, 'cash', 'Cash buffer', '#d14b75', 4, 1);
        """;
        await insert.ExecuteNonQueryAsync();
    }

    private static async Task EnsureDefaultFinanceSectionsForUserAsync(SqliteConnection connection, int userId)
    {
        var command = connection.CreateCommand();
        command.CommandText = """
            select count(*)
            from planner_finance_sections
            where user_id = $user_id;
            """;
        command.Parameters.AddWithValue("$user_id", userId);
        var count = Convert.ToInt32(await command.ExecuteScalarAsync());
        if (count > 0)
        {
            return;
        }

        var insert = connection.CreateCommand();
        insert.CommandText = """
            insert into planner_finance_sections (user_id, section_key, name, color, sort_order, is_builtin)
            values
                ($user_id, 'income', 'Income', '#3f7a46', 0, 1),
                ($user_id, 'fixed', 'Fixed costs', '#c3252d', 1, 1),
                ($user_id, 'flexible', 'Flexible spending', '#8f5c24', 2, 1),
                ($user_id, 'savings', 'Savings', '#6f3db8', 3, 1),
                ($user_id, 'cash', 'Cash buffer', '#d14b75', 4, 1);
            """;
        insert.Parameters.AddWithValue("$user_id", userId);
        await insert.ExecuteNonQueryAsync();
    }

    private static async Task EnsureDefaultFinanceTemplatesForUserAsync(SqliteConnection connection, int userId)
    {
        var command = connection.CreateCommand();
        command.CommandText = """
            select count(*)
            from planner_finance_templates
            where user_id = $user_id;
            """;
        command.Parameters.AddWithValue("$user_id", userId);
        var count = Convert.ToInt32(await command.ExecuteScalarAsync());
        if (count > 0)
        {
            return;
        }

        var insert = connection.CreateCommand();
        insert.CommandText = """
            insert into planner_finance_templates (user_id, section_key, name, budget_amount, actual_amount, notes, is_shared, sort_order)
            values
                ($user_id, 'income', 'Income', 1786, 0, 'Monthly income from the sheet', 1, 0),
                ($user_id, 'fixed', 'Rent', 400, 0, '', 1, 1),
                ($user_id, 'fixed', 'Travel', 160, 0, '', 1, 2),
                ($user_id, 'fixed', 'Lunch', 90, 0, '', 1, 3),
                ($user_id, 'fixed', 'Loan', 211.98, 0, '', 1, 4),
                ($user_id, 'fixed', 'Argos', 250, 0, '', 1, 5),
                ($user_id, 'fixed', 'Credit N', 50, 0, '', 1, 6),
                ($user_id, 'fixed', 'Credit M', 100, 0, '', 1, 7),
                ($user_id, 'fixed', 'PlayStation Plus', 6.99, 0, '', 1, 8),
                ($user_id, 'fixed', 'Monzo Max', 17, 0, '', 1, 9),
                ($user_id, 'fixed', 'Humble Bundle', 11.49, 0, '', 1, 10),
                ($user_id, 'flexible', 'Jo', 190, 0, '', 0, 11),
                ($user_id, 'flexible', 'Luna (visit)', 60, 0, '', 0, 12),
                ($user_id, 'flexible', 'Mum', 200, 0, '', 0, 13),
                ($user_id, 'savings', 'Samsung watch', 558, 0, 'Savings goal', 0, 14),
                ($user_id, 'savings', 'Samsung ring', 399, 0, 'Savings goal', 0, 15),
                ($user_id, 'savings', 'Samsung tablet kit', 1386.01, 0, 'Savings goal', 0, 16);
            """;
        insert.Parameters.AddWithValue("$user_id", userId);
        await insert.ExecuteNonQueryAsync();
    }

    private static async Task InsertDefaultUserAsync(SqliteConnection connection)
    {
        var command = connection.CreateCommand();
        command.CommandText = """
            insert into planner_users (name, accent_color, steam_id64, is_default)
            values ('You', '#c3252d', null, 1);
            """;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task InsertDefaultSettingsAsync(SqliteConnection connection)
    {
        var command = connection.CreateCommand();
        command.CommandText = """
            insert into planner_settings (id, couple_name, relationship_start, default_reminder_days, theme, visual_theme, active_user_id, rawg_api_key, steam_web_api_key, date_idea_color, reminder_color, memory_color, countdown_color, got_together_color, want_to_play_color, playing_color, played_color, finished_color)
            values (1, 'Us', null, 7, 'system', 'persona', 1, null, null, '#c3252d', '#8d1117', '#8f5c24', '#6f3db8', '#d14b75', '#3b6ea8', '#b56b16', '#3f7a46', '#6c4f95');
            """;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task EnsureColumnAsync(SqliteConnection connection, string table, string column, string definition)
    {
        var checkCommand = connection.CreateCommand();
        checkCommand.CommandText = $"pragma table_info({table});";

        var exists = false;
        await using (var reader = await checkCommand.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                if (string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase))
                {
                    exists = true;
                    break;
                }
            }
        }

        if (!exists)
        {
            var alterCommand = connection.CreateCommand();
            alterCommand.CommandText = $"alter table {table} add column {column} {definition};";
            await alterCommand.ExecuteNonQueryAsync();
        }
    }

    private static string GetStringOrDefault(SqliteDataReader reader, int index, string fallback)
    {
        return reader.IsDBNull(index) ? fallback : reader.GetString(index);
    }

    private static string GetMonthKey(DateOnly month) => new DateOnly(month.Year, month.Month, 1).ToString("yyyy-MM-dd");

    private static decimal GetDecimalOrZero(SqliteDataReader reader, int index)
    {
        if (reader.IsDBNull(index))
        {
            return 0m;
        }

        return reader.GetDecimal(index);
    }

    private static async Task<PlannerUser?> GetDefaultUserAsync(SqliteConnection connection)
    {
        var command = connection.CreateCommand();
        command.CommandText = """
            select id, name, accent_color, steam_id64, is_default
            from planner_users
            order by is_default desc, id asc
            limit 1;
            """;

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new PlannerUser
        {
            Id = reader.GetInt32(0),
            Name = reader.GetString(1),
            AccentColor = reader.GetString(2),
            SteamId64 = reader.IsDBNull(3) ? null : reader.GetString(3),
            IsDefault = reader.GetInt32(4) == 1
        };
    }

    private static PlannerEntry ReadEntry(SqliteDataReader reader)
    {
        return new PlannerEntry
        {
            Id = reader.GetInt32(0),
            Title = reader.GetString(1),
            Kind = reader.GetString(2),
            TargetDate = reader.IsDBNull(3) ? null : DateOnly.Parse(reader.GetString(3)),
            Notes = reader.GetString(4),
            UserId = reader.GetInt32(5),
            Status = reader.GetString(6),
            GameTitle = reader.IsDBNull(7) ? null : reader.GetString(7),
            GameCoverUrl = reader.IsDBNull(8) ? null : reader.GetString(8),
            GameStatus = reader.IsDBNull(9) ? null : reader.GetString(9),
            GameRating = reader.IsDBNull(10) ? null : reader.GetInt32(10),
            GameRawgId = reader.IsDBNull(11) ? null : reader.GetInt32(11),
            GameRawgSlug = reader.IsDBNull(12) ? null : reader.GetString(12),
            GamePlatforms = reader.IsDBNull(13) ? null : reader.GetString(13),
            GameSteamAppId = reader.IsDBNull(14) ? null : reader.GetInt32(14),
            SteamOwned = reader.IsDBNull(15) ? null : reader.GetInt32(15) == 1,
            SteamCheckedAt = reader.IsDBNull(16) ? null : DateTimeOffset.Parse(reader.GetString(16)),
            CreatedAt = DateTimeOffset.Parse(reader.GetString(17)),
            UpdatedAt = DateTimeOffset.Parse(reader.GetString(18))
        };
    }

    private static FinanceItem ReadFinanceItem(SqliteDataReader reader)
    {
        return new FinanceItem
        {
            Id = reader.GetInt32(0),
            UserId = reader.GetInt32(1),
            MonthKey = reader.GetString(2),
            Bucket = reader.GetString(3),
            Name = reader.GetString(4),
            BudgetAmount = GetDecimalOrZero(reader, 5),
            ActualAmount = GetDecimalOrZero(reader, 6),
            DueDate = reader.IsDBNull(7) ? null : DateOnly.Parse(reader.GetString(7)),
            Notes = reader.GetString(8),
            IsShared = reader.GetInt32(9) == 1,
            SortOrder = reader.GetInt32(10),
            CreatedAt = DateTimeOffset.Parse(reader.GetString(11)),
            UpdatedAt = DateTimeOffset.Parse(reader.GetString(12))
        };
    }
}
