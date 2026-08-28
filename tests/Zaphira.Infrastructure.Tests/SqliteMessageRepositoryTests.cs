using Microsoft.Data.Sqlite;
using Zaphira.Domain;
using Zaphira.Infrastructure.Persistence;

namespace Zaphira.Infrastructure.Tests;

public sealed class SqliteMessageRepositoryTests
{
    [Fact]
    public async Task SaveAsyncPersistsMessagesAndOrderedPartsAcrossRepositoryInstances()
    {
        string databaseFile = await CreateMigratedDatabaseAsync();
        ConversationId conversationId = ConversationId.New();
        await SaveConversationAsync(databaseFile, conversationId);

        ChatMessage message = new(
            MessageId.New(),
            conversationId,
            MessageRole.Assistant,
            [
                new TextMessagePart("Answer"),
                new FileMessagePart(new FileReference("notes.txt", "text/plain", "files/notes.txt", 12)),
                new ReasoningMessagePart("Short visible rationale."),
                new UnknownMessagePart("vendor-widget", "{}")
            ],
            MessageStatus.Completed,
            DateTimeOffset.UtcNow);

        SqliteMessageRepository firstRepository = new(databaseFile);
        await firstRepository.SaveAsync(message, CancellationToken.None);

        SqliteMessageRepository secondRepository = new(databaseFile);
        IReadOnlyList<ChatMessage> messages = await secondRepository.GetMessagesAsync(conversationId, CancellationToken.None);

        ChatMessage persistedMessage = Assert.Single(messages);
        Assert.Equal(message.Id, persistedMessage.Id);
        Assert.Equal(message.ConversationId, persistedMessage.ConversationId);
        Assert.Equal(message.Role, persistedMessage.Role);
        Assert.Equal(message.Status, persistedMessage.Status);
        Assert.Equal(message.CreatedAt, persistedMessage.CreatedAt);
        Assert.Equal(message.Parts, persistedMessage.Parts);

        File.Delete(databaseFile);
    }

    [Fact]
    public async Task SaveAsyncPersistsFilePartMetadataWithoutBinaryContent()
    {
        string databaseFile = await CreateMigratedDatabaseAsync();
        ConversationId conversationId = ConversationId.New();
        await SaveConversationAsync(databaseFile, conversationId);
        ChatMessage message = new(
            MessageId.New(),
            conversationId,
            MessageRole.User,
            [new FileMessagePart(new FileReference("notes.txt", "text/plain", "files/notes.txt", 12))],
            MessageStatus.Completed,
            DateTimeOffset.UtcNow);

        SqliteMessageRepository repository = new(databaseFile);
        await repository.SaveAsync(message, CancellationToken.None);

        await using SqliteConnection connection = new($"Data Source={databaseFile}");
        await connection.OpenAsync();
        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT display_name, media_type, storage_location, size_in_bytes
            FROM file_references;
            """;

        await using SqliteDataReader reader = await command.ExecuteReaderAsync();

        Assert.True(await reader.ReadAsync());
        Assert.Equal("notes.txt", reader.GetString(0));
        Assert.Equal("text/plain", reader.GetString(1));
        Assert.Equal("files/notes.txt", reader.GetString(2));
        Assert.Equal(12, reader.GetInt64(3));
        Assert.False(await reader.ReadAsync());

        File.Delete(databaseFile);
    }

    [Theory]
    [InlineData(MessageStatus.Pending)]
    [InlineData(MessageStatus.Streaming)]
    [InlineData(MessageStatus.Completed)]
    [InlineData(MessageStatus.Cancelled)]
    [InlineData(MessageStatus.Failed)]
    public async Task SaveAsyncPersistsMessageStatus(MessageStatus status)
    {
        string databaseFile = await CreateMigratedDatabaseAsync();
        ConversationId conversationId = ConversationId.New();
        await SaveConversationAsync(databaseFile, conversationId);
        ChatMessage message = new(
            MessageId.New(),
            conversationId,
            MessageRole.Assistant,
            [new TextMessagePart("Partial answer")],
            status,
            DateTimeOffset.UtcNow);

        SqliteMessageRepository repository = new(databaseFile);
        await repository.SaveAsync(message, CancellationToken.None);

        IReadOnlyList<ChatMessage> messages = await repository.GetMessagesAsync(conversationId, CancellationToken.None);

        Assert.Equal(status, Assert.Single(messages).Status);

        File.Delete(databaseFile);
    }

    [Fact]
    public async Task GetMessagesAsyncReturnsEmptyListWhenConversationHasNoMessages()
    {
        string databaseFile = await CreateMigratedDatabaseAsync();
        ConversationId conversationId = ConversationId.New();
        await SaveConversationAsync(databaseFile, conversationId);
        SqliteMessageRepository repository = new(databaseFile);

        IReadOnlyList<ChatMessage> messages = await repository.GetMessagesAsync(conversationId, CancellationToken.None);

        Assert.Empty(messages);

        File.Delete(databaseFile);
    }

    private static async Task<string> CreateMigratedDatabaseAsync()
    {
        string databaseFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
        await new SqliteDatabaseMigrator().MigrateAsync(databaseFile, CancellationToken.None);

        return databaseFile;
    }

    private static async Task SaveConversationAsync(string databaseFile, ConversationId conversationId)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        SqliteConversationRepository repository = new(databaseFile);
        ConversationSummary summary = new(conversationId, "Research", ConversationPreview.Empty(), 0, now, now);

        await repository.SaveAsync(summary, CancellationToken.None);
    }
}
