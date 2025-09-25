terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~>3.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~>3.0"
    }
  }
}

provider "azurerm" {
  features {}
}

data "azurerm_client_config" "current" {}

data "azurerm_resource_group" "main" {
  name = var.resource_group_name
}

resource "random_string" "resource_token" {
  length  = 13
  special = false
  upper   = false
}

locals {
  resource_token = random_string.resource_token.result
  tags = {
    "azd-env-name" = var.environment_name
    "repo"         = "https://github.com/smsawani/snipsnop-api-2"
  }
  database_name  = "snipsnop"
  container_name = "snips"
}

resource "azurerm_user_assigned_identity" "managed_identity" {
  name                = "managed-identity-${local.resource_token}"
  location            = var.location
  resource_group_name = data.azurerm_resource_group.main.name
  tags                = local.tags
}

resource "azurerm_cosmosdb_account" "cosmos_db_account" {
  name                = "cosmos-db-nosql-${local.resource_token}"
  location            = var.location
  resource_group_name = data.azurerm_resource_group.main.name
  offer_type          = "Standard"
  kind                = "GlobalDocumentDB"

  capabilities {
    name = "EnableServerless"
  }

  consistency_policy {
    consistency_level = "Session"
  }

  geo_location {
    location          = var.location
    failover_priority = 0
    zone_redundant    = false
  }

  public_network_access_enabled = true
  is_virtual_network_filter_enabled = false
  key_vault_key_id = null

  tags = local.tags
}

resource "azurerm_cosmosdb_sql_database" "database" {
  name                = local.database_name
  resource_group_name = data.azurerm_resource_group.main.name
  account_name        = azurerm_cosmosdb_account.cosmos_db_account.name
}

resource "azurerm_cosmosdb_sql_container" "container" {
  name                = local.container_name
  resource_group_name = data.azurerm_resource_group.main.name
  account_name        = azurerm_cosmosdb_account.cosmos_db_account.name
  database_name       = azurerm_cosmosdb_sql_database.database.name
  partition_key_path  = "/userId"
}

resource "azurerm_cosmosdb_sql_role_definition" "cosmos_role" {
  name                = "nosql-data-plane-contributor"
  resource_group_name = data.azurerm_resource_group.main.name
  account_name        = azurerm_cosmosdb_account.cosmos_db_account.name
  type                = "CustomRole"
  assignable_scopes   = [azurerm_cosmosdb_account.cosmos_db_account.id]

  permissions {
    data_actions = [
      "Microsoft.DocumentDB/databaseAccounts/readMetadata",
      "Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/items/*",
      "Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers/*"
    ]
  }
}

resource "azurerm_cosmosdb_sql_role_assignment" "cosmos_role_assignment_managed_identity" {
  resource_group_name = data.azurerm_resource_group.main.name
  account_name        = azurerm_cosmosdb_account.cosmos_db_account.name
  role_definition_id  = azurerm_cosmosdb_sql_role_definition.cosmos_role.id
  principal_id        = azurerm_user_assigned_identity.managed_identity.principal_id
  scope               = azurerm_cosmosdb_account.cosmos_db_account.id
}

resource "azurerm_cosmosdb_sql_role_assignment" "cosmos_role_assignment_deployment_user" {
  count               = var.deployment_user_principal_id != "" ? 1 : 0
  resource_group_name = data.azurerm_resource_group.main.name
  account_name        = azurerm_cosmosdb_account.cosmos_db_account.name
  role_definition_id  = azurerm_cosmosdb_sql_role_definition.cosmos_role.id
  principal_id        = var.deployment_user_principal_id
  scope               = azurerm_cosmosdb_account.cosmos_db_account.id
}

resource "azurerm_log_analytics_workspace" "log_analytics_workspace" {
  name                = "log-analytics-${local.resource_token}"
  location            = var.location
  resource_group_name = data.azurerm_resource_group.main.name
  sku                 = "PerGB2018"
  retention_in_days   = 30
  tags                = local.tags
}

resource "azurerm_application_insights" "application_insights" {
  name                = "appinsights-${local.resource_token}"
  location            = var.location
  resource_group_name = data.azurerm_resource_group.main.name
  workspace_id        = azurerm_log_analytics_workspace.log_analytics_workspace.id
  application_type    = "web"
  tags                = local.tags
}

resource "azurerm_service_plan" "hosting_plan" {
  name                = "plan-${local.resource_token}"
  location            = var.location
  resource_group_name = data.azurerm_resource_group.main.name
  os_type             = "Windows"
  sku_name            = "B1"
  tags                = local.tags
}

resource "azurerm_storage_account" "storage_account" {
  name                     = "st${local.resource_token}"
  location                 = var.location
  resource_group_name      = data.azurerm_resource_group.main.name
  account_tier             = "Standard"
  account_replication_type = "LRS"
  account_kind             = "StorageV2"

  allow_nested_items_to_be_public = false
  public_network_access_enabled   = true

  tags = local.tags
}

resource "azurerm_windows_function_app" "function_app" {
  name                = "func-${local.resource_token}"
  location            = var.location
  resource_group_name = data.azurerm_resource_group.main.name
  service_plan_id     = azurerm_service_plan.hosting_plan.id
  storage_account_name       = azurerm_storage_account.storage_account.name
  storage_account_access_key = azurerm_storage_account.storage_account.primary_access_key

  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.managed_identity.id]
  }

  site_config {
    application_insights_key               = azurerm_application_insights.application_insights.instrumentation_key
    application_insights_connection_string = azurerm_application_insights.application_insights.connection_string
  }

  app_settings = {
    "AzureWebJobsStorage"                          = "DefaultEndpointsProtocol=https;AccountName=${azurerm_storage_account.storage_account.name};EndpointSuffix=core.windows.net;AccountKey=${azurerm_storage_account.storage_account.primary_access_key}"
    "WEBSITE_CONTENTAZUREFILECONNECTIONSTRING"    = "DefaultEndpointsProtocol=https;AccountName=${azurerm_storage_account.storage_account.name};EndpointSuffix=core.windows.net;AccountKey=${azurerm_storage_account.storage_account.primary_access_key}"
    "WEBSITE_CONTENTSHARE"                         = lower("func-${local.resource_token}")
    "FUNCTIONS_EXTENSION_VERSION"                  = "~4"
    "FUNCTIONS_WORKER_RUNTIME"                     = "dotnet-isolated"
    "APPINSIGHTS_INSTRUMENTATIONKEY"               = azurerm_application_insights.application_insights.instrumentation_key
    "APPLICATIONINSIGHTS_CONNECTION_STRING"        = azurerm_application_insights.application_insights.connection_string
    "CONFIGURATION__AZURECOSMOSDB__ENDPOINT"       = azurerm_cosmosdb_account.cosmos_db_account.endpoint
    "CONFIGURATION__AZURECOSMOSDB__DATABASENAME"   = local.database_name
    "CONFIGURATION__AZURECOSMOSDB__CONTAINERNAME"  = local.container_name
    "AZURE_CLIENT_ID"                              = azurerm_user_assigned_identity.managed_identity.client_id
  }

  tags = merge(local.tags, {
    "azd-service-name" = var.service_name
  })
}