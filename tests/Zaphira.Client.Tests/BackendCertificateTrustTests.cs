using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Zaphira.Client.Security;

namespace Zaphira.Client.Tests;

public sealed class BackendCertificateTrustTests
{
    [Fact]
    public void ValidateAcceptsMatchingTrustedCertificateThumbprint()
    {
        using X509Certificate2 certificate = TestCertificateFactory.CreateCertificate();
        TrustedBackendConnection connection = new(
            new Uri("https://localhost:5051"),
            certificate.Thumbprint,
            "Local backend certificate.");
        BackendCertificateTrust trust = new([connection]);

        bool isTrusted = trust.Validate(
            new Uri("https://localhost:5051"),
            certificate,
            SslPolicyErrors.RemoteCertificateChainErrors);

        Assert.True(isTrusted);
    }

    [Fact]
    public void ValidateRejectsUnmatchedCertificateThumbprint()
    {
        using X509Certificate2 trustedCertificate = TestCertificateFactory.CreateCertificate();
        using X509Certificate2 presentedCertificate = TestCertificateFactory.CreateCertificate();
        TrustedBackendConnection connection = new(
            new Uri("https://localhost:5051"),
            trustedCertificate.Thumbprint,
            "Local backend certificate.");
        BackendCertificateTrust trust = new([connection]);

        bool isTrusted = trust.Validate(
            new Uri("https://localhost:5051"),
            presentedCertificate,
            SslPolicyErrors.RemoteCertificateChainErrors);

        Assert.False(isTrusted);
    }

    [Fact]
    public void DiagnoseExplainsUntrustedCertificate()
    {
        using X509Certificate2 trustedCertificate = TestCertificateFactory.CreateCertificate();
        using X509Certificate2 presentedCertificate = TestCertificateFactory.CreateCertificate();
        TrustedBackendConnection connection = new(
            new Uri("https://localhost:5051"),
            trustedCertificate.Thumbprint,
            "Local backend certificate.");
        BackendCertificateTrust trust = new([connection]);

        CertificateTrustDiagnostic diagnostic = trust.Diagnose(
            new Uri("https://localhost:5051"),
            presentedCertificate,
            SslPolicyErrors.RemoteCertificateChainErrors);

        Assert.False(diagnostic.IsTrusted);
        Assert.Equal("Backend certificate does not match a trusted connection.", diagnostic.Message);
        Assert.Equal("Pair this backend again or check the connection settings.", diagnostic.Suggestion);
    }
}
