using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Net.Sockets;
using SslCertTester.Models;

namespace SslCertTester.Services;

/// <summary>
/// Service for checking certificates in the Windows trusted root certificate store
/// </summary>
public class CertificateService
{
    private readonly ILogger<CertificateService> _logger;

    public CertificateService(ILogger<CertificateService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Get the root certificate for a given hostname
    /// </summary>
    public async Task<CertificateCheckResult> GetRootCertificateForHostAsync(string hostname)
    {
        try
        {
            _logger.LogInformation("Getting root certificate for host: {Hostname}", hostname);

            // Remove protocol if included
            hostname = hostname.Replace("https://", "").Replace("http://", "").Split('/')[0];

            // Create an HTTP client handler that allows us to inspect certificates
            using var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
                {
                    // We want to capture the certificate even if validation fails
                    return true;
                }
            };

            using var httpClient = new HttpClient(handler);
            
            // Make the request to trigger certificate retrieval
            try
            {
                var response = await httpClient.GetAsync($"https://{hostname}", HttpCompletionOption.ResponseHeadersRead);
            }
            catch
            {
                // Ignore connection errors - we just need the certificate
            }

            // Use a socket connection to get certificate details
            using var client = new System.Net.Sockets.TcpClient(hostname, 443);
            using var sslStream = new System.Net.Security.SslStream(client.GetStream(), false, 
                (sender, certificate, chain, sslPolicyErrors) => true);

            await sslStream.AuthenticateAsClientAsync(hostname);

            var remoteCertificate = sslStream.RemoteCertificate;
            if (remoteCertificate != null)
            {
                var cert2 = new X509Certificate2(remoteCertificate);
                var chain = new X509Chain();
                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
                chain.Build(cert2);

                // Get the root certificate (last in chain)
                X509Certificate2? rootCert = null;
                if (chain.ChainElements.Count > 0)
                {
                    rootCert = chain.ChainElements[chain.ChainElements.Count - 1].Certificate;
                }

                if (rootCert != null)
                {
                    _logger.LogInformation("Found root certificate: {Subject}", rootCert.Subject);
                    return new CertificateCheckResult
                    {
                        IsInstalled = false,
                        Thumbprint = rootCert.Thumbprint,
                        Subject = rootCert.Subject,
                        Issuer = rootCert.Issuer,
                        NotAfter = rootCert.NotAfter,
                        Message = $"Root certificate retrieved from {hostname}"
                    };
                }
            }

            return new CertificateCheckResult
            {
                IsInstalled = false,
                Message = "Could not retrieve certificate information"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting root certificate for host: {Hostname}", hostname);
            return new CertificateCheckResult
            {
                IsInstalled = false,
                Message = $"Error: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Check if a certificate with the given thumbprint exists in the Windows trusted root store
    /// </summary>
    public CertificateCheckResult CheckCertificateInTrustedRoot(string thumbprint)
    {
        try
        {
            _logger.LogInformation("Checking for certificate with thumbprint: {Thumbprint}", thumbprint);

            // Open the local machine's trusted root certificate store
            using var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);

            // Search for the certificate by thumbprint
            var certs = store.Certificates.Find(
                X509FindType.FindByThumbprint,
                thumbprint.Replace(" ", "").Replace(":", "").ToUpper(),
                false);

            if (certs.Count > 0)
            {
                var cert = certs[0];
                _logger.LogInformation("Certificate found: {Subject}", cert.Subject);

                return new CertificateCheckResult
                {
                    IsInstalled = true,
                    Thumbprint = cert.Thumbprint,
                    Subject = cert.Subject,
                    Issuer = cert.Issuer,
                    NotAfter = cert.NotAfter,
                    Message = "Certificate is installed in the trusted root store"
                };
            }
            else
            {
                _logger.LogInformation("Certificate not found in trusted root store");
                return new CertificateCheckResult
                {
                    IsInstalled = false,
                    Message = "Certificate is NOT installed in the trusted root store"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking certificate in trusted root store");
            return new CertificateCheckResult
            {
                IsInstalled = false,
                Message = $"Error checking certificate: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Check if a certificate with the given subject exists in the Windows trusted root store
    /// </summary>
    public CertificateCheckResult CheckCertificateBySubject(string subject)
    {
        try
        {
            _logger.LogInformation("Checking for certificate with subject: {Subject}", subject);

            using var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);

            // Search for certificates by subject
            var certs = store.Certificates.Find(
                X509FindType.FindBySubjectName,
                subject,
                false);

            if (certs.Count > 0)
            {
                var cert = certs[0];
                _logger.LogInformation("Certificate found: {Subject} with thumbprint {Thumbprint}", 
                    cert.Subject, cert.Thumbprint);

                return new CertificateCheckResult
                {
                    IsInstalled = true,
                    Thumbprint = cert.Thumbprint,
                    Subject = cert.Subject,
                    Issuer = cert.Issuer,
                    NotAfter = cert.NotAfter,
                    Message = $"Certificate is installed in the trusted root store ({certs.Count} found)"
                };
            }
            else
            {
                _logger.LogInformation("Certificate not found in trusted root store");
                return new CertificateCheckResult
                {
                    IsInstalled = false,
                    Message = "Certificate is NOT installed in the trusted root store"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking certificate in trusted root store");
            return new CertificateCheckResult
            {
                IsInstalled = false,
                Message = $"Error checking certificate: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// List all certificates in the trusted root store (for debugging)
    /// </summary>
    public List<CertificateCheckResult> ListAllTrustedRootCertificates()
    {
        var results = new List<CertificateCheckResult>();

        try
        {
            using var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);

            foreach (var cert in store.Certificates)
            {
                results.Add(new CertificateCheckResult
                {
                    IsInstalled = true,
                    Thumbprint = cert.Thumbprint,
                    Subject = cert.Subject,
                    Issuer = cert.Issuer,
                    NotAfter = cert.NotAfter
                });
            }

            _logger.LogInformation("Found {Count} certificates in trusted root store", results.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing certificates");
        }

        return results;
    }
}
