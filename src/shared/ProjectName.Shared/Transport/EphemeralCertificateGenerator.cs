using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace ProjectName.Shared.Transport
{
    /// <summary>
    /// Utility for generating ephemeral self-signed X.509 certificates for TLS 1.3 / DTLS encryption.
    /// In accordance with Milestone 2 and LAN-first security requirements, all control and media
    /// signaling traffic between endpoints must be encrypted in transit using mutual or standard TLS.
    /// </summary>
    public static class EphemeralCertificateGenerator
    {
        /// <summary>
        /// Generates a self-signed X509Certificate2 with an RSA private key for in-memory TLS sessions.
        /// </summary>
        /// <param name="commonName">Subject common name (e.g., device ID or hostname).</param>
        /// <param name="validityDays">Certificate validity duration in days (default: 30 days).</param>
        /// <returns>An X509Certificate2 instance containing the private key.</returns>
        public static X509Certificate2 CreateDeviceCertificate(string commonName = "ProjectNameDevice", int validityDays = 30)
        {
            if (string.IsNullOrWhiteSpace(commonName))
            {
                commonName = "ProjectNameDevice";
            }

            using var rsa = RSA.Create(2048);
            var subjectName = new X500DistinguishedName($"CN={commonName}");
            var request = new CertificateRequest(subjectName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            // Add standard key usages for TLS server and client authentication
            request.CertificateExtensions.Add(
                new X509KeyUsageExtension(
                    X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                    critical: true));

            var enhancedKeyUsage = new OidCollection
            {
                new Oid("1.3.6.1.5.5.7.3.1"), // Server Authentication
                new Oid("1.3.6.1.5.5.7.3.2")  // Client Authentication
            };
            request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(enhancedKeyUsage, critical: false));

            var notBefore = DateTimeOffset.UtcNow.AddMinutes(-5);
            var notAfter = DateTimeOffset.UtcNow.AddDays(validityDays);

            var certificate = request.CreateSelfSigned(notBefore, notAfter);

            // Export to PFX and reload to ensure the private key is properly attached across all OS platforms
            byte[] pfxData = certificate.Export(X509ContentType.Pfx);
            return new X509Certificate2(pfxData, (string?)null, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet);
        }
    }
}
