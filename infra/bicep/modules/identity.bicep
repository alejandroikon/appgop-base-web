// T-INFRA-01: User-Assigned Managed Identity
// Roles: AcrPull (on ACR) + Key Vault Secrets User (on KV)

@description('Location for all resources')
param location string

@description('Name for the managed identity')
param identityName string

@description('Tags to apply to resources')
param tags object = {}

resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: identityName
  location: location
  tags: tags
}

output identityId string = managedIdentity.id
output identityClientId string = managedIdentity.properties.clientId
output identityPrincipalId string = managedIdentity.properties.principalId
