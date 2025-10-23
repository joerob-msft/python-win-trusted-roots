@description('The name of the web app')
param name string

@description('The location of the web app')
param location string = resourceGroup().location

@description('Tags for the resources')
param tags object = {}

@description('The name of the app service plan')
param appServicePlanName string

@description('The runtime stack for the web app')
param runtimeStack string = 'DOTNET|8.0'

@description('Enable Python for WebJobs')
param pythonExtensionEnabled bool = true

// App Service Plan (Windows)
resource appServicePlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: appServicePlanName
  location: location
  tags: tags
  sku: {
    name: 'B1' // Basic tier - supports WebJobs and Always On
    tier: 'Basic'
    capacity: 1
  }
  kind: 'app' // Windows app
  properties: {
    reserved: false // false = Windows, true = Linux
  }
}

// Web App
resource webApp 'Microsoft.Web/sites@2023-01-01' = {
  name: name
  location: location
  tags: union(tags, { 'azd-service-name': 'web' })
  kind: 'app'
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      netFrameworkVersion: 'v8.0'
      alwaysOn: true // Required for continuous WebJobs
      http20Enabled: true
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      windowsFxVersion: runtimeStack
      metadata: [
        {
          name: 'CURRENT_STACK'
          value: 'dotnet'
        }
      ]
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
        {
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
        }
      ]
    }
  }
}

// Configure Python extension for WebJobs
resource pythonExtension 'Microsoft.Web/sites/siteextensions@2023-01-01' = if (pythonExtensionEnabled) {
  parent: webApp
  name: 'python311x64' // Python 3.11 64-bit
}

// WebJob staging configuration
resource webJobConfig 'Microsoft.Web/sites/config@2023-01-01' = {
  parent: webApp
  name: 'web'
  properties: {
    alwaysOn: true
    webSocketsEnabled: false
  }
}

output id string = webApp.id
output name string = webApp.name
output uri string = 'https://${webApp.properties.defaultHostName}'
output appServicePlanId string = appServicePlan.id
