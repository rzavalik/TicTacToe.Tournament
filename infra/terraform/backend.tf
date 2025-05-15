terraform {
  backend "azurerm" {
    resource_group_name  = "TicTacToe"
    storage_account_name = "tictactoeresources"
    container_name       = "terraform"
    key                  = "terraform.tfstate"
  }
}