using Zaphira.Contracts;

namespace Zaphira.Server.Pairing;

internal sealed record ServerPairing
{
    public ServerPairing(
        Guid id,
        string clientName,
        string accessToken,
        DateTimeOffset createdAt,
        bool isRevoked,
        DateTimeOffset revokedAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Pairing identifier cannot be empty.", nameof(id));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(clientName);
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        Id = id;
        ClientName = clientName;
        AccessToken = accessToken;
        CreatedAt = createdAt;
        IsRevoked = isRevoked;
        RevokedAt = revokedAt;
    }

    public Guid Id { get; }

    public string ClientName { get; }

    public string AccessToken { get; }

    public DateTimeOffset CreatedAt { get; }

    public bool IsRevoked { get; }

    public DateTimeOffset RevokedAt { get; }

    public PairingResponse ToResponse() => new(Id, ClientName, CreatedAt, IsRevoked, RevokedAt);
}
