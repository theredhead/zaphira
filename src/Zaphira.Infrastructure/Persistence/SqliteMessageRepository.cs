using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Zaphira.Application;
using Zaphira.Domain;

namespace Zaphira.Infrastructure.Persistence;

public sealed class SqliteMessageRepository : IMessageRepository
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly SqliteConnectionFactory connectionFactory;

    public SqliteMessageRepository(string databaseFile)
    {
        connectionFactory = new SqliteConnectionFactory(databaseFile);
    }

    public async Task SaveAsync(ChatMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        await using SqliteConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using SqliteTransaction transaction =
            (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        await SaveMessageRowAsync(connection, transaction, message, cancellationToken);
        await DeleteMessagePartsAsync(connection, transaction, message.Id, cancellationToken);
        await DeleteFileReferencesAsync(connection, transaction, message.Id, cancellationToken);
        await SaveMessagePartsAsync(connection, transaction, message, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(
        ConversationId conversationId,
        CancellationToken cancellationToken)
    {
        await using SqliteConnection connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        List<ChatMessage> messages = [];

        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT id, conversation_id, role, status, created_at
            FROM messages
            WHERE conversation_id = $conversationId
            ORDER BY created_at ASC, rowid ASC;
            """;
        command.Parameters.AddWithValue("$conversationId", conversationId.Value.ToString());

        await using SqliteDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            MessageId messageId = new(Guid.Parse(reader.GetString(0)));
            IReadOnlyList<IMessagePart> parts = await LoadPartsAsync(connection, messageId, cancellationToken);

            messages.Add(new ChatMessage(
                messageId,
                new ConversationId(Guid.Parse(reader.GetString(1))),
                Enum.Parse<MessageRole>(reader.GetString(2)),
                parts,
                Enum.Parse<MessageStatus>(reader.GetString(3)),
                ParseTimestamp(reader.GetString(4))));
        }

        return messages;
    }

    private static async Task SaveMessageRowAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        ChatMessage message,
        CancellationToken cancellationToken)
    {
        await using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO messages (id, conversation_id, role, status, created_at)
            VALUES ($id, $conversationId, $role, $status, $createdAt)
            ON CONFLICT(id) DO UPDATE SET
                conversation_id = excluded.conversation_id,
                role = excluded.role,
                status = excluded.status;
            """;
        command.Parameters.AddWithValue("$id", message.Id.Value.ToString());
        command.Parameters.AddWithValue("$conversationId", message.ConversationId.Value.ToString());
        command.Parameters.AddWithValue("$role", message.Role.ToString());
        command.Parameters.AddWithValue("$status", message.Status.ToString());
        command.Parameters.AddWithValue("$createdAt", FormatTimestamp(message.CreatedAt));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task DeleteMessagePartsAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        MessageId messageId,
        CancellationToken cancellationToken)
    {
        await using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "DELETE FROM message_parts WHERE message_id = $messageId;";
        command.Parameters.AddWithValue("$messageId", messageId.Value.ToString());

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task DeleteFileReferencesAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        MessageId messageId,
        CancellationToken cancellationToken)
    {
        await using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "DELETE FROM file_references WHERE id LIKE $idPrefix;";
        command.Parameters.AddWithValue("$idPrefix", $"{messageId.Value:N}:%");

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task SaveMessagePartsAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        ChatMessage message,
        CancellationToken cancellationToken)
    {
        for (int sequence = 0; sequence < message.Parts.Count; sequence++)
        {
            IMessagePart part = message.Parts[sequence];
            await using SqliteCommand command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO message_parts (message_id, sequence, kind, content_json)
                VALUES ($messageId, $sequence, $kind, $contentJson);
                """;
            command.Parameters.AddWithValue("$messageId", message.Id.Value.ToString());
            command.Parameters.AddWithValue("$sequence", sequence);
            command.Parameters.AddWithValue("$kind", GetPartKind(part));
            command.Parameters.AddWithValue("$contentJson", SerializePart(part));

            await command.ExecuteNonQueryAsync(cancellationToken);

            if (part is FileMessagePart fileMessagePart)
            {
                await SaveFileReferenceAsync(
                    connection,
                    transaction,
                    message.Id,
                    sequence,
                    fileMessagePart.Reference,
                    cancellationToken);
            }
        }
    }

    private static async Task SaveFileReferenceAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        MessageId messageId,
        int sequence,
        FileReference reference,
        CancellationToken cancellationToken)
    {
        await using SqliteCommand command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO file_references (id, display_name, media_type, storage_location, size_in_bytes, created_at)
            VALUES ($id, $displayName, $mediaType, $storageLocation, $sizeInBytes, $createdAt);
            """;
        command.Parameters.AddWithValue("$id", $"{messageId.Value:N}:{sequence}");
        command.Parameters.AddWithValue("$displayName", reference.DisplayName);
        command.Parameters.AddWithValue("$mediaType", reference.MediaType);
        command.Parameters.AddWithValue("$storageLocation", reference.StorageLocation);
        command.Parameters.AddWithValue("$sizeInBytes", reference.SizeInBytes);
        command.Parameters.AddWithValue("$createdAt", FormatTimestamp(DateTimeOffset.UtcNow));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<IReadOnlyList<IMessagePart>> LoadPartsAsync(
        SqliteConnection connection,
        MessageId messageId,
        CancellationToken cancellationToken)
    {
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT kind, content_json
            FROM message_parts
            WHERE message_id = $messageId
            ORDER BY sequence ASC;
            """;
        command.Parameters.AddWithValue("$messageId", messageId.Value.ToString());

        List<IMessagePart> parts = [];
        await using SqliteDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            parts.Add(DeserializePart(reader.GetString(0), reader.GetString(1)));
        }

        return parts;
    }

    private static string GetPartKind(IMessagePart part) =>
        part switch
        {
            TextMessagePart => "text",
            FileMessagePart => "file",
            ReasoningMessagePart => "reasoning",
            UnknownMessagePart => "unknown",
            _ => throw new NotSupportedException($"Message part type '{part.GetType().Name}' is not supported.")
        };

    private static string SerializePart(IMessagePart part) =>
        part switch
        {
            TextMessagePart text => JsonSerializer.Serialize(new TextPartPayload(text.Text), SerializerOptions),
            FileMessagePart file => JsonSerializer.Serialize(
                new FilePartPayload(
                    file.Reference.DisplayName,
                    file.Reference.MediaType,
                    file.Reference.StorageLocation,
                    file.Reference.SizeInBytes),
                SerializerOptions),
            ReasoningMessagePart reasoning => JsonSerializer.Serialize(new ReasoningPartPayload(reasoning.Summary), SerializerOptions),
            UnknownMessagePart unknown => JsonSerializer.Serialize(
                new UnknownPartPayload(unknown.OriginalKind, unknown.Payload),
                SerializerOptions),
            _ => throw new NotSupportedException($"Message part type '{part.GetType().Name}' is not supported.")
        };

    private static IMessagePart DeserializePart(string kind, string contentJson) =>
        kind switch
        {
            "text" => new TextMessagePart(Deserialize<TextPartPayload>(contentJson).Text),
            "file" => ToFileMessagePart(Deserialize<FilePartPayload>(contentJson)),
            "reasoning" => new ReasoningMessagePart(Deserialize<ReasoningPartPayload>(contentJson).Summary),
            "unknown" => ToUnknownMessagePart(Deserialize<UnknownPartPayload>(contentJson)),
            _ => new UnknownMessagePart(kind, contentJson)
        };

    private static FileMessagePart ToFileMessagePart(FilePartPayload payload) =>
        new(new FileReference(payload.DisplayName, payload.MediaType, payload.StorageLocation, payload.SizeInBytes));

    private static UnknownMessagePart ToUnknownMessagePart(UnknownPartPayload payload) =>
        new(payload.OriginalKind, payload.Payload);

    private static TPayload Deserialize<TPayload>(string contentJson)
    {
        TPayload? payload = JsonSerializer.Deserialize<TPayload>(contentJson, SerializerOptions);

        return payload ?? throw new InvalidOperationException("Stored message part content could not be deserialized.");
    }

    private static string FormatTimestamp(DateTimeOffset value) => value.ToString("O", CultureInfo.InvariantCulture);

    private static DateTimeOffset ParseTimestamp(string value) =>
        DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

    private sealed record TextPartPayload(string Text);

    private sealed record FilePartPayload(string DisplayName, string MediaType, string StorageLocation, long SizeInBytes);

    private sealed record ReasoningPartPayload(string Summary);

    private sealed record UnknownPartPayload(string OriginalKind, string Payload);
}
