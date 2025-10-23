# PowerShell deployment script for manual deployment
param(
    [string]$ResourceGroup = "rg-ssl-cert-tester",
    [string]$WebAppName,
    [string]$Location = "eastus"
)

Write-Host "Starting deployment..." -ForegroundColor Green

# Check if Azure CLI is installed
try {
    az --version | Out-Null
} catch {
    Write-Error "Azure CLI is not installed. Please install it from https://aka.ms/installazurecli"
    exit 1
}

# Login to Azure (if not already logged in)
Write-Host "Checking Azure login status..." -ForegroundColor Yellow
$account = az account show 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Host "Please login to Azure..." -ForegroundColor Yellow
    az login
}

# Build the .NET application
Write-Host "`nBuilding .NET application..." -ForegroundColor Yellow
Set-Location -Path "$PSScriptRoot\SslCertTester"
dotnet publish -c Release -o ./publish
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to build .NET application"
    exit 1
}

# Create deployment package
Write-Host "Creating deployment package..." -ForegroundColor Yellow
Set-Location -Path "$PSScriptRoot"
Compress-Archive -Path "./SslCertTester/publish/*" -DestinationPath "./webapp.zip" -Force

# Deploy web app
Write-Host "`nDeploying web app to Azure..." -ForegroundColor Yellow
az webapp deployment source config-zip `
    --resource-group $ResourceGroup `
    --name $WebAppName `
    --src "./webapp.zip"

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to deploy web app"
    exit 1
}

# Create WebJob package
Write-Host "`nCreating WebJob package..." -ForegroundColor Yellow
Compress-Archive -Path "./WebJobs/SslTester/*" -DestinationPath "./webjob.zip" -Force

# Deploy WebJob (note: this deploys to the continuous WebJobs folder)
Write-Host "Deploying WebJob..." -ForegroundColor Yellow
# For WebJobs, we need to use kudu deployment
$webapp = az webapp show --name $WebAppName --resource-group $ResourceGroup | ConvertFrom-Json
$kuduUrl = "https://$($webapp.defaultHostName.Replace('.azurewebsites.net', '.scm.azurewebsites.net'))"

Write-Host "WebJob needs to be deployed manually via Kudu or Azure Portal" -ForegroundColor Yellow
Write-Host "Upload webjob.zip to: $kuduUrl/api/zip/site/wwwroot/App_Data/jobs/continuous/SslTester" -ForegroundColor Yellow

# Clean up
Write-Host "`nCleaning up temporary files..." -ForegroundColor Yellow
Remove-Item "./webapp.zip" -Force -ErrorAction SilentlyContinue
Remove-Item "./webjob.zip" -Force -ErrorAction SilentlyContinue
Remove-Item "./SslCertTester/publish" -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "`nDeployment completed!" -ForegroundColor Green
Write-Host "Web App URL: https://$($webapp.defaultHostName)" -ForegroundColor Cyan
