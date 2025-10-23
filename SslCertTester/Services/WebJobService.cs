using System.Diagnostics;
using System.Text;
using SslCertTester.Models;

namespace SslCertTester.Services;

/// <summary>
/// Service for triggering and managing Python WebJobs
/// </summary>
public class WebJobService
{
    private readonly ILogger<WebJobService> _logger;
    private readonly IConfiguration _configuration;

    public WebJobService(ILogger<WebJobService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Run a Python WebJob to test SSL connectivity
    /// </summary>
    public Task<WebJobResult> RunWebJobAsync(WebJobRequest request)
    {
        try
        {
            _logger.LogInformation("Running WebJob of type {JobType} for URL: {Url}", 
                request.JobType, request.Url);

            // Determine which Python script to run
            string scriptName = request.JobType.ToLowerInvariant() switch
            {
                "ssl" => "ssl_test.py",
                "requests" => "requests_test.py",
                _ => "ssl_test.py"
            };

            // Get the path to the Python executable and script
            string pythonPath = GetPythonPath();
            string scriptPath = GetScriptPath(scriptName);

            _logger.LogInformation("Python path: {PythonPath}", pythonPath);
            _logger.LogInformation("Script path: {ScriptPath}", scriptPath);

            // Check if script exists
            if (!File.Exists(scriptPath))
            {
                _logger.LogError("Python script not found: {ScriptPath}", scriptPath);
                return Task.FromResult(new WebJobResult
                {
                    Success = false,
                    Error = $"Script not found: {scriptPath}",
                    JobType = request.JobType
                });
            }

            // Start the Python process
            var startInfo = new ProcessStartInfo
            {
                FileName = pythonPath,
                Arguments = $"\"{scriptPath}\" \"{request.Url}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(scriptPath)
            };

            var output = new StringBuilder();
            var error = new StringBuilder();

            using var process = new Process { StartInfo = startInfo };
            
            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    output.AppendLine(e.Data);
                    _logger.LogInformation("WebJob output: {Data}", e.Data);
                }
            };
            
            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    error.AppendLine(e.Data);
                    _logger.LogWarning("WebJob error: {Data}", e.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Wait for the process to complete (with timeout)
            var timeout = TimeSpan.FromSeconds(30);
            if (!process.WaitForExit((int)timeout.TotalMilliseconds))
            {
                process.Kill();
                throw new TimeoutException($"WebJob execution timed out after {timeout.TotalSeconds} seconds");
            }

            var exitCode = process.ExitCode;
            var outputStr = output.ToString();
            var errorStr = error.ToString();

            _logger.LogInformation("WebJob completed with exit code: {ExitCode}", exitCode);

            return Task.FromResult(new WebJobResult
            {
                Success = exitCode == 0,
                Output = outputStr,
                Error = errorStr,
                JobType = request.JobType
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running WebJob");
            return Task.FromResult(new WebJobResult
            {
                Success = false,
                Error = $"Exception: {ex.Message}",
                JobType = request.JobType
            });
        }
    }

    /// <summary>
    /// Get the path to the Python executable
    /// In Azure WebJobs, Python is typically at D:\Python\python.exe
    /// For local development, use system Python
    /// </summary>
    private string GetPythonPath()
    {
        // Check for Azure WebJobs Python path first
        string azurePythonPath = @"D:\Python311\python.exe";
        if (File.Exists(azurePythonPath))
        {
            return azurePythonPath;
        }

        azurePythonPath = @"D:\Python310\python.exe";
        if (File.Exists(azurePythonPath))
        {
            return azurePythonPath;
        }

        azurePythonPath = @"D:\Python39\python.exe";
        if (File.Exists(azurePythonPath))
        {
            return azurePythonPath;
        }

        // For local development, use "python" from PATH
        return "python";
    }

    /// <summary>
    /// Get the path to the Python script
    /// In Azure, WebJobs are typically in D:\home\site\wwwroot\App_Data\jobs\continuous\{jobname}
    /// For local development, use the WebJobs folder in the solution
    /// </summary>
    private string GetScriptPath(string scriptName)
    {
        // Check Azure WebJobs path
        string azureJobPath = @"D:\home\site\wwwroot\App_Data\jobs\continuous\SslTester\" + scriptName;
        if (File.Exists(azureJobPath))
        {
            return azureJobPath;
        }

        // For local development, look in the WebJobs folder relative to the web app
        string localJobPath = Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "WebJobs", "SslTester", scriptName);
        localJobPath = Path.GetFullPath(localJobPath);

        if (File.Exists(localJobPath))
        {
            return localJobPath;
        }

        // Try wwwroot/App_Data/jobs path for local testing
        string wwwrootJobPath = Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "wwwroot", "App_Data", "jobs", "continuous", "SslTester", scriptName);
        wwwrootJobPath = Path.GetFullPath(wwwrootJobPath);

        return wwwrootJobPath;
    }
}
