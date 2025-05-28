variable "resource_group_name" {
  description = "The name of the Azure Resource Group."
  type        = string
  default     = "TicTacToe"
}

variable "location" {
  description = "Azure location for the resource group"
  type        = string
  default     = "eastus2"
}

variable "storage_account_name" {
  description = "The name of the Azure Storage Account used for storing game data and Terraform state."
  type        = string
  default     = "tictactoeresources"
}

variable "container_app_names" {
  description = "List of existing Azure Container Apps to reference."
  type        = list(string)
  default     = ["tictactoe-server", "tictactoe-webui"]
}

variable "acr_name" {
  description = "Name of the Azure Container Registry"
  type        = string
  default     = "tictactoeacr"
}

variable "acr_sku" {
  description = "SKU of the ACR (Basic, Standard, Premium)"
  type        = string
  default     = "Basic"
}

variable "log_analytics_workspace_name" {
  description = "Name of the Log Analytics workspace"
  type        = string
  default	  = "workspace-icacoenLKK"
}

variable "signalr_name" {
  description = "Name of the Azure SignalR Service"
  type        = string
  default	  = "tictactoe-signalr"
}

variable "container_app_env_name" {
  description = "Name of the Container App Environment"
  type        = string
  default	  = "tictactoe-env"
}

variable "server_image" {
  type        = string
  description = "The container image for the tictactoe-server app"
}

variable "webui_image" {
  type        = string
  description = "The container image for the tictactoe-webui app"
}