variable "name" {
  type        = string
  description = "Name of the container app"
}

variable "image" {
  type        = string
  description = "Container image to deploy"
}

variable "container_app_env_id" {
  type        = string
  description = "ID of the container app environment"
}

variable "resource_group_name" {
  type        = string
  description = "Name of the resource group"
  default     = "TicTacToe"
}

variable "registry_server" {
  type        = string
  description = "Container registry server URL"
}

variable "registry_username" {
  type        = string
  description = "Username for container registry"
}

variable "registry_password" {
  type        = string
  description = "Password for container registry"
  sensitive   = true
}

variable "signalr_connection_string" {
  type        = string
  description = "SignalR connection string"
  sensitive   = true
}

variable "storage_connection_string" {
  type        = string
  description = "Storage account connection string"
  sensitive   = true
}

variable "extra_env_vars" {
  type        = map(string)
  description = "Extra environment variables"
  default     = {}
}
