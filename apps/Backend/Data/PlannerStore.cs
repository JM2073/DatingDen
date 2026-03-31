using System.Globalization;
using Microsoft.Data.SqlClient;

namespace RelationshipPlanner.Api.Data;

public sealed class PlannerStore
{
    private readonly string _connectionString;
    private readonly string _masterConnectionString;
    private readonly string _databaseName;

    public PlannerStore(IConfiguration configuration)
        : this(GetConnectionString(configuration))
    {
    }

    public PlannerStore(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(builder.InitialCatalog))
        {
            throw new InvalidOperationException("The PlannerDatabase connection string must include an Initial Catalog.");
        }

        builder.MultipleActiveResultSets = true;

        _connectionString = builder.ConnectionString;
        _databaseName = builder.InitialCatalog;

        var masterBuilder = new SqlConnectionStringBuilder(builder.ConnectionString)
        {
            InitialCatalog = "master"
        };

        _masterConnectionString = masterBuilder.ConnectionString;
    }

    public async Task InitializeAsync()
    {
        await EnsureDatabaseExistsAsync();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                if object_id(N'dbo.planner_users', N'U') is null
                begin
                    create table dbo.planner_users (
                        id int identity(1,1) not null constraint pk_planner_users primary key,
                        name nvarchar(200) not null,
                        accent_color nvarchar(20) not null,
                        steam_id64 nvarchar(32) null,
                        is_default bit not null constraint df_planner_users_is_default default 0
                    );
                end;

                if object_id(N'dbo.planner_settings', N'U') is null
                begin
                    create table dbo.planner_settings (
                        id int not null constraint pk_planner_settings primary key constraint ck_planner_settings_id check (id = 1),
                        couple_name nvarchar(200) not null,
                        relationship_start date null,
                        default_reminder_days int not null,
                        theme nvarchar(40) not null,
                        visual_theme nvarchar(40) not null default 'persona',
                        active_user_id int not null,
                        rawg_api_key nvarchar(200) null,
                        steam_web_api_key nvarchar(200) null,
                        date_idea_color nvarchar(20) not null default '#c3252d',
                        reminder_color nvarchar(20) not null default '#8d1117',
                        memory_color nvarchar(20) not null default '#8f5c24',
                        countdown_color nvarchar(20) not null default '#6f3db8',
                        got_together_color nvarchar(20) not null default '#d14b75',
                        want_to_play_color nvarchar(20) not null default '#3b6ea8',
                        playing_color nvarchar(20) not null default '#b56b16',
                        played_color nvarchar(20) not null default '#3f7a46',
                        finished_color nvarchar(20) not null default '#6c4f95'
                    );
                end;

                if object_id(N'dbo.planner_entries', N'U') is null
                begin
                    create table dbo.planner_entries (
                        id int identity(1,1) not null constraint pk_planner_entries primary key,
                        title nvarchar(300) not null,
                        kind nvarchar(40) not null,
                        target_date date null,
                        notes nvarchar(max) not null,
                        user_id int not null,
                        status nvarchar(40) not null,
                        game_title nvarchar(300) null,
                        game_cover_url nvarchar(1000) null,
                        game_status nvarchar(40) null,
                        game_rating int null,
                        game_rawg_id int null,
                        game_rawg_slug nvarchar(200) null,
                        game_platforms nvarchar(500) null,
                        game_steam_app_id int null,
                        steam_owned bit null,
                        steam_checked_at datetimeoffset(7) null,
                        created_at datetimeoffset(7) not null,
                        updated_at datetimeoffset(7) not null
                    );
                end;

                if object_id(N'dbo.planner_finance_items', N'U') is null
                begin
                    create table dbo.planner_finance_items (
                        id int identity(1,1) not null constraint pk_planner_finance_items primary key,
                        user_id int not null,
                        month_key nvarchar(16) not null,
                        bucket nvarchar(100) not null,
                        name nvarchar(200) not null,
                        budget_amount decimal(18,2) not null constraint df_planner_finance_items_budget default 0,
                        actual_amount decimal(18,2) not null constraint df_planner_finance_items_actual default 0,
                        due_date date null,
                        notes nvarchar(max) not null constraint df_planner_finance_items_notes default '',
                        is_shared bit not null constraint df_planner_finance_items_shared default 0,
                        sort_order int not null constraint df_planner_finance_items_sort default 0,
                        created_at datetimeoffset(7) not null,
                        updated_at datetimeoffset(7) not null
                    );
                end;

                if object_id(N'dbo.planner_finance_sections', N'U') is null
                begin
                    create table dbo.planner_finance_sections (
                        id int identity(1,1) not null constraint pk_planner_finance_sections primary key,
                        user_id int not null,
                        section_key nvarchar(100) not null,
                        name nvarchar(200) not null,
                        color nvarchar(20) not null,
                        sort_order int not null constraint df_planner_finance_sections_sort default 0,
                        is_builtin bit not null constraint df_planner_finance_sections_builtin default 0
                    );
                end;

                if object_id(N'dbo.planner_finance_imports', N'U') is null
                begin
                    create table dbo.planner_finance_imports (
                        id int identity(1,1) not null constraint pk_planner_finance_imports primary key,
                        user_id int not null,
                        month_key nvarchar(16) not null,
                        source nvarchar(100) not null,
                        imported_at datetimeoffset(7) not null
                    );
                end;

                if object_id(N'dbo.planner_finance_templates', N'U') is null
                begin
                    create table dbo.planner_finance_templates (
                        id int identity(1,1) not null constraint pk_planner_finance_templates primary key,
                        user_id int not null,
                        section_key nvarchar(100) not null,
                        name nvarchar(200) not null,
                        budget_amount decimal(18,2) not null constraint df_planner_finance_templates_budget default 0,
                        actual_amount decimal(18,2) not null constraint df_planner_finance_templates_actual default 0,
                        notes nvarchar(max) not null constraint df_planner_finance_templates_notes default '',
                        is_shared bit not null constraint df_planner_finance_templates_shared default 0,
                        sort_order int not null constraint df_planner_finance_templates_sort default 0
                    );
                end;
                """;
            await command.ExecuteNonQueryAsync();
        }

        if (await CountAsync(connection, "dbo.planner_users") == 0)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                insert into dbo.planner_users (name, accent_color, steam_id64, is_default)
                values (N'You', N'#c3252d', null, 1);
                """;
            await command.ExecuteNonQueryAsync();
        }

        if (await CountAsync(connection, "dbo.planner_settings") == 0)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                insert into dbo.planner_settings (
                    id, couple_name, relationship_start, default_reminder_days, theme, visual_theme, active_user_id,
                    rawg_api_key, steam_web_api_key, date_idea_color, reminder_color, memory_color, countdown_color,
                    got_together_color, want_to_play_color, playing_color, played_color, finished_color
                ) values (
                    1, N'Us', null, 7, N'system', N'persona', 1,
                    null, null, N'#c3252d', N'#8d1117', N'#8f5c24', N'#6f3db8',
                    N'#d14b75', N'#3b6ea8', N'#b56b16', N'#3f7a46', N'#6c4f95'
                );
                """;
            await command.ExecuteNonQueryAsync();
        }
    }

    public async Task<IReadOnlyList<PlannerUser>> GetUsersAsync()
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            select id, name, accent_color, steam_id64, is_default
            from dbo.planner_users
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
                IsDefault = reader.GetBoolean(4)
            });
        }

        return users;
    }

    public async Task<PlannerUser> SaveUserAsync(PlannerUser user)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        if (user.IsDefault)
        {
            await using var clear = connection.CreateCommand();
            clear.CommandText = "update dbo.planner_users set is_default = 0 where id <> @id;";
            clear.Parameters.AddWithValue("@id", user.Id);
            await clear.ExecuteNonQueryAsync();
        }

        if (user.Id <= 0)
        {
            await using var insert = connection.CreateCommand();
            insert.CommandText = """
                insert into dbo.planner_users (name, accent_color, steam_id64, is_default)
                output inserted.id
                values (@name, @accent_color, @steam_id64, @is_default);
                """;
            BindUser(insert, user);
            user.Id = Convert.ToInt32(await insert.ExecuteScalarAsync());
            return user;
        }

        await using var update = connection.CreateCommand();
        update.CommandText = """
            update dbo.planner_users
            set name = @name,
                accent_color = @accent_color,
                steam_id64 = @steam_id64,
                is_default = @is_default
            where id = @id;
            """;
        update.Parameters.AddWithValue("@id", user.Id);
        BindUser(update, user);
        await update.ExecuteNonQueryAsync();
        return user;
    }

    public async Task<PlannerSettings> GetSettingsAsync()
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            select couple_name, relationship_start, default_reminder_days, theme, active_user_id,
                   rawg_api_key, steam_web_api_key, visual_theme,
                   date_idea_color, reminder_color, memory_color, countdown_color, got_together_color,
                   want_to_play_color, playing_color, played_color, finished_color
            from dbo.planner_settings
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
            RelationshipStart = reader.IsDBNull(1) ? null : DateOnly.FromDateTime(reader.GetDateTime(1)),
            DefaultReminderDays = reader.GetInt32(2),
            Theme = reader.GetString(3),
            ActiveUserId = reader.GetInt32(4),
            RawgApiKey = reader.IsDBNull(5) ? null : reader.GetString(5),
            SteamWebApiKey = reader.IsDBNull(6) ? null : reader.GetString(6),
            VisualTheme = reader.GetString(7),
            DateIdeaColor = reader.GetString(8),
            ReminderColor = reader.GetString(9),
            MemoryColor = reader.GetString(10),
            CountdownColor = reader.GetString(11),
            GotTogetherColor = reader.GetString(12),
            WantToPlayColor = reader.GetString(13),
            PlayingColor = reader.GetString(14),
            PlayedColor = reader.GetString(15),
            FinishedColor = reader.GetString(16)
        };
    }

    public async Task<PlannerSettings> SaveSettingsAsync(PlannerSettings settings)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            merge dbo.planner_settings as target
            using (select 1 as id) as source
            on target.id = source.id
            when matched then update set
                couple_name = @couple_name,
                relationship_start = @relationship_start,
                default_reminder_days = @default_reminder_days,
                theme = @theme,
                visual_theme = @visual_theme,
                active_user_id = @active_user_id,
                rawg_api_key = @rawg_api_key,
                steam_web_api_key = @steam_web_api_key,
                date_idea_color = @date_idea_color,
                reminder_color = @reminder_color,
                memory_color = @memory_color,
                countdown_color = @countdown_color,
                got_together_color = @got_together_color,
                want_to_play_color = @want_to_play_color,
                playing_color = @playing_color,
                played_color = @played_color,
                finished_color = @finished_color
            when not matched then insert (
                id, couple_name, relationship_start, default_reminder_days, theme, visual_theme, active_user_id,
                rawg_api_key, steam_web_api_key, date_idea_color, reminder_color, memory_color, countdown_color,
                got_together_color, want_to_play_color, playing_color, played_color, finished_color
            ) values (
                1, @couple_name, @relationship_start, @default_reminder_days, @theme, @visual_theme, @active_user_id,
                @rawg_api_key, @steam_web_api_key, @date_idea_color, @reminder_color, @memory_color, @countdown_color,
                @got_together_color, @want_to_play_color, @playing_color, @played_color, @finished_color
            );
            """;
        BindSettings(command, settings);
        await command.ExecuteNonQueryAsync();
        return settings;
    }

    public async Task<IReadOnlyList<PlannerEntry>> GetEntriesAsync(int? userId = null)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            select id, title, kind, target_date, notes, user_id, status,
                   game_title, game_cover_url, game_status, game_rating, game_rawg_id, game_rawg_slug, game_platforms,
                   game_steam_app_id, steam_owned, steam_checked_at, created_at, updated_at
            from dbo.planner_entries
            where (@user_id is null or user_id = @user_id)
            order by case when target_date is null then created_at else cast(target_date as datetimeoffset) end desc, id desc;
            """;
        command.Parameters.AddWithValue("@user_id", userId is null ? DBNull.Value : userId.Value);

        var entries = new List<PlannerEntry>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            entries.Add(ReadEntry(reader));
        }

        return entries;
    }

    public async Task<PlannerEntry> SaveEntryAsync(PlannerEntry entry)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var now = DateTimeOffset.UtcNow;
        if (entry.Id <= 0)
        {
            await using var insert = connection.CreateCommand();
            insert.CommandText = """
                insert into dbo.planner_entries (
                    title, kind, target_date, notes, user_id, status,
                    game_title, game_cover_url, game_status, game_rating, game_rawg_id, game_rawg_slug, game_platforms,
                    game_steam_app_id, steam_owned, steam_checked_at, created_at, updated_at
                )
                output inserted.id
                values (
                    @title, @kind, @target_date, @notes, @user_id, @status,
                    @game_title, @game_cover_url, @game_status, @game_rating, @game_rawg_id, @game_rawg_slug, @game_platforms,
                    @game_steam_app_id, @steam_owned, @steam_checked_at, @created_at, @updated_at
                );
                """;
            BindEntry(insert, entry, now, isInsert: true);
            entry.Id = Convert.ToInt32(await insert.ExecuteScalarAsync());
            entry.CreatedAt = now;
            entry.UpdatedAt = now;
            return entry;
        }

        entry.UpdatedAt = now;
        await using var update = connection.CreateCommand();
        update.CommandText = """
            update dbo.planner_entries
            set title = @title,
                kind = @kind,
                target_date = @target_date,
                notes = @notes,
                user_id = @user_id,
                status = @status,
                game_title = @game_title,
                game_cover_url = @game_cover_url,
                game_status = @game_status,
                game_rating = @game_rating,
                game_rawg_id = @game_rawg_id,
                game_rawg_slug = @game_rawg_slug,
                game_platforms = @game_platforms,
                game_steam_app_id = @game_steam_app_id,
                steam_owned = @steam_owned,
                steam_checked_at = @steam_checked_at,
                updated_at = @updated_at
            where id = @id;
            """;
        update.Parameters.AddWithValue("@id", entry.Id);
        BindEntry(update, entry, now, isInsert: false);
        await update.ExecuteNonQueryAsync();
        return entry;
    }

    public async Task DeleteEntryAsync(int id)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "delete from dbo.planner_entries where id = @id;";
        command.Parameters.AddWithValue("@id", id);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<PlannerSummary> GetSummaryAsync()
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            select
                count(*) as total_entries,
                coalesce(sum(case when kind = 'date' then 1 else 0 end), 0) as date_ideas,
                coalesce(sum(case when kind = 'reminder' then 1 else 0 end), 0) as reminders,
                coalesce(sum(case when kind = 'memory' then 1 else 0 end), 0) as memories,
                coalesce(sum(case when kind = 'countdown' then 1 else 0 end), 0) as countdowns,
                coalesce(sum(case when kind = 'got_together' then 1 else 0 end), 0) as got_together_items,
                coalesce(sum(case when kind in ('game_plan', 'game_status') then 1 else 0 end), 0) as game_entries,
                coalesce(sum(case when game_status = 'want_to_play' then 1 else 0 end), 0) as want_to_play_games,
                coalesce(sum(case when game_status = 'playing' then 1 else 0 end), 0) as playing_games,
                coalesce(sum(case when game_status = 'played' then 1 else 0 end), 0) as played_games,
                coalesce(sum(case when game_status = 'finished' then 1 else 0 end), 0) as finished_games,
                max(updated_at) as last_updated
            from dbo.planner_entries;
            """;

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return new PlannerSummary();
        }

        return new PlannerSummary
        {
            TotalEntries = GetInt(reader, 0),
            DateIdeas = GetInt(reader, 1),
            Reminders = GetInt(reader, 2),
            Memories = GetInt(reader, 3),
            Countdowns = GetInt(reader, 4),
            GotTogetherItems = GetInt(reader, 5),
            GameEntries = GetInt(reader, 6),
            WantToPlayGames = GetInt(reader, 7),
            PlayingGames = GetInt(reader, 8),
            PlayedGames = GetInt(reader, 9),
            FinishedGames = GetInt(reader, 10),
            LastUpdated = reader.IsDBNull(11) ? null : reader.GetDateTimeOffset(11)
        };
    }

    private static PlannerEntry ReadEntry(SqlDataReader reader)
    {
        return new PlannerEntry
        {
            Id = reader.GetInt32(0),
            Title = reader.GetString(1),
            Kind = reader.GetString(2),
            TargetDate = reader.IsDBNull(3) ? null : DateOnly.FromDateTime(reader.GetDateTime(3)),
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
            SteamOwned = reader.IsDBNull(15) ? null : reader.GetBoolean(15),
            SteamCheckedAt = reader.IsDBNull(16) ? null : reader.GetDateTimeOffset(16),
            CreatedAt = reader.GetDateTimeOffset(17),
            UpdatedAt = reader.GetDateTimeOffset(18)
        };
    }

    private static void BindUser(SqlCommand command, PlannerUser user)
    {
        command.Parameters.AddWithValue("@name", user.Name);
        command.Parameters.AddWithValue("@accent_color", user.AccentColor);
        command.Parameters.AddWithValue("@steam_id64", string.IsNullOrWhiteSpace(user.SteamId64) ? DBNull.Value : user.SteamId64);
        command.Parameters.AddWithValue("@is_default", user.IsDefault);
    }

    private static void BindSettings(SqlCommand command, PlannerSettings settings)
    {
        command.Parameters.AddWithValue("@couple_name", settings.CoupleName);
        command.Parameters.AddWithValue("@relationship_start", settings.RelationshipStart is null ? DBNull.Value : settings.RelationshipStart.Value.ToDateTime(TimeOnly.MinValue));
        command.Parameters.AddWithValue("@default_reminder_days", settings.DefaultReminderDays);
        command.Parameters.AddWithValue("@theme", settings.Theme);
        command.Parameters.AddWithValue("@visual_theme", settings.VisualTheme);
        command.Parameters.AddWithValue("@active_user_id", settings.ActiveUserId);
        command.Parameters.AddWithValue("@rawg_api_key", string.IsNullOrWhiteSpace(settings.RawgApiKey) ? DBNull.Value : settings.RawgApiKey);
        command.Parameters.AddWithValue("@steam_web_api_key", string.IsNullOrWhiteSpace(settings.SteamWebApiKey) ? DBNull.Value : settings.SteamWebApiKey);
        command.Parameters.AddWithValue("@date_idea_color", settings.DateIdeaColor);
        command.Parameters.AddWithValue("@reminder_color", settings.ReminderColor);
        command.Parameters.AddWithValue("@memory_color", settings.MemoryColor);
        command.Parameters.AddWithValue("@countdown_color", settings.CountdownColor);
        command.Parameters.AddWithValue("@got_together_color", settings.GotTogetherColor);
        command.Parameters.AddWithValue("@want_to_play_color", settings.WantToPlayColor);
        command.Parameters.AddWithValue("@playing_color", settings.PlayingColor);
        command.Parameters.AddWithValue("@played_color", settings.PlayedColor);
        command.Parameters.AddWithValue("@finished_color", settings.FinishedColor);
    }

    private static void BindEntry(SqlCommand command, PlannerEntry entry, DateTimeOffset timestamp, bool isInsert)
    {
        command.Parameters.AddWithValue("@title", entry.Title);
        command.Parameters.AddWithValue("@kind", entry.Kind);
        command.Parameters.AddWithValue("@target_date", entry.TargetDate is null ? DBNull.Value : entry.TargetDate.Value.ToDateTime(TimeOnly.MinValue));
        command.Parameters.AddWithValue("@notes", entry.Notes);
        command.Parameters.AddWithValue("@user_id", entry.UserId);
        command.Parameters.AddWithValue("@status", entry.Status);
        command.Parameters.AddWithValue("@game_title", string.IsNullOrWhiteSpace(entry.GameTitle) ? DBNull.Value : entry.GameTitle);
        command.Parameters.AddWithValue("@game_cover_url", string.IsNullOrWhiteSpace(entry.GameCoverUrl) ? DBNull.Value : entry.GameCoverUrl);
        command.Parameters.AddWithValue("@game_status", string.IsNullOrWhiteSpace(entry.GameStatus) ? DBNull.Value : entry.GameStatus);
        command.Parameters.AddWithValue("@game_rating", entry.GameRating is null ? DBNull.Value : entry.GameRating.Value);
        command.Parameters.AddWithValue("@game_rawg_id", entry.GameRawgId is null ? DBNull.Value : entry.GameRawgId.Value);
        command.Parameters.AddWithValue("@game_rawg_slug", string.IsNullOrWhiteSpace(entry.GameRawgSlug) ? DBNull.Value : entry.GameRawgSlug);
        command.Parameters.AddWithValue("@game_platforms", string.IsNullOrWhiteSpace(entry.GamePlatforms) ? DBNull.Value : entry.GamePlatforms);
        command.Parameters.AddWithValue("@game_steam_app_id", entry.GameSteamAppId is null ? DBNull.Value : entry.GameSteamAppId.Value);
        command.Parameters.AddWithValue("@steam_owned", entry.SteamOwned is null ? DBNull.Value : entry.SteamOwned.Value);
        command.Parameters.AddWithValue("@steam_checked_at", entry.SteamCheckedAt is null ? DBNull.Value : entry.SteamCheckedAt.Value);
        command.Parameters.AddWithValue("@created_at", isInsert ? timestamp : entry.CreatedAt);
        command.Parameters.AddWithValue("@updated_at", timestamp);
    }

    private async Task EnsureDatabaseExistsAsync()
    {
        await using var connection = new SqlConnection(_masterConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            if db_id(@database_name) is null
            begin
                declare @sql nvarchar(max) = N'create database ' + quotename(@database_name);
                exec sp_executesql @sql;
            end
            """;
        command.Parameters.AddWithValue("@database_name", _databaseName);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<int> CountAsync(SqlConnection connection, string tableName)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"select count(1) from {tableName};";

        var result = await command.ExecuteScalarAsync();
        return result is null or DBNull ? 0 : Convert.ToInt32(result, CultureInfo.InvariantCulture);
    }

    private static int GetInt(SqlDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? 0 : reader.GetInt32(ordinal);
    }

    private static string GetConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PlannerDatabase");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Missing ConnectionStrings:PlannerDatabase configuration.");
        }

        return connectionString;
    }
}
