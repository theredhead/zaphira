using Microsoft.Data.Sqlite;

namespace Zaphira.Infrastructure.Persistence;

public sealed class SqliteDatabaseMigrator
{
    private static readonly IReadOnlyList<SqliteMigration> Migrations =
    [
        new("0001_initial_schema", InitialSchema)
    ];

    public async Task MigrateAsync(string databaseFile, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseFile);

        string fullDatabaseFile = Path.GetFullPath(databaseFile);
        string databaseDirectory = Path.GetDirectoryName(fullDatabaseFile)
            ?? throw new InvalidOperationException("Database file must have a parent directory.");

        Directory.CreateDirectory(databaseDirectory);

        SqliteConnectionStringBuilder connectionStringBuilder = new()
        {
            DataSource = fullDatabaseFile,
            ForeignKeys = true
        };

        await using SqliteConnection connection = new(connectionStringBuilder.ToString());
        await connection.OpenAsync(cancellationToken);

        await ExecuteNonQueryAsync(connection, "PRAGMA foreign_keys = ON;", cancellationToken);
        await ExecuteNonQueryAsync(connection, CreateSchemaMigrationsTable, cancellationToken);

        foreach (SqliteMigration migration in Migrations)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (await MigrationHasBeenAppliedAsync(connection, migration.Name, cancellationToken))
            {
                continue;
            }

            await using SqliteTransaction transaction =
                (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
            await ExecuteNonQueryAsync(connection, migration.Sql, transaction, cancellationToken);
            await RecordMigrationAsync(connection, migration.Name, transaction, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
    }

    private static async Task<bool> MigrationHasBeenAppliedAsync(
        SqliteConnection connection,
        string migrationName,
        CancellationToken cancellationToken)
    {
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM schema_migrations
            WHERE name = $name;
            """;
        command.Parameters.AddWithValue("$name", migrationName);

        object? result = await command.ExecuteScalarAsync(cancellationToken);

        return Convert.ToInt32(result) == 1;
    }

    private static async Task RecordMigrationAsync(
        SqliteConnection connection,
        string migrationName,
        SqliteTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO schema_migrations (name, applied_at)
            VALUES ($name, $appliedAt);
            """;
        command.Parameters.AddWithValue("$name", migrationName);
        command.Parameters.AddWithValue("$appliedAt", DateTimeOffset.UtcNow.ToString("O"));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task ExecuteNonQueryAsync(
        SqliteConnection connection,
        string commandText,
        CancellationToken cancellationToken)
    {
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = commandText;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task ExecuteNonQueryAsync(
        SqliteConnection connection,
        string commandText,
        SqliteTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = commandText;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private sealed record SqliteMigration(string Name, string Sql);

    private const string CreateSchemaMigrationsTable = """
        CREATE TABLE IF NOT EXISTS schema_migrations (
            name TEXT PRIMARY KEY NOT NULL,
            applied_at TEXT NOT NULL
        );
        """;

    private const string InitialSchema = """
        CREATE TABLE conversations (
            id TEXT PRIMARY KEY NOT NULL,
            title TEXT NOT NULL,
            preview TEXT NOT NULL,
            message_count INTEGER NOT NULL CHECK (message_count >= 0),
            created_at TEXT NOT NULL,
            updated_at TEXT NOT NULL
        );

        CREATE TABLE messages (
            id TEXT PRIMARY KEY NOT NULL,
            conversation_id TEXT NOT NULL,
            role TEXT NOT NULL,
            status TEXT NOT NULL,
            created_at TEXT NOT NULL,
            FOREIGN KEY (conversation_id) REFERENCES conversations(id) ON DELETE CASCADE
        );

        CREATE TABLE message_parts (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            message_id TEXT NOT NULL,
            sequence INTEGER NOT NULL CHECK (sequence >= 0),
            kind TEXT NOT NULL,
            content_json TEXT NOT NULL,
            FOREIGN KEY (message_id) REFERENCES messages(id) ON DELETE CASCADE,
            UNIQUE (message_id, sequence)
        );

        CREATE TABLE file_references (
            id TEXT PRIMARY KEY NOT NULL,
            display_name TEXT NOT NULL,
            media_type TEXT NOT NULL,
            storage_location TEXT NOT NULL,
            size_in_bytes INTEGER NOT NULL CHECK (size_in_bytes >= 0),
            created_at TEXT NOT NULL
        );

        CREATE INDEX messages_conversation_id_index ON messages(conversation_id);
        CREATE INDEX message_parts_message_id_index ON message_parts(message_id);
        """;
}
