using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Zaphira.Infrastructure.Storage;

namespace Zaphira.Infrastructure.Security;

public sealed class ServerHttpsCertificateManager
{
    private const string CertificatePassword = "";
    private const string CertificateSubject = "CN=localhost";

    public async Task<ServerHttpsCertificateMaterial> LoadOrCreateAsync(
        ZaphiraDataDirectories dataDirectories,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dataDirectories);
        cancellationToken.ThrowIfCancellationRequested();

        Directory.CreateDirectory(dataDirectories.ServerCertificatesRoot);

        X509Certificate2 certificate = File.Exists(dataDirectories.ServerHttpsCertificateFile)
            ? LoadCertificate(dataDirectories.ServerHttpsCertificateFile)
            : await CreateAndStoreCertificateAsync(dataDirectories.ServerHttpsCertificateFile, cancellationToken);

        return new ServerHttpsCertificateMaterial(
            certificate,
            dataDirectories.ServerHttpsCertificateFile,
            certificate.Thumbprint,
            "Loaded HTTPS certificate for localhost.");
    }

    private static X509Certificate2 LoadCertificate(string certificateFile) =>
        X509CertificateLoader.LoadPkcs12FromFile(
            certificateFile,
            CertificatePassword,
            X509KeyStorageFlags.DefaultKeySet);

    private static async Task<X509Certificate2> CreateAndStoreCertificateAsync(
        string certificateFile,
        CancellationToken cancellationToken)
    {
        using RSA rsa = RSA.Create(2048);
        CertificateRequest request = new(CertificateSubject, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        request.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(certificateAuthority: false, hasPathLengthConstraint: false, pathLengthConstraint: 0, critical: true));
        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, critical: true));
        request.CertificateExtensions.Add(
            new X509EnhancedKeyUsageExtension(
                [new Oid("1.3.6.1.5.5.7.3.1", "Server Authentication")],
                critical: false));

        SubjectAlternativeNameBuilder subjectAlternativeNameBuilder = new();
        subjectAlternativeNameBuilder.AddDnsName("localhost");
        subjectAlternativeNameBuilder.AddIpAddress(IPAddress.Loopback);
        subjectAlternativeNameBuilder.AddIpAddress(IPAddress.IPv6Loopback);
        request.CertificateExtensions.Add(subjectAlternativeNameBuilder.Build());

        using X509Certificate2 certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddMinutes(-5),
            DateTimeOffset.UtcNow.AddYears(2));

        byte[] certificateBytes = certificate.Export(X509ContentType.Pkcs12, CertificatePassword);
        await File.WriteAllBytesAsync(certificateFile, certificateBytes, cancellationToken);

        return LoadCertificate(certificateFile);
    }
}
