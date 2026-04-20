// T-INFRA-06: App Service Plan (B1 Linux) + App Service
// Container-based, pulls from ACR via managed identity

@description('Location for all resources')
param location string

@description('App Service Plan name')
param appServicePlanName string

@description('App Service (web app) name')
param appServiceName string

@description('ACR login server (e.g. acrgop360staging.azurecr.io)')
param acrLoginServer string

@description('Docker image name without tag (e.g. gop-api)')
param dockerImageName string = 'gop-api'

@description('Docker image tag')
param dockerImageTag string = 'latest'

@description('Managed Identity resource ID')
param identityId string

@description('Managed Identity client ID')
param identityClientId string

@description('Key Vault URI for app configuration')
param keyVaultUri string

@description('Application Insights connection string')
param appInsightsConnectionString string

@description('Tags to apply to resources')
param tags object = {}

resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: appServicePlanName
  location: location
  tags: tags
  sku: {
    name: 'B1'
    tier: 'Basic'
  }
  kind: 'linux'
  properties: {
    reserved: true  // Required for Linux
  }
}

resource appService 'Microsoft.Web/sites@2023-12-01' = {
  name: appServiceName
  location: location
  tags: tags
  kind: 'app,linux,container'
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${identityId}': {}
    }
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true  // RN-SEC-01
    siteConfig: {
      linuxFxVersion: 'DOCKER|${acrLoginServer}/${dockerImageName}:${dockerImageTag}'
      acrUseManagedIdentityCreds: true
      acrUserManagedIdentityID: identityClientId
      alwaysOn: false  // B1 can sleep — staging only
      http20Enabled: true
      minTlsVersion: '1.2'  // RN-SEC-01
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Staging'
        }
        {
          name: 'ASPNETCORE_URLS'
          value: 'http://+:8080'
        }
        {
          name: 'AZURE_CLIENT_ID'
          value: identityClientId
        }
        {
          name: 'KeyVaultUri'
          value: keyVaultUri
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
        {
          name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
          value: '~3'
        }
        {
          name: 'WEBSITES_PORT'
          value: '8080'
        }
        // Traefik/container config
        {
          name: 'DOCKER_REGISTRY_SERVER_URL'
          value: 'https://${acrLoginServer}'
        }
      ]
      cors: {
        allowedOrigins: [
          'https://*.netlify.app'
          'http://localhost:4200'
          'https://localhost:4200'
        ]
        supportCredentials: true
      }
    }
  }
}

// Health check configuration
resource healthCheck 'Microsoft.Web/sites/config@2023-12-01' = {
  parent: appService
  name: 'web'
  properties: {
    healthCheckPath: '/health/ready'
  }
}

output appServiceId string = appService.id
output appServiceUrl string = 'https://${appService.properties.defaultHostName}'
output appServiceName string = appService.name
output appServicePlanId string = appServicePlan.id
