namespace Zaphira.Server.Pairing;

internal sealed record PairingCode
{
    public static PairingCode None { get; } = new("__zaphira_no_pairing_code__", DateTimeOffset.UnixEpoch);

    public PairingCode(string value, DateTimeOffset expiresAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        Value = value;
        ExpiresAt = expiresAt;
    }

    public string Value { get; }

    public DateTimeOffset ExpiresAt { get; }

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;
}
