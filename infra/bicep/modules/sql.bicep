// T-INFRA-04: Azure SQL Server + Database Serverless
// GP_S_Gen5_1, auto-pause 60min, RN-INFRA-02

@description('Location for all resources')
param location string

@description('SQL Server name (globally unique)')
param sqlServerName string

@description('SQL Database name')
param sqlDatabaseName string

@description('SQL Admin login (stored in KV, not in DB firewall for users)')
param sqlAdminLogin string

@secure()
@description('SQL Admin password')
param sqlAdminPassword string

@description('Development IP to whitelist in firewall (optional)')
param devIpAddress string = ''

@description('Tags to apply to resources')
param tags object = {}

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: sqlServerName
  location: location
  tags: tags
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    version: '12.0'
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

// Allow Azure Services through firewall (required for App Service to connect)
resource firewallAllowAzureServices 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: sqlServer
  name: 'AllowAllWindowsAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// Optional dev IP whitelist
resource firewallDevIp 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = if (!empty(devIpAddress)) {
  parent: sqlServer
  name: 'DevMachine'
  properties: {
    startIpAddress: devIpAddress
    endIpAddress: devIpAddress
  }
}

// Serverless database: GP_S_Gen5_1, auto-pause 60min
resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: sqlDatabaseName
  location: location
  tags: tags
  sku: {
    name: 'GP_S_Gen5'
    tier: 'GeneralPurpose'
    family: 'Gen5'
    capacity: 1
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 34359738368  // 32 GB
    autoPauseDelay: 60         // RN-INFRA-02: auto-pause after 60 min
    minCapacity: json('0.5')
    readScale: 'Disabled'
    zoneRedundant: false
    requestedBackupStorageRedundancy: 'Local'  // cheapest backup
  }
}

output sqlServerId string = sqlServer.id
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output sqlServerName string = sqlServer.name
output sqlDatabaseName string = sqlDatabase.name
