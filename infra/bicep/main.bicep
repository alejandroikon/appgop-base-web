// T-INFRA-07: Main Bicep Orchestrator
// GOP 360° Staging Infrastructure
// Region: eastus2, Cost target: ≤$25/month

targetScope = 'resourceGroup'

@description('Environment name')
param environment string = 'staging'

@description('Location for all resources')
param location string = resourceGroup().location

@description('SQL admin login')
param sqlAdminLogin string = 'gopadmin'

@secure()
@description('SQL admin password')
param sqlAdminPassword string

@description('Developer IP to whitelist in SQL firewall (optional)')
param devIpAddress string = ''

@description('Docker image tag to deploy')
param dockerImageTag string = 'latest'

@description('Tags applied to all resources')
param tags object = {
  project: 'GOP360'
  environment: 'staging'
  managedBy: 'bicep'
  owner: 'IKLABS-Interkont'
}

// ── Naming ──────────────────────────────────────────────────────────────────
var prefix = 'gop360-${environment}'
var identityName = 'id-${prefix}'
var acrName = 'acr${replace(prefix, '-', '')}' // no hyphens: acrgop360staging
var keyVaultName = 'kv-${prefix}'
var sqlServerName = 'sql-${prefix}'
var sqlDatabaseName = 'sqldb-${prefix}'
var logAnalyticsName = 'log-${prefix}'
var appInsightsName = 'appi-${prefix}'
var appServicePlanName = 'asp-${prefix}'
var appServiceName = 'app-${prefix}-api'

// ── Modules ─────────────────────────────────────────────────────────────────

module identity 'modules/identity.bicep' = {
  name: 'identity'
  params: {
    location: location
    identityName: identityName
    tags: tags
  }
}

module acr 'modules/acr.bicep' = {
  name: 'acr'
  params: {
    location: location
    acrName: acrName
    identityPrincipalId: identity.outputs.identityPrincipalId
    tags: tags
  }
}

module keyvault 'modules/keyvault.bicep' = {
  name: 'keyvault'
  params: {
    location: location
    keyVaultName: keyVaultName
    identityPrincipalId: identity.outputs.identityPrincipalId
    tags: tags
  }
}

module sql 'modules/sql.bicep' = {
  name: 'sql'
  params: {
    location: location
    sqlServerName: sqlServerName
    sqlDatabaseName: sqlDatabaseName
    sqlAdminLogin: sqlAdminLogin
    sqlAdminPassword: sqlAdminPassword
    devIpAddress: devIpAddress
    tags: tags
  }
}

module monitoring 'modules/monitoring.bicep' = {
  name: 'monitoring'
  params: {
    location: location
    logAnalyticsName: logAnalyticsName
    appInsightsName: appInsightsName
    tags: tags
  }
}

module appservice 'modules/appservice.bicep' = {
  name: 'appservice'
  params: {
    location: location
    appServicePlanName: appServicePlanName
    appServiceName: appServiceName
    acrLoginServer: acr.outputs.acrLoginServer
    dockerImageName: 'gop-api'
    dockerImageTag: dockerImageTag
    identityId: identity.outputs.identityId
    identityClientId: identity.outputs.identityClientId
    keyVaultUri: keyvault.outputs.keyVaultUri
    keyVaultName: keyvault.outputs.keyVaultName
    appInsightsConnectionString: monitoring.outputs.appInsightsConnectionString
    tags: tags
  }
}

// ── Outputs ──────────────────────────────────────────────────────────────────

output appServiceUrl string = appservice.outputs.appServiceUrl
output acrLoginServer string = acr.outputs.acrLoginServer
output keyVaultUri string = keyvault.outputs.keyVaultUri
output appInsightsConnectionString string = monitoring.outputs.appInsightsConnectionString
output sqlServerFqdn string = sql.outputs.sqlServerFqdn
output identityClientId string = identity.outputs.identityClientId
output resourceGroupName string = resourceGroup().name
