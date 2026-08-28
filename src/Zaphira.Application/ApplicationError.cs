namespace Zaphira.Application;

public sealed record ApplicationError
{
    public static ApplicationError None { get; } = new("None", "No error.");

    public ApplicationError(string code, string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Code = code;
        Message = message;
    }

    public string Code { get; }

    public string Message { get; }
}
