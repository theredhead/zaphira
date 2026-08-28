using System.Security.Cryptography.X509Certificates;

namespace Zaphira.Infrastructure.Security;

public sealed record ServerHttpsCertificateMaterial
{
    public ServerHttpsCertificateMaterial(
        X509Certificate2 certificate,
        string certificatePath,
        string thumbprint,
        string diagnosticMessage)
    {
        ArgumentNullException.ThrowIfNull(certificate);
        ArgumentException.ThrowIfNullOrWhiteSpace(certificatePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(thumbprint);
        ArgumentException.ThrowIfNullOrWhiteSpace(diagnosticMessage);

        Certificate = certificate;
        CertificatePath = Path.GetFullPath(certificatePath);
        Thumbprint = thumbprint;
        DiagnosticMessage = diagnosticMessage;
    }

    public X509Certificate2 Certificate { get; }

    public string CertificatePath { get; }

    public string Thumbprint { get; }

    public string DiagnosticMessage { get; }
}
