using Zaphira.Application;

namespace Zaphira.Application.Tests;

public sealed class ApplicationErrorTests
{
    [Fact]
    public void NoneRepresentsAbsenceWithoutUsingNull()
    {
        ApplicationError error = ApplicationError.None;

        Assert.Equal("None", error.Code);
        Assert.Equal("No error.", error.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void ConstructorRejectsInvalidCode(string code)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            new ApplicationError(code, "A useful message."));

        Assert.Equal("code", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void ConstructorRejectsInvalidMessage(string message)
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            new ApplicationError("Example.Code", message));

        Assert.Equal("message", exception.ParamName);
    }
}
