using Zaphira.Domain;
using Zaphira.Application;
using Zaphira.Infrastructure.Persistence;

namespace Zaphira.Infrastructure.Tests;

public sealed class SqliteConversationRepositoryTests
{
    [Fact]
    public async Task SaveAsyncPersistsConversationSummaryAcrossRepositoryInstances()
    {
        string databaseFile = await CreateMigratedDatabaseAsync();
        ConversationSummary summary = new(
            ConversationId.New(),
            "Research",
            new ConversationPreview("Latest answer"),
            2,
            DateTimeOffset.UtcNow.AddMinutes(-1),
            DateTimeOffset.UtcNow);

        SqliteConversationRepository firstRepository = new(databaseFile);
        await firstRepository.SaveAsync(summary, CancellationToken.None);

        SqliteConversationRepository secondRepository = new(databaseFile);
        IReadOnlyList<ConversationSummary> summaries = await secondRepository.GetSummariesAsync(CancellationToken.None);

        ConversationSummary persistedSummary = Assert.Single(summaries);
        Assert.Equal(summary, persistedSummary);

        File.Delete(databaseFile);
    }

    [Fact]
    public async Task GetSummariesAsyncReturnsEmptyListWhenNoConversationsExist()
    {
        string databaseFile = await CreateMigratedDatabaseAsync();
        SqliteConversationRepository repository = new(databaseFile);

        IReadOnlyList<ConversationSummary> summaries = await repository.GetSummariesAsync(CancellationToken.None);

        Assert.Empty(summaries);

        File.Delete(databaseFile);
    }

    [Fact]
    public async Task GetSummaryAsyncReturnsFoundSummary()
    {
        string databaseFile = await CreateMigratedDatabaseAsync();
        SqliteConversationRepository repository = new(databaseFile);
        ConversationSummary summary = new(
            ConversationId.New(),
            "Research",
            new ConversationPreview("Latest answer"),
            2,
            DateTimeOffset.UtcNow.AddMinutes(-1),
            DateTimeOffset.UtcNow);

        await repository.SaveAsync(summary, CancellationToken.None);

        ConversationSummaryLookup lookup = await repository.GetSummaryAsync(summary.Id, CancellationToken.None);

        Assert.True(lookup.Exists);
        Assert.Equal(summary, lookup.Summary);

        File.Delete(databaseFile);
    }

    [Fact]
    public async Task GetSummaryAsyncReturnsNotFoundLookup()
    {
        string databaseFile = await CreateMigratedDatabaseAsync();
        SqliteConversationRepository repository = new(databaseFile);
        ConversationId conversationId = ConversationId.New();

        ConversationSummaryLookup lookup = await repository.GetSummaryAsync(conversationId, CancellationToken.None);

        Assert.False(lookup.Exists);
        Assert.Equal(conversationId, lookup.Summary.Id);

        File.Delete(databaseFile);
    }

    [Fact]
    public async Task DeleteAsyncRemovesConversation()
    {
        string databaseFile = await CreateMigratedDatabaseAsync();
        SqliteConversationRepository repository = new(databaseFile);
        ConversationSummary summary = new(
            ConversationId.New(),
            "Research",
            ConversationPreview.Empty(),
            0,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);

        await repository.SaveAsync(summary, CancellationToken.None);

        bool deleted = await repository.DeleteAsync(summary.Id, CancellationToken.None);
        IReadOnlyList<ConversationSummary> summaries = await repository.GetSummariesAsync(CancellationToken.None);

        Assert.True(deleted);
        Assert.Empty(summaries);

        File.Delete(databaseFile);
    }

    [Fact]
    public async Task DeleteAsyncReturnsFalseWhenConversationDoesNotExist()
    {
        string databaseFile = await CreateMigratedDatabaseAsync();
        SqliteConversationRepository repository = new(databaseFile);

        bool deleted = await repository.DeleteAsync(ConversationId.New(), CancellationToken.None);

        Assert.False(deleted);

        File.Delete(databaseFile);
    }

    [Fact]
    public async Task GetSummariesAsyncOrdersNewestUpdatedConversationFirst()
    {
        string databaseFile = await CreateMigratedDatabaseAsync();
        SqliteConversationRepository repository = new(databaseFile);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        ConversationSummary older = new(ConversationId.New(), "Older", ConversationPreview.Empty(), 0, now.AddHours(-2), now.AddHours(-2));
        ConversationSummary newer = new(ConversationId.New(), "Newer", ConversationPreview.Empty(), 0, now.AddHours(-1), now);

        await repository.SaveAsync(older, CancellationToken.None);
        await repository.SaveAsync(newer, CancellationToken.None);

        IReadOnlyList<ConversationSummary> summaries = await repository.GetSummariesAsync(CancellationToken.None);

        Assert.Equal([newer.Id, older.Id], summaries.Select(summary => summary.Id));

        File.Delete(databaseFile);
    }

    private static async Task<string> CreateMigratedDatabaseAsync()
    {
        string databaseFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");
        await new SqliteDatabaseMigrator().MigrateAsync(databaseFile, CancellationToken.None);

        return databaseFile;
    }
}
