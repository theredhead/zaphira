using Zaphira.Domain;

namespace Zaphira.Domain.Tests;

public sealed class StrongIdentifierTests
{
    [Fact]
    public void ConversationIdRejectsEmptyGuid()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new ConversationId(Guid.Empty));

        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void MessageIdRejectsEmptyGuid()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new MessageId(Guid.Empty));

        Assert.Equal("value", exception.ParamName);
    }
}
