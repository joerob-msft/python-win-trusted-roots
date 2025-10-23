using Microsoft.AspNetCore.Mvc;
using SslCertTester.Models;
using SslCertTester.Services;

namespace SslCertTester.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CertificateController : ControllerBase
{
    private readonly CertificateService _certificateService;
    private readonly ILogger<CertificateController> _logger;

    public CertificateController(
        CertificateService certificateService,
        ILogger<CertificateController> logger)
    {
        _certificateService = certificateService;
        _logger = logger;
    }

    /// <summary>
    /// Check if a certificate with the given thumbprint exists in the trusted root store
    /// </summary>
    [HttpGet("check/{thumbprint}")]
    public ActionResult<CertificateCheckResult> CheckCertificate(string thumbprint)
    {
        _logger.LogInformation("Checking certificate with thumbprint: {Thumbprint}", thumbprint);
        var result = _certificateService.CheckCertificateInTrustedRoot(thumbprint);
        return Ok(result);
    }

    /// <summary>
    /// Check if a certificate with the given subject exists in the trusted root store
    /// </summary>
    [HttpGet("check-subject/{subject}")]
    public ActionResult<CertificateCheckResult> CheckCertificateBySubject(string subject)
    {
        _logger.LogInformation("Checking certificate with subject: {Subject}", subject);
        var result = _certificateService.CheckCertificateBySubject(subject);
        return Ok(result);
    }

    /// <summary>
    /// List all certificates in the trusted root store (for debugging)
    /// </summary>
    [HttpGet("list-all")]
    public ActionResult<List<CertificateCheckResult>> ListAll()
    {
        _logger.LogInformation("Listing all trusted root certificates");
        var results = _certificateService.ListAllTrustedRootCertificates();
        return Ok(results);
    }

    /// <summary>
    /// Get the root certificate for a given hostname
    /// </summary>
    [HttpGet("lookup/{hostname}")]
    public async Task<ActionResult<CertificateCheckResult>> LookupCertificate(string hostname)
    {
        _logger.LogInformation("Looking up certificate for hostname: {Hostname}", hostname);
        var result = await _certificateService.GetRootCertificateForHostAsync(hostname);
        return Ok(result);
    }
}
