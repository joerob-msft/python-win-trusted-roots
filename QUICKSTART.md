# Quick Start Guide

## Local Testing

### 1. Install Python Dependencies
```powershell
cd WebJobs\SslTester
pip install -r requirements.txt
```

### 2. Run the Web App Locally
```powershell
cd SslCertTester
dotnet run
```

Navigate to: `https://localhost:5001`

### 3. Test Python Scripts Directly
```powershell
cd WebJobs\SslTester

# Test with OpenSSL (no auto-install)
python ssl_test.py www.ssl.com

# Test with requests/WinCertStore (triggers auto-install)
python requests_test.py www.ssl.com
```

## Deploy to Azure

### Option 1: Azure Developer CLI (Recommended)
```powershell
# Initialize (first time only)
azd init

# Provision and deploy
azd up
```

### Option 2: Manual Deployment
```powershell
# Set your variables
$resourceGroup = "rg-ssl-cert-tester"
$location = "eastus"
$webAppName = "app-ssl-cert-tester-unique123"

# 1. Deploy infrastructure
az deployment sub create `
  --location $location `
  --template-file ./infra/main.bicep `
  --parameters environmentName=dev location=$location

# 2. Build and deploy web app
cd SslCertTester
dotnet publish -c Release -o ./publish
Compress-Archive -Path ./publish/* -DestinationPath ../webapp.zip -Force
az webapp deployment source config-zip `
  --resource-group $resourceGroup `
  --name $webAppName `
  --src ../webapp.zip

# 3. Deploy WebJob
cd ..
Compress-Archive -Path ./WebJobs/SslTester/* -DestinationPath ./webjob.zip -Force

# Install Python on the App Service (if not already)
az webapp config set `
  --resource-group $resourceGroup `
  --name $webAppName `
  --linux-fx-version "PYTHON|3.11"

# Upload WebJob manually via Azure Portal or Kudu
# Navigate to: https://<your-app>.scm.azurewebsites.net/DebugConsole
# Upload to: D:\home\site\wwwroot\App_Data\jobs\continuous\SslTester\
```

## Using the Application

### Testing Flow

1. **Enter Target URL**
   - Default: `www.ssl.com`
   - Any HTTPS endpoint works

2. **Lookup Certificate Chain**
   - Click "Lookup Certificate Chain"
   - Optionally enter the root certificate thumbprint manually

3. **Check Certificate Store (Before)**
   - Click "Check Certificate Store"
   - Should show "NOT INSTALLED" initially

4. **Run OpenSSL Test**
   - Click "Run OpenSSL Test"
   - Expected: FAILS (certificate verification error)
   - Check certificate store again → Still NOT INSTALLED

5. **Run Requests Test**
   - Click "Run Requests Test"
   - Expected: SUCCEEDS (connection established)
   - Check certificate store again → NOW INSTALLED

### Key Behaviors

| Test Type | Library | Uses | Triggers Auto-Install? | Expected Result (No Cert) |
|-----------|---------|------|------------------------|---------------------------|
| OpenSSL   | `ssl`   | OpenSSL | ❌ NO | Fails - Certificate verification error |
| Requests  | `requests` + `wincertstore` | Windows CryptoAPI | ✅ YES | Succeeds - Certificate auto-installed |

## Troubleshooting

### WebJob doesn't execute
1. Ensure "Always On" is enabled in App Service configuration
2. Verify Python is installed: `az webapp config show --name <app-name> --resource-group <rg>`
3. Check logs in Kudu: `https://<app-name>.scm.azurewebsites.net/DebugConsole`

### Certificate check fails
1. Run with elevated permissions (Administrator)
2. Check the certificate store manually:
   ```powershell
   Get-ChildItem -Path Cert:\LocalMachine\Root | Where-Object {$_.Subject -like "*SSL.com*"}
   ```

### Python dependencies missing on Azure
1. Install via Kudu console:
   ```bash
   cd D:\home\site\wwwroot\App_Data\jobs\continuous\SslTester
   D:\Python311\python.exe -m pip install -r requirements.txt
   ```

## Architecture Overview

```
┌─────────────────────────────────────────┐
│   Azure App Service (Windows)           │
│   ├─ .NET 8 Web App                     │
│   │  ├─ Razor Pages UI                  │
│   │  ├─ API Controllers                 │
│   │  └─ Certificate Services            │
│   └─ Python WebJobs                     │
│      ├─ ssl_test.py (OpenSSL)           │
│      └─ requests_test.py (CryptoAPI)    │
└─────────────────────────────────────────┘
         │
         ├─ Reads/Checks
         │
         ▼
┌─────────────────────────────────────────┐
│ Windows Trusted Root Certificate Store  │
│ (LocalMachine\Root)                     │
└─────────────────────────────────────────┘
```

## Key Files

- `SslCertTester/` - .NET 8 web application
  - `Controllers/CertificateController.cs` - Certificate store API
  - `Controllers/WebJobController.cs` - WebJob execution API
  - `Services/CertificateService.cs` - Certificate store operations
  - `Services/WebJobService.cs` - Python process management
  - `Pages/Index.cshtml` - Main UI

- `WebJobs/SslTester/` - Python scripts
  - `ssl_test.py` - OpenSSL-based test (no auto-install)
  - `requests_test.py` - CryptoAPI test (auto-install)
  - `requirements.txt` - Python dependencies

- `infra/` - Bicep infrastructure as code
  - `main.bicep` - Main template
  - `app/web.bicep` - Web app resources

- `azure.yaml` - Azure Developer CLI configuration
- `deploy.ps1` - Manual deployment script

## Next Steps

1. **Customize the target URL** - Test with different certificate authorities
2. **Add more scenarios** - Create additional Python scripts for edge cases
3. **Monitoring** - Enable Application Insights for detailed logging
4. **CI/CD** - Set up GitHub Actions or Azure DevOps pipelines
5. **Security** - Configure managed identity for enhanced security

## Resources

- [Azure App Service](https://learn.microsoft.com/azure/app-service/)
- [WebJobs Documentation](https://learn.microsoft.com/azure/app-service/webjobs-create)
- [Windows Trusted Root Program](https://learn.microsoft.com/security/trusted-root/program-requirements)
- [Python SSL Module](https://docs.python.org/3/library/ssl.html)
