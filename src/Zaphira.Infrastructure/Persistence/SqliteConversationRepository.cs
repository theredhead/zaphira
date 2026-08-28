using System.Globalization;
using Microsoft.Data.Sqlite;
using Zaphira.Application;
using Zaphira.Domain;

namespace Zaphira.Infrastructure.Persistence;

public sealed class SqliteConversationRepository : IConversationRepository
{
    private readonly SqliteConnectionFactory connectionFactory;

    public SqliteConversationRepository(string databaseFile)
    {
        connectionFactory = new SqliteConnectionFactory(databaseFile);
    }

    public async Task SaveAsync(ConversationSummary summary, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(summary);

        await using SqliteConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO conversations (id, title, preview, message_count, created_at, updated_at)
            VALUES ($id, $title, $preview, $messageCount, $createdAt, $updatedAt)
            ON CONFLICT(id) DO UPDATE SET
                title = excluded.title,
                preview = excluded.preview,
                message_count = excluded.message_count,
                updated_at = excluded.updated_at;
            """;
        command.Parameters.AddWithValue("$id", summary.Id.Value.ToString());
        command.Parameters.AddWithValue("$title", summary.Title);
        command.Parameters.AddWithValue("$preview", summary.Preview.Text);
        command.Parameters.AddWithValue("$messageCount", summary.MessageCount);
        command.Parameters.AddWithValue("$createdAt", FormatTimestamp(summary.CreatedAt));
        command.Parameters.AddWithValue("$updatedAt", FormatTimestamp(summary.UpdatedAt));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<ConversationSummaryLookup> GetSummaryAsync(
        ConversationId conversationId,
        CancellationToken cancellationToken)
    {
        await using SqliteConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, title, preview, message_count, created_at, updated_at
            FROM conversations
            WHERE id = $id;
            """;
        command.Parameters.AddWithValue("$id", conversationId.Value.ToString());

        await using SqliteDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return ConversationSummaryLookup.NotFound(conversationId);
        }

        return ConversationSummaryLookup.Found(ReadSummary(reader));
    }

    public async Task<IReadOnlyList<ConversationSummary>> GetSummariesAsync(CancellationToken cancellationToken)
    {
        await using SqliteConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, title, preview, message_count, created_at, updated_at
            FROM conversations
            ORDER BY updated_at DESC, created_at DESC;
            """;

        List<ConversationSummary> summaries = [];
        await using SqliteDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            summaries.Add(ReadSummary(reader));
        }

        return summaries;
    }

    public async Task<bool> DeleteAsync(ConversationId conversationId, CancellationToken cancellationToken)
    {
        await using SqliteConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "DELETE FROM conversations WHERE id = $id;";
        command.Parameters.AddWithValue("$id", conversationId.Value.ToString());

        int affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);

        return affectedRows > 0;
    }

    private static ConversationSummary ReadSummary(SqliteDataReader reader) =>
        new(
            new ConversationId(Guid.Parse(reader.GetString(0))),
            reader.GetString(1),
            new ConversationPreview(reader.GetString(2)),
            reader.GetInt32(3),
            ParseTimestamp(reader.GetString(4)),
            ParseTimestamp(reader.GetString(5)));

    private static string FormatTimestamp(DateTimeOffset value) => value.ToString("O", CultureInfo.InvariantCulture);

    private static DateTimeOffset ParseTimestamp(string value) =>
        DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
}
