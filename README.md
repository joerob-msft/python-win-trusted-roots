# SSL Certificate Tester for Azure Web App

A .NET 8 web application with Python WebJobs to test SSL certificate validation and Windows Trusted Root Certificate Store behavior on Azure App Service.

## Overview

This application demonstrates the difference between SSL certificate validation methods on Windows:
- **OpenSSL-based validation** (Python `ssl` library) - Does NOT trigger automatic certificate installation
- **Windows CryptoAPI validation** (Python `requests` with `wincertstore`) - WILL trigger automatic certificate installation from Windows Trusted Root Program

## Architecture

- **.NET 8 Web App**: Modern responsive UI for testing and monitoring
- **Python WebJobs**: Background jobs that test SSL connections
- **Azure App Service**: Windows-based hosting with .NET 8 runtime
- **Bicep IaC**: Infrastructure as Code for deployment

## Features

- 🌐 Test SSL connections to any HTTPS endpoint
- 🔍 Client-side certificate chain lookup
- 🪟 Check Windows Trusted Root Certificate Store
- 🐍 Python WebJob execution with real-time output
- 📊 Compare OpenSSL vs Windows CryptoAPI behavior
- 🎨 Modern, responsive UI with Bootstrap 5

## Project Structure

```
.
├── SslCertTester/          # .NET 8 Web Application
│   ├── Controllers/        # API controllers
│   ├── Models/            # Data models
│   ├── Pages/             # Razor Pages
│   └── Services/          # Business logic services
├── WebJobs/
│   └── SslTester/         # Python WebJob scripts
│       ├── ssl_test.py    # OpenSSL-based test
│       └── requests_test.py # WinCertStore test
├── infra/                 # Bicep infrastructure files
│   ├── main.bicep
│   └── app/
│       └── web.bicep
└── azure.yaml             # Azure Developer CLI config
```

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Python 3.9+](https://www.python.org/downloads/)
- [Azure CLI](https://docs.microsoft.com/cli/azure/install-azure-cli)
- [Azure Developer CLI (azd)](https://learn.microsoft.com/azure/developer/azure-developer-cli/install-azd)
- An Azure subscription

## Local Development

### Prerequisites for Local Development

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (required)
- [Python 3.9 or higher](https://www.python.org/downloads/) (required)
- Windows OS (for full certificate store testing)

### Step-by-Step Local Setup

#### 1. Clone the Repository

```powershell
git clone https://github.com/joerob-msft/python-win-trusted-roots.git
cd python-win-trusted-roots
```

#### 2. Install Python Dependencies

```powershell
cd WebJobs\SslTester
pip install -r requirements.txt
```

This installs:

- `requests` - HTTP library that uses Windows CryptoAPI
- `wincertstore` - Windows certificate store integration

#### 3. Copy WebJob Scripts to Local Development Location

```powershell
# From repository root
Copy-Item -Path "WebJobs\SslTester\*" -Destination "SslCertTester\wwwroot\App_Data\jobs\continuous\SslTester\" -Recurse -Force
```

#### 4. Run the .NET Web Application

```powershell
cd SslCertTester
dotnet run
```

The application will start on `http://localhost:5257` (or check the console output for the exact port).

#### 5. Open in Browser

Navigate to the URL shown in the console output (typically `http://localhost:5257`).

### Testing Locally

#### Test Through the Web UI

1. **Open** `http://localhost:5257` in your browser
2. **Click** "Show Certificate Lookup Options" for guidance
3. **Click** "Check for SSL.com Root Certificates" to see if it's already installed
4. **Click** "Run OpenSSL Test" to test without triggering installation
5. **Check** certificate store again (should still not be installed)
6. **Click** "Run Requests Test" to trigger automatic installation
7. **Check** certificate store one more time (should now be installed)

#### Test Python Scripts Directly (Command Line)

```powershell
# Navigate to WebJobs directory
cd WebJobs\SslTester

# Test with OpenSSL (no auto-install)
python ssl_test.py www.ssl.com

# Test with requests/WinCertStore (triggers auto-install)
python requests_test.py www.ssl.com
```

#### Verify Certificate Installation Manually

```powershell
# Check if SSL.com root certificate is installed
Get-ChildItem -Path Cert:\LocalMachine\Root | Where-Object {$_.Subject -like "*SSL.com*"}
```

### Understanding the Code Differences

See [CODE_COMPARISON.md](CODE_COMPARISON.md) for detailed explanation and code examples showing:
- ✅ How OpenSSL approach works (no auto-install)
- ✅ How Windows CryptoAPI approach works (auto-install)
- ✅ Side-by-side comparison
- ✅ When to use each approach
- ✅ Complete working examples you can copy

### Troubleshooting Local Development

#### Port Already in Use
If you see "address already in use" error:
```powershell
# Kill existing dotnet process
Get-Process -Name "dotnet" | Stop-Process -Force

# Or specify a different port
dotnet run --urls "http://localhost:5000"
```

#### Python Not Found
Ensure Python is in your PATH:
```powershell
python --version
# Should show Python 3.9 or higher
```

#### WebJob Scripts Not Found
Make sure you copied the WebJob files:
```powershell
# Check if files exist
Test-Path "SslCertTester\wwwroot\App_Data\jobs\continuous\SslTester\ssl_test.py"
# Should return True
```

#### Certificate Check Requires Administrator
To check the LocalMachine certificate store, you may need to run PowerShell as Administrator:
```powershell
# Right-click PowerShell and "Run as Administrator"
Get-ChildItem -Path Cert:\LocalMachine\Root
```

## Deployment to Azure

### Using Azure Developer CLI (Recommended)

1. **Initialize azd environment**:
```powershell
azd init
```

2. **Provision infrastructure**:

```powershell
```

2. **Provision infrastructure**:
```powershell
azd provision
```

3. **Deploy application**:
```powershell
azd deploy
```

4. **Or do both in one step**:

```powershell
```

4. **Or do both in one step**:
```powershell
azd up
```

### Manual Deployment

1. **Deploy infrastructure**:
```powershell
az deployment sub create --location eastus --template-file ./infra/main.bicep --parameters environmentName=dev location=eastus
```

2. **Deploy web app**:
```powershell
cd SslCertTester
dotnet publish -c Release -o ./publish
Compress-Archive -Path ./publish/* -DestinationPath ../webapp.zip -Force
az webapp deployment source config-zip --resource-group rg-dev --name <webapp-name> --src ../webapp.zip
```

3. **Deploy WebJob**:
```powershell
cd WebJobs/SslTester
Compress-Archive -Path ./* -DestinationPath ../../webjob.zip -Force
az webapp deployment source config-zip --resource-group rg-dev --name <webapp-name> --src ../../webjob.zip
```

## How It Works

### SSL Library Test (OpenSSL)
The `ssl_test.py` script uses Python's built-in `ssl` library, which relies on OpenSSL for certificate validation. OpenSSL does NOT integrate with Windows CryptoAPI and will NOT trigger automatic certificate installation from the Windows Trusted Root Program.

**Expected Behavior**: Connection fails if the root certificate is not in OpenSSL's certificate store.

### Requests Library Test (WinCertStore)
The `requests_test.py` script uses the `requests` library with `wincertstore`, which integrates with Windows CryptoAPI. This WILL trigger the Windows Trusted Root Program to automatically download and install trusted root certificates.

**Expected Behavior**: Connection succeeds, and the certificate is automatically installed in the Windows trusted root store.

## API Endpoints

- `GET /api/Certificate/check/{thumbprint}` - Check if certificate exists by thumbprint
- `GET /api/Certificate/check-subject/{subject}` - Check if certificate exists by subject
- `GET /api/Certificate/list-all` - List all trusted root certificates
- `POST /api/WebJob/run` - Trigger a WebJob test

## Configuration

### App Settings (in Azure)
The application works out of the box with default settings. For production, consider:
- Enabling Application Insights
- Configuring custom domains
- Setting up SSL certificates
- Configuring firewall rules

### Python Version
The Bicep template configures Python 3.11 for WebJobs. To change the version, modify `infra/app/web.bicep`:
```bicep
name: 'python311x64' // Change to python310x64, python39x64, etc.
```

## Testing Scenarios

### Scenario 1: Initial State (No Certificate)
1. Check certificate store → NOT INSTALLED
2. Run OpenSSL test → FAILS (expected)
3. Check certificate store → Still NOT INSTALLED
4. Run Requests test → SUCCEEDS
5. Check certificate store → NOW INSTALLED

### Scenario 2: Certificate Already Installed
1. Check certificate store → INSTALLED
2. Run OpenSSL test → May still FAIL (uses separate cert store)
3. Run Requests test → SUCCEEDS

## Troubleshooting

### WebJob doesn't run
- Ensure "Always On" is enabled in App Service settings
- Check that Python is installed on the App Service
- Verify WebJob files are in `D:\home\site\wwwroot\App_Data\jobs\continuous\SslTester\`

### Certificate check returns incorrect results
- Ensure the web app has permissions to read the certificate store
- Run the app with elevated permissions if testing locally

### Python dependencies missing
- Install requirements: `pip install -r requirements.txt`
- On Azure, ensure Python site extension is installed

## Security Considerations

- Uses HTTPS only
- Minimum TLS version: 1.2
- FTPS disabled
- Follows Azure security best practices
- Uses managed identity where applicable

## Contributing

Feel free to open issues or submit pull requests for improvements.

## License

See LICENSE file for details.

## Resources

- [Azure App Service Documentation](https://learn.microsoft.com/azure/app-service/)
- [WebJobs in Azure App Service](https://learn.microsoft.com/azure/app-service/webjobs-create)
- [Windows Trusted Root Program](https://learn.microsoft.com/security/trusted-root/program-requirements)
- [Python SSL Module](https://docs.python.org/3/library/ssl.html)
- [Requests Library](https://requests.readthedocs.io/)
