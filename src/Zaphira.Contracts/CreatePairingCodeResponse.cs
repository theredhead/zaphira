namespace Zaphira.Contracts;

public sealed record CreatePairingCodeResponse
{
    public CreatePairingCodeResponse(string code, DateTimeOffset expiresAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        Code = code;
        ExpiresAt = expiresAt;
    }

    public string Code { get; }

    public DateTimeOffset ExpiresAt { get; }
}
