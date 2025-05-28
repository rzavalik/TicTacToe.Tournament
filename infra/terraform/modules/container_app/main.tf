resource "azurerm_container_app" "this" {
  name                         = var.name
  resource_group_name          = var.resource_group_name
  container_app_environment_id = var.container_app_env_id
  revision_mode                = "Single"

  registry {
    server               = var.registry_server
    username             = var.registry_username
    password_secret_name = "acr-password"
  }

  secret {
    name  = "acr-password"
    value = var.registry_password
  }

  ingress {
    external_enabled = true
    target_port      = 80
    transport        = "auto"
    traffic_weight {
      percentage      = 100
      latest_revision = true
    }
  }

  template {
    min_replicas = 1
    max_replicas = 1

    container {
      name   = var.name
      image  = var.image
      cpu    = 0.25
      memory = "0.5Gi"

      env {
        name  = "Azure__SignalR__ConnectionString"
        value = var.signalr_connection_string
      }

      env {
        name  = "Azure__Storage__ConnectionString"
        value = var.storage_connection_string
      }

      dynamic "env" {
        for_each = var.extra_env_vars
        content {
          name  = env.key
          value = env.value
        }
      }
    }
  }
}