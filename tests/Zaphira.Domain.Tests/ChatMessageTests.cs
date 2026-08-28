using Zaphira.Domain;

namespace Zaphira.Domain.Tests;

public sealed class ChatMessageTests
{
    [Fact]
    public void ConstructorStoresImmutableMessageData()
    {
        MessageId messageId = MessageId.New();
        ConversationId conversationId = ConversationId.New();
        TextMessagePart part = new("Hello Zaphira");
        DateTimeOffset createdAt = DateTimeOffset.UtcNow;

        ChatMessage message = new(
            messageId,
            conversationId,
            MessageRole.User,
            [part],
            MessageStatus.Completed,
            createdAt);

        Assert.Equal(messageId, message.Id);
        Assert.Equal(conversationId, message.ConversationId);
        Assert.Equal(MessageRole.User, message.Role);
        Assert.Equal([part], message.Parts);
        Assert.Equal(MessageStatus.Completed, message.Status);
        Assert.Equal(createdAt, message.CreatedAt);
    }

    [Fact]
    public void ConstructorAllowsEmptyPartsForPendingMessage()
    {
        ChatMessage message = new(
            MessageId.New(),
            ConversationId.New(),
            MessageRole.Assistant,
            [],
            MessageStatus.Pending,
            DateTimeOffset.UtcNow);

        Assert.Empty(message.Parts);
    }

    [Fact]
    public void ConstructorRejectsEmptyPartsForCompletedMessage()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            new ChatMessage(
                MessageId.New(),
                ConversationId.New(),
                MessageRole.User,
                [],
                MessageStatus.Completed,
                DateTimeOffset.UtcNow));

        Assert.Equal("parts", exception.ParamName);
    }

    [Fact]
    public void ConstructorRejectsNullParts()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            new ChatMessage(
                MessageId.New(),
                ConversationId.New(),
                MessageRole.User,
                null!,
                MessageStatus.Completed,
                DateTimeOffset.UtcNow));

        Assert.Equal("parts", exception.ParamName);
    }

    [Fact]
    public void ConstructorRejectsNullPartItem()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            new ChatMessage(
                MessageId.New(),
                ConversationId.New(),
                MessageRole.User,
                [null!],
                MessageStatus.Completed,
                DateTimeOffset.UtcNow));

        Assert.Equal("parts", exception.ParamName);
    }
}
