param  appServicePlanName  string = 'asp-mockapi'
param webAppName string = 'app-mockapi'
param storageAccountName string = 'stmockapi001'
param logAnalyticsWorkspaceName string = 'logs-mockapi'
param applicationInsightsName string = 'ai-mockapi'
param location string = resourceGroup().location

resource logAnalyticsWorkspace'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: logAnalyticsWorkspaceName
  location: location
  properties: {
    retentionInDays: 30
    sku: {
      name: 'PerGB2018'
    }
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: applicationInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    DisableIpMasking: false
    IngestionMode: 'LogAnalytics'
    Request_Source: 'rest'
    RetentionInDays: 90
    SamplingPercentage: json('50')
    WorkspaceResourceId: logAnalyticsWorkspace.id
  }
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2021-04-01' = {
  name: storageAccountName
  location: location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
  properties: {
    accessTier: 'Hot'
  }
}

resource mockcallsShare 'Microsoft.Storage/storageAccounts/fileServices/shares@2021-04-01' = {
  name: '${storageAccount.name}/default/mockcalls'
}

resource endpointsShare 'Microsoft.Storage/storageAccounts/fileServices/shares@2021-04-01' = {
  name: '${storageAccount.name}/default/endpoints'
}


resource  appServicePlan  'Microsoft.Web/serverfarms@2020-12-01' = {
   name: appServicePlanName
   location: location
   kind: 'linux'
   properties: {
      reserved: true
  }
sku: {
   name: 'B1'
   tier: 'Basic'
  }
}

resource  webApp  'Microsoft.Web/sites@2021-01-01' = {
  name: webAppName
  location: location
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      appSettings: [{
        name: 'Endpoints__ApiDefSubFolderName'
        value: 'Endpoints'
      }
      {
        name: 'Endpoints__MockApiCallsSubFolder'
        value: 'MockCalls'
      }
      {
        name: 'Endpoints__APIs__0__SwaggerLocation'
        value: 'petstore/swagger.json'
      }
      {
        name: 'Endpoints__APIs__0__ApiName'
        value: 'petstore'
      }
      {
        name: 'Endpoints__APIs__1__SwaggerLocation'
        value: 'https://app.swaggerhub.com/apiproxy/registry/TSAISIDOROS/SySkaki/1.0.0'
      }
      {
        name: 'Endpoints__APIs__1__ApiName'
        value: 'chess'
      }
      {
        name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
        value: appInsights.properties.InstrumentationKey
      }]
      linuxFxVersion: 'DOCKER|adipa/mock-rest-api:latest'
    }
  }
}

resource storageSetting 'Microsoft.Web/sites/config@2021-01-15' = {
  name: 'azurestorageaccounts'
  parent: webApp
  properties: {
    mockcalls: {
      type: 'AzureFiles'
      shareName: 'mockcalls'
      mountPath: '/app/MockCalls'
      accountName: storageAccount.name
      accessKey: storageAccount.listKeys().keys[0].value
    }
    endpoints: {
      type: 'AzureFiles'
      shareName: 'endpoints'
      mountPath: '/app/Endpoints'
      accountName: storageAccount.name
      accessKey: storageAccount.listKeys().keys[0].value
    }
  }
}
