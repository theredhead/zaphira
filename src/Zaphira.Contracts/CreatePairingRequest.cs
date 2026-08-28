namespace Zaphira.Contracts;

public sealed record CreatePairingRequest
{
    public CreatePairingRequest(string code, string clientName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientName);

        Code = code;
        ClientName = clientName;
    }

    public string Code { get; }

    public string ClientName { get; }
}
