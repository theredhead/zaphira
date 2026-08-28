using Zaphira.Domain;

namespace Zaphira.Domain.Tests;

public sealed class ReasoningMessagePartTests
{
    [Fact]
    public void HiddenCreatesExplicitPlaceholder()
    {
        ReasoningMessagePart part = ReasoningMessagePart.Hidden();

        Assert.Equal("Reasoning not provided.", part.Summary);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void ConstructorRejectsEmptySummary(string summary)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new ReasoningMessagePart(summary));

        Assert.Equal("summary", exception.ParamName);
    }

    [Fact]
    public void ConstructorRejectsNullSummary()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new ReasoningMessagePart(null!));

        Assert.Equal("summary", exception.ParamName);
    }
}
