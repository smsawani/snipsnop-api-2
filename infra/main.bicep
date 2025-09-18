metadata description = 'Provisions resources for a web application that uses Azure SDK for .NET to connect to Azure Cosmos DB for NoSQL.'

targetScope = 'resourceGroup'

@minLength(1)
@maxLength(64)
@description('Name of the environment that can be used as part of naming resource convention.')
param environmentName string

@minLength(1)
@description('Primary location for all resources.')
param location string

@description('Id of the principal to assign database and application roles.')
param deploymentUserPrincipalId string = ''

// serviceName is used as value for the tag (azd-service-name) azd uses to identify deployment host
param serviceName string = 'api'

var resourceToken = toLower(uniqueString(resourceGroup().id, environmentName, location))
var tags = {
  'azd-env-name': environmentName
  repo: 'https://github.com/smsawani/snipsnop-api-2'
}

module managedIdentity 'br/public:avm/res/managed-identity/user-assigned-identity:0.4.0' = {
  name: 'user-assigned-identity'
  params: {
    name: 'managed-identity-${resourceToken}'
    location: location
    tags: tags
  }
}

var databaseName = 'snipsnop'
var containerName = 'snips'

module cosmosDbAccount 'br/public:avm/res/document-db/database-account:0.8.1' = {
  name: 'cosmos-db-account'
  params: {
    name: 'cosmos-db-nosql-${resourceToken}'
    location: location
    locations: [
      {
        failoverPriority: 0
        locationName: location
        isZoneRedundant: false
      }
    ]
    tags: tags
    disableKeyBasedMetadataWriteAccess: true
    disableLocalAuth: true
    networkRestrictions: {
      publicNetworkAccess: 'Enabled'
      ipRules: []
      virtualNetworkRules: []
    }
    capabilitiesToAdd: [
      'EnableServerless'
    ]
    sqlRoleDefinitions: [
      {
        name: 'nosql-data-plane-contributor'
        dataAction: [
          'Microsoft.DocumentDB/databaseAccounts/readMetadata'
          'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/items/*'
          'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/*'
        ]
      }
    ]
    sqlRoleAssignmentsPrincipalIds: union(
      [
        managedIdentity.outputs.principalId
      ],
      !empty(deploymentUserPrincipalId) ? [deploymentUserPrincipalId] : []
    )
    sqlDatabases: [
      {
        name: databaseName
        containers: [
          {
            name: containerName
            paths: [
              '/userId'
            ]
          }
        ]
      }
    ]
  }
}

module logAnalyticsWorkspace 'br/public:avm/res/operational-insights/workspace:0.7.0' = {
  name: 'log-analytics-workspace'
  params: {
    name: 'log-analytics-${resourceToken}'
    location: location
    tags: tags
  }
}

module applicationInsights 'br/public:avm/res/insights/component:0.4.2' = {
  name: 'application-insights'
  params: {
    name: 'appinsights-${resourceToken}'
    location: location
    tags: tags
    workspaceResourceId: logAnalyticsWorkspace.outputs.resourceId
  }
}

module hostingPlan 'br/public:avm/res/web/serverfarm:0.3.0' = {
  name: 'hosting-plan'
  params: {
    name: 'plan-${resourceToken}'
    location: location
    tags: tags
    skuName: 'B1' // Consumption plan for Function Apps
  }
}

var storageAccountName = 'st${resourceToken}'

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageAccountName
  location: location
  tags: tags
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    allowBlobPublicAccess: false
    publicNetworkAccess: 'Enabled'
  }
}

module functionApp 'br/public:avm/res/web/site:0.11.1' = {
  name: 'function-app'
  params: {
    name: 'func-${resourceToken}'
    location: location
    tags: union(tags, { 'azd-service-name': serviceName })
    kind: 'functionapp'
    serverFarmResourceId: hostingPlan.outputs.resourceId
    managedIdentities: {
      systemAssigned: false
      userAssignedResourceIds: [
        managedIdentity.outputs.resourceId
      ]
    }
    appSettingsKeyValuePairs: {
      AzureWebJobsStorage: 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};EndpointSuffix=${environment().suffixes.storage};AccountKey=${listKeys(storageAccount.id, '2023-01-01').keys[0].value}'
      WEBSITE_CONTENTAZUREFILECONNECTIONSTRING: 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};EndpointSuffix=${environment().suffixes.storage};AccountKey=${listKeys(storageAccount.id, '2023-01-01').keys[0].value}'
      WEBSITE_CONTENTSHARE: toLower('func-${resourceToken}')
      FUNCTIONS_EXTENSION_VERSION: '~4'
      FUNCTIONS_WORKER_RUNTIME: 'dotnet-isolated'
      APPINSIGHTS_INSTRUMENTATIONKEY: applicationInsights.outputs.instrumentationKey
      APPLICATIONINSIGHTS_CONNECTION_STRING: applicationInsights.outputs.connectionString
      'CONFIGURATION__AZURECOSMOSDB__ENDPOINT': cosmosDbAccount.outputs.endpoint
      'CONFIGURATION__AZURECOSMOSDB__DATABASENAME': databaseName
      'CONFIGURATION__AZURECOSMOSDB__CONTAINERNAME': containerName
      AZURE_CLIENT_ID: managedIdentity.outputs.clientId
    }
  }
}

// Azure Cosmos DB for Table outputs
output CONFIGURATION__AZURECOSMOSDB__ENDPOINT string = cosmosDbAccount.outputs.endpoint
output CONFIGURATION__AZURECOSMOSDB__DATABASENAME string = databaseName
output CONFIGURATION__AZURECOSMOSDB__CONTAINERNAME string = containerName

// Azure Function App outputs
output AZURE_FUNCTION_APP_NAME string = functionApp.outputs.name
output AZURE_FUNCTION_APP_URL string = 'https://${functionApp.outputs.defaultHostname}'
