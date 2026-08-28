namespace Zaphira.Client.Security;

public sealed record CertificateTrustDiagnostic
{
    public CertificateTrustDiagnostic(bool isTrusted, string message, string suggestion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ArgumentException.ThrowIfNullOrWhiteSpace(suggestion);

        IsTrusted = isTrusted;
        Message = message;
        Suggestion = suggestion;
    }

    public bool IsTrusted { get; }

    public string Message { get; }

    public string Suggestion { get; }

    public static CertificateTrustDiagnostic Trusted() =>
        new(true, "Backend certificate is trusted.", "Continue.");

    public static CertificateTrustDiagnostic MissingCertificate() =>
        new(false, "Backend did not present a certificate.", "Restart the backend and try again.");

    public static CertificateTrustDiagnostic UntrustedCertificate() =>
        new(false, "Backend certificate does not match a trusted connection.", "Pair this backend again or check the connection settings.");
}
