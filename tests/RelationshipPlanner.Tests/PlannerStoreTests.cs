using Microsoft.Data.SqlClient;
using RelationshipPlanner.Api.Data;

namespace RelationshipPlanner.Tests;

public sealed class PlannerStoreTests
{
    private string _databaseName = string.Empty;
    private string _connectionString = string.Empty;
    private string _masterConnectionString = string.Empty;

    [SetUp]
    public void SetUp()
    {
        _databaseName = $"RelationshipPlannerTests_{Guid.NewGuid():N}";

        var builder = new SqlConnectionStringBuilder
        {
            DataSource = @"(localdb)\RPDB",
            InitialCatalog = _databaseName,
            IntegratedSecurity = true,
            TrustServerCertificate = true,
            MultipleActiveResultSets = true
        };

        _connectionString = builder.ConnectionString;
        _masterConnectionString = new SqlConnectionStringBuilder(builder.ConnectionString)
        {
            InitialCatalog = "master"
        }.ConnectionString;
    }

    [TearDown]
    public async Task TearDown()
    {
        await DropDatabaseAsync();
    }

    [Test]
    public async Task InitializeAsync_CreatesDatabaseInSqlServer()
    {
        var store = CreateStore();

        await store.InitializeAsync();

        await using var connection = new SqlConnection(_masterConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "select db_id(@database_name);";
        command.Parameters.AddWithValue("@database_name", _databaseName);

        var result = await command.ExecuteScalarAsync();

        Assert.That(result, Is.Not.Null);
        Assert.That(Convert.ToInt32(result), Is.GreaterThan(0));
    }

    [Test]
    public async Task InitializeAsync_CreatesDefaultTables()
    {
        var store = CreateStore();

        await store.InitializeAsync();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        foreach (var tableName in new[]
        {
            "planner_users",
            "planner_settings",
            "planner_entries",
            "planner_finance_items",
            "planner_finance_sections",
            "planner_finance_imports",
            "planner_finance_templates"
        })
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "select object_id(@object_name, 'U');";
            command.Parameters.AddWithValue("@object_name", $"dbo.{tableName}");

            var result = await command.ExecuteScalarAsync();

            Assert.That(result, Is.Not.Null, $"{tableName} should exist.");
            Assert.That(Convert.ToInt32(result), Is.GreaterThan(0), $"{tableName} should exist.");
        }

        foreach (var columnName in new[]
        {
            "debt_total_amount",
            "initial_paid_amount"
        })
        {
            await using var columnCommand = connection.CreateCommand();
            columnCommand.CommandText = """
                select col_length('dbo.planner_finance_items', @column_name);
                """;
            columnCommand.Parameters.AddWithValue("@column_name", columnName);

            var columnResult = await columnCommand.ExecuteScalarAsync();

            Assert.That(columnResult, Is.Not.Null, $"{columnName} should exist.");
            Assert.That(Convert.ToInt32(columnResult), Is.GreaterThan(0), $"{columnName} should exist.");
        }
    }

    [Test]
    public async Task InitializeAsync_SeedsDefaultUserAndSettings()
    {
        var store = CreateStore();

        await store.InitializeAsync();

        var users = await store.GetUsersAsync();
        var settings = await store.GetSettingsAsync();

        Assert.That(users, Has.Count.EqualTo(1));
        Assert.That(users[0].Name, Is.EqualTo("You"));
        Assert.That(users[0].IsDefault, Is.True);
        Assert.That(settings.CoupleName, Is.EqualTo("Us"));
        Assert.That(settings.ActiveUserId, Is.EqualTo(1));
        Assert.That(settings.VisualTheme, Is.EqualTo("persona"));
    }

    [Test]
    public async Task SaveUserAsync_PersistsInsertAndDefaultSelection()
    {
        var store = CreateStore();
        await store.InitializeAsync();

        var inserted = await store.SaveUserAsync(new PlannerUser
        {
            Name = "Alex",
            AccentColor = "#112233",
            IsDefault = true
        });

        var users = await store.GetUsersAsync();

        Assert.That(inserted.Id, Is.GreaterThan(0));
        Assert.That(users, Has.Count.EqualTo(2));
        Assert.That(users.Single(user => user.Id == inserted.Id).IsDefault, Is.True);
        Assert.That(users.Single(user => user.Name == "You").IsDefault, Is.False);
    }

    [Test]
    public async Task SaveEntryAsync_PersistsEntryAndSummary()
    {
        var store = CreateStore();
        await store.InitializeAsync();

        var saved = await store.SaveEntryAsync(new PlannerEntry
        {
            Title = "Game night",
            Kind = "date",
            Notes = "Co-op and snacks",
            UserId = 1,
            Status = "planned",
            TargetDate = new DateOnly(2026, 4, 5)
        });

        var entries = await store.GetEntriesAsync(1);
        var summary = await store.GetSummaryAsync();

        Assert.That(saved.Id, Is.GreaterThan(0));
        Assert.That(entries, Has.Count.EqualTo(1));
        Assert.That(entries[0].Title, Is.EqualTo("Game night"));
        Assert.That(entries[0].TargetDate, Is.EqualTo(new DateOnly(2026, 4, 5)));
        Assert.That(summary.TotalEntries, Is.EqualTo(1));
        Assert.That(summary.DateIdeas, Is.EqualTo(1));
    }

    [Test]
    public async Task SaveFinanceItemAsync_PersistsDebtFieldsAndSummary()
    {
        var store = CreateStore();
        await store.InitializeAsync();

        var saved = await store.SaveFinanceItemAsync(new FinanceItem
        {
            UserId = 1,
            MonthKey = "2026-04-01",
            Bucket = "fixed",
            Name = "Car finance",
            BudgetAmount = 220.50m,
            ActualAmount = 125.25m,
            DebtTotalAmount = 1200m,
            InitialPaidAmount = 300m,
            Notes = "Monthly car finance",
            IsShared = true,
            SortOrder = 0
        });

        var items = await store.GetFinanceItemsAsync(1, new DateOnly(2026, 4, 1));
        var summary = await store.GetFinanceSummaryAsync(1, new DateOnly(2026, 4, 1));

        Assert.That(saved.Id, Is.GreaterThan(0));
        Assert.That(items, Has.Count.EqualTo(1));
        Assert.That(items[0].DebtTotalAmount, Is.EqualTo(1200m));
        Assert.That(items[0].InitialPaidAmount, Is.EqualTo(300m));
        Assert.That(summary.DebtTotalAmount, Is.EqualTo(1200m));
        Assert.That(summary.InitialPaidAmount, Is.EqualTo(300m));
        Assert.That(summary.DebtRemainingAmount, Is.EqualTo(900m));
    }

    private PlannerStore CreateStore()
    {
        return new PlannerStore(_connectionString);
    }

    private async Task DropDatabaseAsync()
    {
        if (string.IsNullOrWhiteSpace(_databaseName))
        {
            return;
        }

        try
        {
            await using var connection = new SqlConnection(_masterConnectionString);
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = """
                if db_id(@database_name) is not null
                begin
                    declare @sql nvarchar(max) = N'alter database ' + quotename(@database_name) + N' set single_user with rollback immediate; drop database ' + quotename(@database_name) + N';';
                    exec sp_executesql @sql;
                end
                """;
            command.Parameters.AddWithValue("@database_name", _databaseName);
            await command.ExecuteNonQueryAsync();
        }
        catch (SqlException)
        {
        }
    }
}
