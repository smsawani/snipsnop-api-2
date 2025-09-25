output "CONFIGURATION__AZURECOSMOSDB__ENDPOINT" {
  description = "Azure Cosmos DB endpoint URL"
  value       = azurerm_cosmosdb_account.cosmos_db_account.endpoint
}

output "CONFIGURATION__AZURECOSMOSDB__DATABASENAME" {
  description = "Azure Cosmos DB database name"
  value       = local.database_name
}

output "CONFIGURATION__AZURECOSMOSDB__CONTAINERNAME" {
  description = "Azure Cosmos DB container name"
  value       = local.container_name
}

output "AZURE_FUNCTION_APP_NAME" {
  description = "Name of the Azure Function App"
  value       = azurerm_windows_function_app.function_app.name
}

output "AZURE_FUNCTION_APP_URL" {
  description = "URL of the Azure Function App"
  value       = "https://${azurerm_windows_function_app.function_app.default_hostname}"
}