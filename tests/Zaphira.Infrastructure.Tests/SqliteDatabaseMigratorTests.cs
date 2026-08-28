using Microsoft.Data.Sqlite;
using Zaphira.Infrastructure.Persistence;

namespace Zaphira.Infrastructure.Tests;

public sealed class SqliteDatabaseMigratorTests
{
    [Fact]
    public async Task MigrateAsyncCreatesVersionedDatabaseSchema()
    {
        string databaseFile = GetTemporaryDatabaseFile();
        SqliteDatabaseMigrator migrator = new();

        await migrator.MigrateAsync(databaseFile, CancellationToken.None);

        await using SqliteConnection connection = OpenDatabase(databaseFile);
        await connection.OpenAsync();

        Assert.True(await TableExistsAsync(connection, "schema_migrations"));
        Assert.True(await TableExistsAsync(connection, "conversations"));
        Assert.True(await TableExistsAsync(connection, "messages"));
        Assert.True(await TableExistsAsync(connection, "message_parts"));
        Assert.True(await TableExistsAsync(connection, "file_references"));
        Assert.Equal(1, await AppliedMigrationCountAsync(connection));

        File.Delete(databaseFile);
    }

    [Fact]
    public async Task MigrateAsyncIsIdempotent()
    {
        string databaseFile = GetTemporaryDatabaseFile();
        SqliteDatabaseMigrator migrator = new();

        await migrator.MigrateAsync(databaseFile, CancellationToken.None);
        await migrator.MigrateAsync(databaseFile, CancellationToken.None);

        await using SqliteConnection connection = OpenDatabase(databaseFile);
        await connection.OpenAsync();

        Assert.Equal(1, await AppliedMigrationCountAsync(connection));

        File.Delete(databaseFile);
    }

    [Fact]
    public async Task MessagePartsRequireExistingMessages()
    {
        string databaseFile = GetTemporaryDatabaseFile();
        SqliteDatabaseMigrator migrator = new();

        await migrator.MigrateAsync(databaseFile, CancellationToken.None);

        await using SqliteConnection connection = OpenDatabase(databaseFile);
        await connection.OpenAsync();

        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO message_parts (message_id, sequence, kind, content_json)
            VALUES ('missing-message', 0, 'text', '{}');
            """;

        await Assert.ThrowsAsync<SqliteException>(() => command.ExecuteNonQueryAsync());

        File.Delete(databaseFile);
    }

    [Fact]
    public async Task FileReferenceSchemaStoresMetadataWithoutBinaryContent()
    {
        string databaseFile = GetTemporaryDatabaseFile();
        SqliteDatabaseMigrator migrator = new();

        await migrator.MigrateAsync(databaseFile, CancellationToken.None);

        await using SqliteConnection connection = OpenDatabase(databaseFile);
        await connection.OpenAsync();

        IReadOnlyList<string> columnTypes = await ColumnTypesAsync(connection, "file_references");

        Assert.DoesNotContain("BLOB", columnTypes);

        File.Delete(databaseFile);
    }

    private static string GetTemporaryDatabaseFile() =>
        Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");

    private static SqliteConnection OpenDatabase(string databaseFile)
    {
        SqliteConnectionStringBuilder connectionStringBuilder = new()
        {
            DataSource = databaseFile,
            ForeignKeys = true
        };

        return new SqliteConnection(connectionStringBuilder.ToString());
    }

    private static async Task<bool> TableExistsAsync(SqliteConnection connection, string tableName)
    {
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM sqlite_master
            WHERE type = 'table'
              AND name = $tableName;
            """;
        command.Parameters.AddWithValue("$tableName", tableName);

        object? result = await command.ExecuteScalarAsync();

        return Convert.ToInt32(result) == 1;
    }

    private static async Task<int> AppliedMigrationCountAsync(SqliteConnection connection)
    {
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM schema_migrations;";

        object? result = await command.ExecuteScalarAsync();

        return Convert.ToInt32(result);
    }

    private static async Task<IReadOnlyList<string>> ColumnTypesAsync(SqliteConnection connection, string tableName)
    {
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({tableName});";

        List<string> columnTypes = [];
        await using SqliteDataReader reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            columnTypes.Add(reader.GetString(2).ToUpperInvariant());
        }

        return columnTypes;
    }
}
