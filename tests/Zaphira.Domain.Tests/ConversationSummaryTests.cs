using Zaphira.Domain;

namespace Zaphira.Domain.Tests;

public sealed class ConversationSummaryTests
{
    [Fact]
    public void ConstructorStoresSummaryData()
    {
        ConversationId conversationId = ConversationId.New();
        ConversationPreview preview = new("Latest answer");
        DateTimeOffset createdAt = DateTimeOffset.UtcNow.AddMinutes(-2);
        DateTimeOffset updatedAt = DateTimeOffset.UtcNow;

        ConversationSummary summary = new(conversationId, "Research", preview, 3, createdAt, updatedAt);

        Assert.Equal(conversationId, summary.Id);
        Assert.Equal("Research", summary.Title);
        Assert.Equal(preview, summary.Preview);
        Assert.Equal(3, summary.MessageCount);
        Assert.Equal(createdAt, summary.CreatedAt);
        Assert.Equal(updatedAt, summary.UpdatedAt);
    }

    [Fact]
    public void EmptyPreviewCreatesExplicitPlaceholder()
    {
        ConversationPreview preview = ConversationPreview.Empty();

        Assert.Equal("No messages yet.", preview.Text);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void ConstructorRejectsEmptyTitle(string title)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            new ConversationSummary(ConversationId.New(), title, ConversationPreview.Empty(), 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));

        Assert.Equal("title", exception.ParamName);
    }

    [Fact]
    public void ConstructorRejectsNegativeMessageCount()
    {
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ConversationSummary(ConversationId.New(), "Research", ConversationPreview.Empty(), -1, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));

        Assert.Equal("messageCount", exception.ParamName);
    }

    [Fact]
    public void ConstructorRejectsUpdatedAtBeforeCreatedAt()
    {
        DateTimeOffset createdAt = DateTimeOffset.UtcNow;

        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            new ConversationSummary(ConversationId.New(), "Research", ConversationPreview.Empty(), 0, createdAt, createdAt.AddSeconds(-1)));

        Assert.Equal("updatedAt", exception.ParamName);
    }

    [Fact]
    public void PreviewRejectsNullText()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new ConversationPreview(null!));

        Assert.Equal("text", exception.ParamName);
    }
}
