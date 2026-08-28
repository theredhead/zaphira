namespace Zaphira.Application;

public sealed record OperationResult
{
    private OperationResult(bool isSuccess, ApplicationError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        if (isSuccess && error != ApplicationError.None)
        {
            throw new ArgumentException("A successful result cannot contain an error.", nameof(error));
        }

        if (!isSuccess && error == ApplicationError.None)
        {
            throw new ArgumentException("A failed result must contain an error.", nameof(error));
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public ApplicationError Error { get; }

    public static OperationResult Success() => new(true, ApplicationError.None);

    public static OperationResult Failure(ApplicationError error) => new(false, error);
}
