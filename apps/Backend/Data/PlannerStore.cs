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
                        debt_total_amount decimal(18,2) not null constraint df_planner_finance_items_debt_total default 0,
                        initial_paid_amount decimal(18,2) not null constraint df_planner_finance_items_initial_paid default 0,
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

        await using (var addColumnCommand = connection.CreateCommand())
        {
            addColumnCommand.CommandText = """
                if col_length('dbo.planner_finance_items', 'debt_total_amount') is null
                begin
                    alter table dbo.planner_finance_items
                    add debt_total_amount decimal(18,2) not null constraint df_planner_finance_items_debt_total default 0 with values;
                end;

                if col_length('dbo.planner_finance_items', 'initial_paid_amount') is null
                begin
                    alter table dbo.planner_finance_items
                    add initial_paid_amount decimal(18,2) not null constraint df_planner_finance_items_initial_paid default 0 with values;
                end;
                """;
            await addColumnCommand.ExecuteNonQueryAsync();
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

        await EnsureDefaultFinanceSectionsForUserAsync(connection, 1);
        await EnsureDefaultFinanceTemplatesForUserAsync(connection, 1);
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

    public async Task<PlannerUser?> GetUserAsync(int id)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            select id, name, accent_color, steam_id64, is_default
            from dbo.planner_users
            where id = @id;
            """;
        command.Parameters.AddWithValue("@id", id);

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
            IsDefault = reader.GetBoolean(4)
        };
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

    public async Task DeleteUserAsync(int id)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var userCount = await CountAsync(connection, "dbo.planner_users");
        if (userCount <= 1)
        {
            return;
        }

        await using (var deleteCommand = connection.CreateCommand())
        {
            deleteCommand.CommandText = "delete from dbo.planner_users where id = @id;";
            deleteCommand.Parameters.AddWithValue("@id", id);
            await deleteCommand.ExecuteNonQueryAsync();
        }

        var settings = await GetSettingsAsync();
        if (settings.ActiveUserId == id)
        {
            settings.ActiveUserId = await GetDefaultUserIdAsync(connection) ?? 1;
            await SaveSettingsAsync(settings);
        }
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

    public async Task<IReadOnlyList<FinanceItem>> GetFinanceItemsAsync(int userId, DateOnly month)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = """
            select id, user_id, month_key, bucket, name, budget_amount, actual_amount, debt_total_amount, initial_paid_amount, due_date, notes, is_shared, sort_order, created_at, updated_at
            from dbo.planner_finance_items
            where user_id = @user_id and month_key = @month_key
            order by sort_order asc, bucket asc, name asc, id asc;
            """;
        command.Parameters.AddWithValue("@user_id", userId);
        command.Parameters.AddWithValue("@month_key", GetMonthKey(month));

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
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var command = connection.CreateCommand();
        command.CommandText = """
            select id, user_id, month_key, bucket, name, budget_amount, actual_amount, debt_total_amount, initial_paid_amount, due_date, notes, is_shared, sort_order, created_at, updated_at
            from dbo.planner_finance_items
            where id = @id;
            """;
        command.Parameters.AddWithValue("@id", id);

        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? ReadFinanceItem(reader) : null;
    }

    public async Task<FinanceItem> SaveFinanceItemAsync(FinanceItem item)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        var now = DateTimeOffset.UtcNow;
        if (item.Id != 0)
        {
            var existing = await GetFinanceItemAsync(item.Id);
            if (existing is not null)
            {
                item.CreatedAt = existing.CreatedAt;
            }
        }

        await using var command = connection.CreateCommand();
        if (item.Id == 0)
        {
            command.CommandText = """
                insert into dbo.planner_finance_items (
                    user_id, month_key, bucket, name, budget_amount, actual_amount, debt_total_amount, initial_paid_amount, due_date,
                    notes, is_shared, sort_order, created_at, updated_at
                )
                output inserted.id
                values (
                    @user_id, @month_key, @bucket, @name, @budget_amount, @actual_amount, @debt_total_amount, @initial_paid_amount, @due_date,
                    @notes, @is_shared, @sort_order, @created_at, @updated_at
                );
                """;
            command.Parameters.AddWithValue("@created_at", now);
        }
        else
        {
            command.CommandText = """
                update dbo.planner_finance_items
                set user_id = @user_id,
                    month_key = @month_key,
                    bucket = @bucket,
                    name = @name,
                    budget_amount = @budget_amount,
                    actual_amount = @actual_amount,
                    debt_total_amount = @debt_total_amount,
                    initial_paid_amount = @initial_paid_amount,
                    due_date = @due_date,
                    notes = @notes,
                    is_shared = @is_shared,
                    sort_order = @sort_order,
                    updated_at = @updated_at
                where id = @id;
                select @id;
                """;
            command.Parameters.AddWithValue("@id", item.Id);
            command.Parameters.AddWithValue("@created_at", item.CreatedAt == default ? now : item.CreatedAt);
        }

        command.Parameters.AddWithValue("@user_id", item.UserId);
        command.Parameters.AddWithValue("@month_key", string.IsNullOrWhiteSpace(item.MonthKey) ? GetMonthKey(DateOnly.FromDateTime(DateTime.Today)) : item.MonthKey);
        command.Parameters.AddWithValue("@bucket", string.IsNullOrWhiteSpace(item.Bucket) ? "income" : item.Bucket);
        command.Parameters.AddWithValue("@name", item.Name);
        command.Parameters.AddWithValue("@budget_amount", item.BudgetAmount);
        command.Parameters.AddWithValue("@actual_amount", item.ActualAmount);
        command.Parameters.AddWithValue("@debt_total_amount", item.DebtTotalAmount);
        command.Parameters.AddWithValue("@initial_paid_amount", item.InitialPaidAmount);
        command.Parameters.AddWithValue("@due_date", item.DueDate is null ? DBNull.Value : item.DueDate.Value.ToDateTime(TimeOnly.MinValue));
        command.Parameters.AddWithValue("@notes", item.Notes);
        command.Parameters.AddWithValue("@is_shared", item.IsShared);
        command.Parameters.AddWithValue("@sort_order", item.SortOrder);
        command.Parameters.AddWithValue("@updated_at", now);

        item.Id = Convert.ToInt32(await command.ExecuteScalarAsync());
        item.MonthKey = string.IsNullOrWhiteSpace(item.MonthKey) ? GetMonthKey(DateOnly.FromDateTime(DateTime.Today)) : item.MonthKey;
        item.CreatedAt = item.CreatedAt == default ? now : item.CreatedAt;
        item.UpdatedAt = now;
        return item;
    }

    public async Task DeleteFinanceItemAsync(int id)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "delete from dbo.planner_finance_items where id = @id;";
        command.Parameters.AddWithValue("@id", id);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<FinanceSummary> GetFinanceSummaryAsync(int userId, DateOnly month)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            select
                count(*) as item_count,
                coalesce(sum(case when bucket = 'income' then budget_amount else 0 end), 0) as budgeted_income,
                coalesce(sum(case when bucket = 'income' then actual_amount else 0 end), 0) as actual_income,
                coalesce(sum(case when bucket <> 'income' then budget_amount else 0 end), 0) as budgeted_outflow,
                coalesce(sum(case when bucket <> 'income' then actual_amount else 0 end), 0) as actual_outflow,
                coalesce(sum(case when bucket = 'savings' then budget_amount else 0 end), 0) as savings_budget,
                coalesce(sum(case when bucket = 'cash' then budget_amount else 0 end), 0) as cash_budget,
                coalesce(sum(debt_total_amount), 0) as debt_total_amount,
                coalesce(sum(initial_paid_amount), 0) as initial_paid_amount,
                max(updated_at) as last_updated
            from dbo.planner_finance_items
            where user_id = @user_id and month_key = @month_key;
            """;
        command.Parameters.AddWithValue("@user_id", userId);
        command.Parameters.AddWithValue("@month_key", GetMonthKey(month));

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return new FinanceSummary();
        }

        return new FinanceSummary
        {
            ItemCount = GetInt(reader, 0),
            BudgetedIncome = GetDecimalOrZero(reader, 1),
            ActualIncome = GetDecimalOrZero(reader, 2),
            BudgetedOutflow = GetDecimalOrZero(reader, 3),
            ActualOutflow = GetDecimalOrZero(reader, 4),
            SavingsBudget = GetDecimalOrZero(reader, 5),
            CashBudget = GetDecimalOrZero(reader, 6),
            DebtTotalAmount = GetDecimalOrZero(reader, 7),
            InitialPaidAmount = GetDecimalOrZero(reader, 8),
            LastUpdated = reader.IsDBNull(9) ? null : reader.GetDateTimeOffset(9)
        };
    }

    public async Task<IReadOnlyList<FinanceSection>> GetFinanceSectionsAsync(int userId)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await EnsureDefaultFinanceSectionsForUserAsync(connection, userId);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            select id, user_id, section_key, name, color, sort_order, is_builtin
            from dbo.planner_finance_sections
            where user_id = @user_id
            order by is_builtin desc, sort_order asc, name asc;
            """;
        command.Parameters.AddWithValue("@user_id", userId);

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
                IsBuiltin = reader.GetBoolean(6)
            });
        }

        return sections;
    }

    public async Task<FinanceSection?> GetFinanceSectionAsync(int id)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            select id, user_id, section_key, name, color, sort_order, is_builtin
            from dbo.planner_finance_sections
            where id = @id;
            """;
        command.Parameters.AddWithValue("@id", id);

        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? new FinanceSection
        {
            Id = reader.GetInt32(0),
            UserId = reader.GetInt32(1),
            SectionKey = reader.GetString(2),
            Name = reader.GetString(3),
            Color = reader.GetString(4),
            SortOrder = reader.GetInt32(5),
            IsBuiltin = reader.GetBoolean(6)
        } : null;
    }

    public async Task<FinanceSection> SaveFinanceSectionAsync(FinanceSection section)
    {
        await using var connection = new SqlConnection(_connectionString);
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

        await using var command = connection.CreateCommand();
        if (section.Id == 0)
        {
            command.CommandText = """
                insert into dbo.planner_finance_sections (user_id, section_key, name, color, sort_order, is_builtin)
                output inserted.id
                values (@user_id, @section_key, @name, @color, @sort_order, @is_builtin);
                """;
        }
        else
        {
            command.CommandText = """
                update dbo.planner_finance_sections
                set user_id = @user_id,
                    section_key = @section_key,
                    name = @name,
                    color = @color,
                    sort_order = @sort_order,
                    is_builtin = @is_builtin
                where id = @id;
                select @id;
                """;
            command.Parameters.AddWithValue("@id", section.Id);
        }

        command.Parameters.AddWithValue("@user_id", section.UserId);
        command.Parameters.AddWithValue("@section_key", section.SectionKey);
        command.Parameters.AddWithValue("@name", section.Name);
        command.Parameters.AddWithValue("@color", section.Color);
        command.Parameters.AddWithValue("@sort_order", section.SortOrder);
        command.Parameters.AddWithValue("@is_builtin", section.IsBuiltin);

        section.Id = Convert.ToInt32(await command.ExecuteScalarAsync());
        return section;
    }

    public async Task DeleteFinanceSectionAsync(int id)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "delete from dbo.planner_finance_sections where id = @id and is_builtin = 0;";
        command.Parameters.AddWithValue("@id", id);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<IReadOnlyList<FinanceTemplateItem>> GetFinanceTemplatesAsync(int userId)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await EnsureDefaultFinanceTemplatesForUserAsync(connection, userId);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            select id, user_id, section_key, name, budget_amount, actual_amount, notes, is_shared, sort_order
            from dbo.planner_finance_templates
            where user_id = @user_id
            order by sort_order asc, section_key asc, name asc, id asc;
            """;
        command.Parameters.AddWithValue("@user_id", userId);

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
                IsShared = reader.GetBoolean(7),
                SortOrder = reader.GetInt32(8)
            });
        }

        return templates;
    }

    public async Task<FinanceTemplateItem?> GetFinanceTemplateAsync(int id)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            select id, user_id, section_key, name, budget_amount, actual_amount, notes, is_shared, sort_order
            from dbo.planner_finance_templates
            where id = @id;
            """;
        command.Parameters.AddWithValue("@id", id);

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
            IsShared = reader.GetBoolean(7),
            SortOrder = reader.GetInt32(8)
        } : null;
    }

    public async Task<FinanceTemplateItem> SaveFinanceTemplateAsync(FinanceTemplateItem template)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        if (template.Id == 0)
        {
            command.CommandText = """
                insert into dbo.planner_finance_templates (user_id, section_key, name, budget_amount, actual_amount, notes, is_shared, sort_order)
                output inserted.id
                values (@user_id, @section_key, @name, @budget_amount, @actual_amount, @notes, @is_shared, @sort_order);
                """;
        }
        else
        {
            command.CommandText = """
                update dbo.planner_finance_templates
                set user_id = @user_id,
                    section_key = @section_key,
                    name = @name,
                    budget_amount = @budget_amount,
                    actual_amount = @actual_amount,
                    notes = @notes,
                    is_shared = @is_shared,
                    sort_order = @sort_order
                where id = @id;
                select @id;
                """;
            command.Parameters.AddWithValue("@id", template.Id);
        }

        command.Parameters.AddWithValue("@user_id", template.UserId);
        command.Parameters.AddWithValue("@section_key", template.SectionKey);
        command.Parameters.AddWithValue("@name", template.Name);
        command.Parameters.AddWithValue("@budget_amount", template.BudgetAmount);
        command.Parameters.AddWithValue("@actual_amount", template.ActualAmount);
        command.Parameters.AddWithValue("@notes", template.Notes);
        command.Parameters.AddWithValue("@is_shared", template.IsShared);
        command.Parameters.AddWithValue("@sort_order", template.SortOrder);

        template.Id = Convert.ToInt32(await command.ExecuteScalarAsync());
        return template;
    }

    public async Task DeleteFinanceTemplateAsync(int id)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "delete from dbo.planner_finance_templates where id = @id;";
        command.Parameters.AddWithValue("@id", id);
        await command.ExecuteNonQueryAsync();
    }

    public async Task ReassignFinanceItemsAsync(int userId, string fromSectionKey, string toSectionKey)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            update dbo.planner_finance_items
            set bucket = @to_section_key,
                updated_at = @updated_at
            where user_id = @user_id and bucket = @from_section_key;
            """;
        command.Parameters.AddWithValue("@user_id", userId);
        command.Parameters.AddWithValue("@from_section_key", fromSectionKey);
        command.Parameters.AddWithValue("@to_section_key", toSectionKey);
        command.Parameters.AddWithValue("@updated_at", DateTimeOffset.UtcNow);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<bool> HasFinanceImportAsync(int userId, DateOnly month, string source)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            select count(1)
            from dbo.planner_finance_imports
            where user_id = @user_id and month_key = @month_key and source = @source;
            """;
        command.Parameters.AddWithValue("@user_id", userId);
        command.Parameters.AddWithValue("@month_key", GetMonthKey(month));
        command.Parameters.AddWithValue("@source", source);

        var result = await command.ExecuteScalarAsync();
        return result is not null and not DBNull && Convert.ToInt32(result, CultureInfo.InvariantCulture) > 0;
    }

    public async Task RecordFinanceImportAsync(int userId, DateOnly month, string source)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            insert into dbo.planner_finance_imports (user_id, month_key, source, imported_at)
            values (@user_id, @month_key, @source, @imported_at);
            """;
        command.Parameters.AddWithValue("@user_id", userId);
        command.Parameters.AddWithValue("@month_key", GetMonthKey(month));
        command.Parameters.AddWithValue("@source", source);
        command.Parameters.AddWithValue("@imported_at", DateTimeOffset.UtcNow);
        await command.ExecuteNonQueryAsync();
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

    private static FinanceItem ReadFinanceItem(SqlDataReader reader)
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
            DebtTotalAmount = GetDecimalOrZero(reader, 7),
            InitialPaidAmount = GetDecimalOrZero(reader, 8),
            DueDate = reader.IsDBNull(9) ? null : DateOnly.FromDateTime(reader.GetDateTime(9)),
            Notes = reader.GetString(10),
            IsShared = reader.GetBoolean(11),
            SortOrder = reader.GetInt32(12),
            CreatedAt = reader.GetDateTimeOffset(13),
            UpdatedAt = reader.GetDateTimeOffset(14)
        };
    }

    private static async Task EnsureDefaultFinanceSectionsForUserAsync(SqlConnection connection, int userId)
    {
        await using var countCommand = connection.CreateCommand();
        countCommand.CommandText = """
            select count(1)
            from dbo.planner_finance_sections
            where user_id = @user_id;
            """;
        countCommand.Parameters.AddWithValue("@user_id", userId);

        var countResult = await countCommand.ExecuteScalarAsync();
        var count = countResult is null or DBNull ? 0 : Convert.ToInt32(countResult, CultureInfo.InvariantCulture);
        if (count > 0)
        {
            return;
        }

        await using var insert = connection.CreateCommand();
        insert.CommandText = """
            insert into dbo.planner_finance_sections (user_id, section_key, name, color, sort_order, is_builtin)
            values
                (@user_id, 'income', 'Income', '#3f7a46', 0, 1),
                (@user_id, 'fixed', 'Fixed costs', '#c3252d', 1, 1),
                (@user_id, 'flexible', 'Flexible spending', '#8f5c24', 2, 1),
                (@user_id, 'savings', 'Savings', '#6f3db8', 3, 1),
                (@user_id, 'cash', 'Cash buffer', '#d14b75', 4, 1);
            """;
        insert.Parameters.AddWithValue("@user_id", userId);
        await insert.ExecuteNonQueryAsync();
    }

    private static async Task EnsureDefaultFinanceTemplatesForUserAsync(SqlConnection connection, int userId)
    {
        await using var countCommand = connection.CreateCommand();
        countCommand.CommandText = """
            select count(1)
            from dbo.planner_finance_templates
            where user_id = @user_id;
            """;
        countCommand.Parameters.AddWithValue("@user_id", userId);

        var countResult = await countCommand.ExecuteScalarAsync();
        var count = countResult is null or DBNull ? 0 : Convert.ToInt32(countResult, CultureInfo.InvariantCulture);
        if (count > 0)
        {
            return;
        }

        await using var insert = connection.CreateCommand();
        insert.CommandText = """
            insert into dbo.planner_finance_templates (user_id, section_key, name, budget_amount, actual_amount, notes, is_shared, sort_order)
            values
                (@user_id, 'income', 'Income', 1786, 0, 'Monthly income from the sheet', 1, 0),
                (@user_id, 'fixed', 'Rent', 400, 0, '', 1, 1),
                (@user_id, 'fixed', 'Travel', 160, 0, '', 1, 2),
                (@user_id, 'fixed', 'Lunch', 90, 0, '', 1, 3),
                (@user_id, 'fixed', 'Loan', 211.98, 0, '', 1, 4),
                (@user_id, 'fixed', 'Argos', 250, 0, '', 1, 5),
                (@user_id, 'fixed', 'Credit N', 50, 0, '', 1, 6),
                (@user_id, 'fixed', 'Credit M', 100, 0, '', 1, 7),
                (@user_id, 'fixed', 'PlayStation Plus', 6.99, 0, '', 1, 8),
                (@user_id, 'fixed', 'Monzo Max', 17, 0, '', 1, 9),
                (@user_id, 'fixed', 'Humble Bundle', 11.49, 0, '', 1, 10),
                (@user_id, 'flexible', 'Jo', 190, 0, '', 0, 11),
                (@user_id, 'flexible', 'Luna (visit)', 60, 0, '', 0, 12),
                (@user_id, 'flexible', 'Mum', 200, 0, '', 0, 13),
                (@user_id, 'savings', 'Samsung watch', 558, 0, 'Savings goal', 0, 14),
                (@user_id, 'savings', 'Samsung ring', 399, 0, 'Savings goal', 0, 15),
                (@user_id, 'savings', 'Samsung tablet kit', 1386.01, 0, 'Savings goal', 0, 16);
            """;
        insert.Parameters.AddWithValue("@user_id", userId);
        await insert.ExecuteNonQueryAsync();
    }

    private static string GetMonthKey(DateOnly month) => new DateOnly(month.Year, month.Month, 1).ToString("yyyy-MM-dd");

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

    private static async Task<int?> GetDefaultUserIdAsync(SqlConnection connection)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            select top (1) id
            from dbo.planner_users
            order by is_default desc, name asc;
            """;

        var result = await command.ExecuteScalarAsync();
        return result is null or DBNull ? null : Convert.ToInt32(result, CultureInfo.InvariantCulture);
    }

    private static int GetInt(SqlDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? 0 : reader.GetInt32(ordinal);
    }

    private static decimal GetDecimalOrZero(SqlDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? 0m : reader.GetDecimal(ordinal);
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
