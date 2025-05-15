# Gets the existing blog storage
resource "azurerm_storage_account" "tictactoeresources" {
  name                     = "tictactoeresources"
  resource_group_name      = azurerm_resource_group.tictactoe_rg.name
  location                 = azurerm_resource_group.tictactoe_rg.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
}

# Prepare the Resource Group (tictactoe_rg)
resource "azurerm_resource_group" "tictactoe_rg" {
  name     = var.resource_group_name
  location = var.location
}

# Prepare the Azure ACR to deploy docker images (tictactoe_acr)
resource "azurerm_container_registry" "tictactoe_acr" {
  name                = var.acr_name
  resource_group_name = azurerm_resource_group.tictactoe_rg.name
  location            = azurerm_resource_group.tictactoe_rg.location
  sku                 = var.acr_sku
  admin_enabled       = true
}

# Prepare the Log Analytics (tictactoe_logs)
resource "azurerm_log_analytics_workspace" "tictactoe_logs" {
  name                = var.log_analytics_workspace_name
  location            = azurerm_resource_group.tictactoe_rg.location
  resource_group_name = azurerm_resource_group.tictactoe_rg.name
  sku                 = "PerGB2018"
  retention_in_days   = 30
}

# Prepare the Container Environment (tictactoe_env)
resource "azurerm_container_app_environment" "tictactoe_env" {
  name                       = var.container_app_env_name
  location                   = azurerm_resource_group.tictactoe_rg.location
  resource_group_name        = azurerm_resource_group.tictactoe_rg.name
  log_analytics_workspace_id = azurerm_log_analytics_workspace.tictactoe_logs.id
}

# Prepare the SignalR Service (tictactoe_signalr)
resource "azurerm_signalr_service" "tictactoe_signalr" {
  name                = var.signalr_name
  location            = azurerm_resource_group.tictactoe_rg.location
  resource_group_name = azurerm_resource_group.tictactoe_rg.name
  sku {
    name     = "Standard_S1"
    capacity = 1
  }
  cors {
    allowed_origins = ["*"]
  }
}

# Prepare the Azure Container Apps (tictactoe_server and tictactoe_webui)
module "tictactoe_server" {
  source = "./modules/container_app"

  name                      = "tictactoe-server"
  image                     = var.server_image
  container_app_env_id      = azurerm_container_app_environment.tictactoe_env.id
  registry_server           = azurerm_container_registry.tictactoe_acr.login_server
  registry_username         = azurerm_container_registry.tictactoe_acr.admin_username
  registry_password         = azurerm_container_registry.tictactoe_acr.admin_password
  signalr_connection_string = azurerm_signalr_service.tictactoe_signalr.primary_connection_string
  storage_connection_string = azurerm_storage_account.tictactoeresources.primary_connection_string
}

module "tictactoe_webui" {
  source = "./modules/container_app"

  name                      = "tictactoe-webui"
  image                     = var.webui_image
  container_app_env_id      = azurerm_container_app_environment.tictactoe_env.id
  registry_server           = azurerm_container_registry.tictactoe_acr.login_server
  registry_username         = azurerm_container_registry.tictactoe_acr.admin_username
  registry_password         = azurerm_container_registry.tictactoe_acr.admin_password
  signalr_connection_string = azurerm_signalr_service.tictactoe_signalr.primary_connection_string
  storage_connection_string = azurerm_storage_account.tictactoeresources.primary_connection_string
  extra_env_vars = {
    Azure__SignalR__HubUrl = "https://${module.tictactoe_server.fqdn}/tournamentHub"
    SIGNALR_HUB_URL        = "https://${module.tictactoe_server.fqdn}/tournamentHub"
  }
}