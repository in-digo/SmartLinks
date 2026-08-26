using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using SmartLinks.Management.Infrastructure.Persistence;
using SmartLinks.Management.IntegrationTests.Infrastructure;

namespace SmartLinks.Management.IntegrationTests.Persistence;

public sealed class ManagementMigrationTests : IClassFixture<PostgreSqlFixture>
{
    private static readonly string[] _expectedTableNames = ["configuration_changes", "published_smart_links", "smart_link_rules", "smart_links"];
    private static readonly string[] _expectedCitextTableNames = ["published_smart_links", "smart_links"];
    private static readonly string[] _expectedSlugIndexNames = ["ux_published_smart_links_slug", "ux_smart_links_slug"];
    private static readonly string[] _expectedRevisionIndexColumns = ["revision"];

    private readonly PostgreSqlFixture _fixture;

    /// <summary>
    /// Инициализирует тесты миграций Management
    /// </summary>
    public ManagementMigrationTests(PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Проверяет создание таблиц Management применением миграций к пустой базе
    /// </summary>
    [Fact]
    public async Task MigrateAsyncCreatesManagementTables()
    {
        await using var context = CreateContext();
        await RecreateDatabaseAsync(context);

        var connection = await GetOpenConnectionAsync(context);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT table_name
            FROM information_schema.tables
            WHERE table_schema = 'management'
            ORDER BY table_name;
            """;

        var tableNames = await ReadStringsAsync(command);

        Assert.Equal(_expectedTableNames, tableNames);
    }

    /// <summary>
    /// Проверяет создание регистронезависимых столбцов slug
    /// </summary>
    [Fact]
    public async Task MigrateAsyncCreatesCitextSlugColumns()
    {
        await using var context = CreateContext();
        await RecreateDatabaseAsync(context);

        var connection = await GetOpenConnectionAsync(context);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT table_name
            FROM information_schema.columns
            WHERE table_schema = 'management'
                AND column_name = 'slug'
                AND udt_name = 'citext'
            ORDER BY table_name;
            """;

        var tableNames = await ReadStringsAsync(command);

        Assert.Equal(_expectedCitextTableNames, tableNames);
    }

    /// <summary>
    /// Проверяет создание индексов для slug и глобальной ревизии
    /// </summary>
    [Fact]
    public async Task MigrateAsyncCreatesSlugAndRevisionIndexes()
    {
        await using var context = CreateContext();
        await RecreateDatabaseAsync(context);

        var connection = await GetOpenConnectionAsync(context);

        await using var slugIndexCommand = connection.CreateCommand();
        slugIndexCommand.CommandText = """
            SELECT indexname
            FROM pg_indexes
            WHERE schemaname = 'management'
                AND indexname IN ('ux_published_smart_links_slug', 'ux_smart_links_slug')
            ORDER BY indexname;
            """;

        var slugIndexNames = await ReadStringsAsync(slugIndexCommand);

        await using var revisionIndexCommand = connection.CreateCommand();
        revisionIndexCommand.CommandText = """
            SELECT key_usage.column_name
            FROM information_schema.table_constraints AS table_constraint
            INNER JOIN information_schema.key_column_usage AS key_usage
                ON table_constraint.constraint_catalog = key_usage.constraint_catalog
                AND table_constraint.constraint_schema = key_usage.constraint_schema
                AND table_constraint.constraint_name = key_usage.constraint_name
            WHERE table_constraint.constraint_schema = 'management'
                AND table_constraint.table_name = 'configuration_changes'
                AND table_constraint.constraint_type = 'PRIMARY KEY'
            ORDER BY key_usage.ordinal_position;
            """;

        var revisionIndexColumns = await ReadStringsAsync(revisionIndexCommand);

        Assert.Equal(_expectedSlugIndexNames, slugIndexNames);
        Assert.Equal(_expectedRevisionIndexColumns, revisionIndexColumns);
    }

    /// <summary>
    /// Проверяет безопасное повторное применение актуальных миграций
    /// </summary>
    [Fact]
    public async Task MigrateAsyncCanBeAppliedRepeatedly()
    {
        await using var context = CreateContext();

        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();

        var firstAppliedMigrations = (await context.Database.GetAppliedMigrationsAsync()).ToArray();

        await context.Database.MigrateAsync();

        var secondAppliedMigrations = (await context.Database.GetAppliedMigrationsAsync()).ToArray();
        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();

        Assert.NotEmpty(firstAppliedMigrations);
        Assert.Equal(firstAppliedMigrations, secondAppliedMigrations);
        Assert.Empty(pendingMigrations);
    }

    /// <summary>
    /// Проверяет настройку PostgreSQL-провайдера design-time factory
    /// </summary>
    [Fact]
    public void CreateDbContextConfiguresPostgreSqlProvider()
    {
        using var context = new ManagementDbContextFactory().CreateDbContext(Array.Empty<string>());

        Assert.Equal("Npgsql.EntityFrameworkCore.PostgreSQL", context.Database.ProviderName);
    }

    /// <summary>
    /// Пересоздаёт базу применением актуальных миграций
    /// </summary>
    private static async Task RecreateDatabaseAsync(ManagementDbContext context)
    {
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
    }

    /// <summary>
    /// Возвращает открытое соединение контекста
    /// </summary>
    private static async Task<DbConnection> GetOpenConnectionAsync(ManagementDbContext context)
    {
        var connection = context.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync();

        return connection;
    }

    /// <summary>
    /// Читает первый строковый столбец результата запроса
    /// </summary>
    private static async Task<List<string>> ReadStringsAsync(DbCommand command)
    {
        var values = new List<string>();

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
            values.Add(reader.GetString(0));

        return values;
    }

    /// <summary>
    /// Создаёт контекст для тестовой PostgreSQL
    /// </summary>
    private ManagementDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ManagementDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;

        return new ManagementDbContext(options);
    }
}