namespace Zaphira.Contracts;

public sealed record PairingResponse
{
    public static DateTimeOffset NotRevokedAt { get; } = DateTimeOffset.UnixEpoch;

    public PairingResponse(
        Guid id,
        string clientName,
        DateTimeOffset createdAt,
        bool isRevoked,
        DateTimeOffset revokedAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Pairing identifier cannot be empty.", nameof(id));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(clientName);

        Id = id;
        ClientName = clientName;
        CreatedAt = createdAt;
        IsRevoked = isRevoked;
        RevokedAt = revokedAt;
    }

    public Guid Id { get; }

    public string ClientName { get; }

    public DateTimeOffset CreatedAt { get; }

    public bool IsRevoked { get; }

    public DateTimeOffset RevokedAt { get; }
}
