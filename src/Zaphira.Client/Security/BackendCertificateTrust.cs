using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Zaphira.Client.Security;

public sealed class BackendCertificateTrust
{
    private readonly IReadOnlyList<TrustedBackendConnection> trustedConnections;

    public BackendCertificateTrust(IEnumerable<TrustedBackendConnection> trustedConnections)
    {
        ArgumentNullException.ThrowIfNull(trustedConnections);

        TrustedBackendConnection[] materializedConnections = trustedConnections.ToArray();
        if (materializedConnections.Any(connection => connection is null))
        {
            throw new ArgumentException("Trusted backend connections cannot contain null values.", nameof(trustedConnections));
        }

        this.trustedConnections = materializedConnections;
    }

    public bool Validate(Uri backendAddress, X509Certificate2 certificate, SslPolicyErrors policyErrors)
    {
        ArgumentNullException.ThrowIfNull(backendAddress);
        ArgumentNullException.ThrowIfNull(certificate);

        if (policyErrors == SslPolicyErrors.None)
        {
            return true;
        }

        string presentedThumbprint = TrustedBackendConnection.NormalizeThumbprint(certificate.Thumbprint);

        return trustedConnections.Any(connection =>
            Uri.Compare(
                connection.BackendAddress,
                backendAddress,
                UriComponents.HttpRequestUrl,
                UriFormat.SafeUnescaped,
                StringComparison.OrdinalIgnoreCase) == 0
            && connection.CertificateThumbprint == presentedThumbprint);
    }

    public CertificateTrustDiagnostic Diagnose(
        Uri backendAddress,
        X509Certificate2 certificate,
        SslPolicyErrors policyErrors)
    {
        ArgumentNullException.ThrowIfNull(backendAddress);
        ArgumentNullException.ThrowIfNull(certificate);

        return Validate(backendAddress, certificate, policyErrors)
            ? CertificateTrustDiagnostic.Trusted()
            : CertificateTrustDiagnostic.UntrustedCertificate();
    }

    public HttpClientHandler CreateHttpClientHandler(Uri backendAddress)
    {
        ArgumentNullException.ThrowIfNull(backendAddress);

        return new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, certificate, _, policyErrors) =>
            {
                if (certificate is null)
                {
                    return false;
                }

                X509Certificate2 certificate2 = certificate as X509Certificate2 ?? new X509Certificate2(certificate);

                return Validate(backendAddress, certificate2, policyErrors);
            }
        };
    }
}
