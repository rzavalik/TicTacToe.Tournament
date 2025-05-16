# TicTacToe Tournament - Infrastructure Deployment

This folder contains the Terraform configuration required to deploy the TicTacToe Tournament backend and Web UI on Microsoft Azure.

## ✅ Prerequisites

Before you begin, ensure the following tools and resources are set up:

- [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli)
- [Terraform CLI](https://developer.hashicorp.com/terraform/downloads)
- [Docker](https://www.docker.com/products/docker-desktop)
- An **active Azure Subscription**
- An **Azure Blob Storage account with a container for Terraform state**

---

## 1. Clone the Repository

```bash
git clone https://github.com/rzavalik/TicTacToe.Tournament.git
cd TicTacToe.Tournament/infra
```

---

## 2. Prepare Remote State (Blob Storage)

Terraform uses a remote backend to store state. This setup uses Azure Blob Storage.

### a. Create a Storage Account (if needed)

Please, create the Storage Account in the same region you are going to deploy the app. So mind the --location parameter, it should be the same as you are going to provide in step 5.

```bash
az storage account create \
  --name <yourstorageaccount> \
  --resource-group <yourresourcegroup> \
  --location "eastus2" \
  --sku Standard_LRS
```

### b. Create a Container for Terraform State

```bash
az storage container create \
  --name terraform \
  --account-name <yourstorageaccount>
```

---

## 3. Update `backend.tf`

Edit `backend.tf` and replace the values below with your actual configuration:

```hcl
terraform {
  backend "azurerm" {
    resource_group_name  = "<yourresourcegroup>"
    storage_account_name = "<yourstorageaccount>"
    container_name       = "terraform"
    key                  = "tictactoe.terraform.tfstate"
  }
}
```

---

## 4. Update Azure Subscription ID

Edit the `provider.tf` file and replace the `subscription_id` with your own:

```hcl
provider "azurerm" {
  features        = {}
  subscription_id = "00000000-0000-0000-0000-000000000000" # <- replace this
}
```

You can retrieve your current subscription ID by running:

```bash
az account show --query id -o tsv
```

---

## 5. Configure Terraform Variables

Copy the example variables file and update it with your values:

```bash
cp terraform.tfvars.example terraform.tfvars
```

Edit `terraform.tfvars` with the appropriate settings:

```hcl
resource_group_name = "your-resource-group"
location            = "eastus2"
container_registry  = "youracrname"
project_name        = "tictactoe"
```

---

## 6. Initialize Terraform

```bash
terraform init
```

---

## 7. Review the Plan

```bash
terraform plan
```

---

## 8. Apply the Deployment

```bash
terraform apply
```

Type `yes` when prompted to confirm the changes.

---

## 9. Output Values

After successful deployment, Terraform will output:

- `server_url`: Public endpoint for the Tournament backend
- `webui_url`: Public endpoint for the Web UI
- `webui_hub_url`: SignalR endpoint used by players

These values must be used to configure the apps (`DumbPlayer`, `SmartPlayer`, and `OpenAIClientPlayer`) in their respective `appSettings.json`.

---

## 🧪 Deploy Players and Web UI

After deployment, return to the root project and follow the CI or manual instructions to:

- Build and publish Docker images
- Update appSettings dynamically
- Launch `DumbPlayer`, `SmartPlayer`, and `OpenAIClientPlayer` with the correct configuration

---

## 🔁 Destroy Infrastructure (Optional)

To destroy all resources created by Terraform:

```bash
terraform destroy
```

---

## 🆘 Troubleshooting

- **Authentication Failed**: Run `az login` again and make sure your subscription is active.
- **State Lock Issues**: Check if another Terraform process is running or delete lock files manually in Azure Blob.
- **Docker Errors**: Ensure you're logged into ACR: `az acr login --name <youracrname>`

---

Happy deploying! 🚀