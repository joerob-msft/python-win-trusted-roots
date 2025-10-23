namespace SslCertTester.Models;

/// <summary>
/// Request to trigger a WebJob test
/// </summary>
public class WebJobRequest
{
    public string Url { get; set; } = string.Empty;
    public string JobType { get; set; } = "ssl"; // "ssl" or "requests"
}
