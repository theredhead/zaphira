using Zaphira.Domain;

namespace Zaphira.Domain.Tests;

public sealed class TextMessagePartTests
{
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void ConstructorRejectsEmptyText(string text)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new TextMessagePart(text));

        Assert.Equal("text", exception.ParamName);
    }

    [Fact]
    public void ConstructorRejectsNullText()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new TextMessagePart(null!));

        Assert.Equal("text", exception.ParamName);
    }
}
