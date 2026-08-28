namespace Zaphira.Contracts;

public sealed record PairingListResponse
{
    public PairingListResponse(IReadOnlyList<PairingResponse> pairings)
    {
        ArgumentNullException.ThrowIfNull(pairings);

        PairingResponse[] materializedPairings = pairings.ToArray();
        if (materializedPairings.Any(pairing => pairing is null))
        {
            throw new ArgumentException("Pairings cannot contain null values.", nameof(pairings));
        }

        Pairings = materializedPairings;
    }

    public IReadOnlyList<PairingResponse> Pairings { get; }
}
