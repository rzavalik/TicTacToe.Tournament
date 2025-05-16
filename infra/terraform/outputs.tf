output "acr_login_server" {
  value = azurerm_container_registry.tictactoe_acr.login_server
}

output "acr_admin_username" {
  value = azurerm_container_registry.tictactoe_acr.admin_username
  sensitive = true
}

output "acr_admin_password" {
  value     = azurerm_container_registry.tictactoe_acr.admin_password
  sensitive = true
}

output "signalr_connection_string" {
  value     = azurerm_signalr_service.tictactoe_signalr.primary_connection_string
  sensitive = true
}

output "storage_connection_string" {
  value     = azurerm_storage_account.tictactoeresources.primary_connection_string
  sensitive = true
}

output "server_url" {
  description = "FQDN of the tictactoe-server app"
  value       = "https://${module.tictactoe_server.fqdn}"
}

output "webui_url" {
  description = "FQDN of the tictactoe-webui app"
  value       = "https://${module.tictactoe_webui.fqdn}"
}

output "webui_hub_url" {
  value = "https://${module.tictactoe_server.fqdn}/tournamentHub"
}

output "signalr_endpoint" {
  description = "SignalR service endpoint"
  value       = "https://${azurerm_signalr_service.tictactoe_signalr.name}.service.signalr.net"
}