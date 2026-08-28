using Zaphira.Application;

namespace Zaphira.Application.Tests;

public sealed class OperationResultTests
{
    [Fact]
    public void SuccessHasNoErrorWithoutUsingNull()
    {
        OperationResult result = OperationResult.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(ApplicationError.None, result.Error);
    }

    [Fact]
    public void FailureCarriesError()
    {
        ApplicationError error = new("Backend.Unavailable", "The backend is unavailable.");

        OperationResult result = OperationResult.Failure(error);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void FailureRejectsNoneError()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            OperationResult.Failure(ApplicationError.None));

        Assert.Equal("error", exception.ParamName);
    }
}
