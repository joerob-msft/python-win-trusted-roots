namespace SslCertTester.Models;

/// <summary>
/// Result from running a WebJob test
/// </summary>
public class WebJobResult
{
    public bool Success { get; set; }
    public string Output { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
    public string JobType { get; set; } = string.Empty;
}
