// T-INFRA-03: Azure Key Vault (Standard, RBAC-based, soft-delete 7d)
// Secrets: ConnectionStrings--DefaultConnection, JwtSettings--SigningKey

@description('Location for all resources')
param location string

@description('Key Vault name (3-24 chars, globally unique)')
param keyVaultName string

@description('Principal ID of the managed identity that needs Key Vault Secrets User')
param identityPrincipalId string

@description('Tenant ID for Key Vault')
param tenantId string = tenant().tenantId

@description('Tags to apply to resources')
param tags object = {}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  tags: tags
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: tenantId
    enableRbacAuthorization: true   // Use RBAC instead of access policies
    enableSoftDelete: true
    softDeleteRetentionInDays: 7    // RN-SEC-03: minimum 7d
    enabledForDeployment: false
    enabledForDiskEncryption: false
    enabledForTemplateDeployment: true
    publicNetworkAccess: 'Enabled'
    networkAcls: {
      bypass: 'AzureServices'
      defaultAction: 'Allow'
    }
  }
}

// Key Vault Secrets User role (read secrets)
var kvSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'

resource kvSecretsUserRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, identityPrincipalId, kvSecretsUserRoleId)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', kvSecretsUserRoleId)
    principalId: identityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

output keyVaultId string = keyVault.id
output keyVaultUri string = keyVault.properties.vaultUri
output keyVaultName string = keyVault.name
