using Zaphira.Domain;

namespace Zaphira.Domain.Tests;

public sealed class UnknownMessagePartTests
{
    [Fact]
    public void ConstructorStoresUnsupportedPartDetails()
    {
        UnknownMessagePart part = new("vendor-widget", "{}");

        Assert.Equal("vendor-widget", part.OriginalKind);
        Assert.Equal("{}", part.Payload);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void ConstructorRejectsEmptyOriginalKind(string originalKind)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new UnknownMessagePart(originalKind, "{}"));

        Assert.Equal("originalKind", exception.ParamName);
    }

    [Fact]
    public void ConstructorRejectsNullPayload()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            new UnknownMessagePart("vendor-widget", null!));

        Assert.Equal("payload", exception.ParamName);
    }
}
