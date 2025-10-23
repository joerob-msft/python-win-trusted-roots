namespace SslCertTester.Models;

/// <summary>
/// Result of checking for a certificate in the Windows trusted root store
/// </summary>
public class CertificateCheckResult
{
    public bool IsInstalled { get; set; }
    public string? Thumbprint { get; set; }
    public string? Subject { get; set; }
    public string? Issuer { get; set; }
    public DateTime? NotAfter { get; set; }
    public string? Message { get; set; }
}
