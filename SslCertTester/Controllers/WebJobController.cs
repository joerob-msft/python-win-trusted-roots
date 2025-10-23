using Microsoft.AspNetCore.Mvc;
using SslCertTester.Models;
using SslCertTester.Services;

namespace SslCertTester.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WebJobController : ControllerBase
{
    private readonly WebJobService _webJobService;
    private readonly ILogger<WebJobController> _logger;

    public WebJobController(
        WebJobService webJobService,
        ILogger<WebJobController> logger)
    {
        _webJobService = webJobService;
        _logger = logger;
    }

    /// <summary>
    /// Trigger a WebJob to test SSL connectivity
    /// </summary>
    [HttpPost("run")]
    public async Task<ActionResult<WebJobResult>> RunWebJob([FromBody] WebJobRequest request)
    {
        _logger.LogInformation("Triggering WebJob: {JobType} for URL: {Url}", 
            request.JobType, request.Url);

        if (string.IsNullOrWhiteSpace(request.Url))
        {
            return BadRequest(new WebJobResult
            {
                Success = false,
                Error = "URL is required",
                JobType = request.JobType
            });
        }

        var result = await _webJobService.RunWebJobAsync(request);
        return Ok(result);
    }
}
