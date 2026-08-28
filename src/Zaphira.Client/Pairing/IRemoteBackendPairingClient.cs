using Zaphira.Contracts;

namespace Zaphira.Client.Pairing;

public interface IRemoteBackendPairingClient
{
    Task<bool> CheckBackendAsync(CancellationToken cancellationToken);

    Task<CreatePairingResponse> PairAsync(
        string pairingCode,
        string clientName,
        CancellationToken cancellationToken);

    Task RevokePairingAsync(Guid pairingId, string accessToken, CancellationToken cancellationToken);
}
