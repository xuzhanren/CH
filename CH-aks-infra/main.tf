# main.tf

provider "azurerm" {
  features {
    resource_group {
      prevent_deletion_if_contains_resources = false
    }
  }
  #skip_provider_registration = true
}

resource "azurerm_resource_group" "ch_aks_rg" {
  name     = var.resource_group_name
  location = var.resource_group_location
}

resource "azurerm_kubernetes_cluster" "ch_aks_cluster" {
  name                = var.aks_cluster_name
  location            = azurerm_resource_group.ch_aks_rg.location
  resource_group_name = azurerm_resource_group.ch_aks_rg.name
  dns_prefix          = var.dns_prefix

  default_node_pool {
    name       = "system"
    node_count = 1
    vm_size    = var.vm_size  # Uses the VM size variable
  }

  node_provisioning_profile {
    mode = "Manual" # Or "Auto" depending on whether you are using Node Auto-Provisioning (NAP)
  }
  identity {
    type = "SystemAssigned"
  }

  network_profile {
    network_plugin = "azure"
  }
}

resource "azurerm_kubernetes_cluster_node_pool" "ch_user_pool" {
  name                  = "userpool"
  kubernetes_cluster_id = azurerm_kubernetes_cluster.ch_aks_cluster.id
  vm_size               = var.vm_size  # Uses the same VM size variable
  node_count            = 1
  mode                  = "User"

  node_labels = {
    "nodepool-type" = "user-nodes"
  }
}

# Storage account holding the media folders that used to live under CanHappy/wwwroot.
# Each folder is a blob container with anonymous read access so the site can reference
# the images/videos by URL: https://<account>.blob.core.windows.net/<container>/<file>
resource "azurerm_storage_account" "ch_images" {
  name                            = var.storage_account_name
  resource_group_name             = azurerm_resource_group.ch_aks_rg.name
  location                        = azurerm_resource_group.ch_aks_rg.location
  account_tier                    = "Standard"
  account_replication_type        = "LRS"
  account_kind                    = "StorageV2"
  min_tls_version                 = "TLS1_2"
  https_traffic_only_enabled      = true
  allow_nested_items_to_be_public = true

  lifecycle {
    prevent_destroy = false
  }

# Lets the browser-side photo editors load existing images from another origin.
  blob_properties {
    cors_rule {
      allowed_origins    = ["*"]
      allowed_methods    = ["GET", "HEAD"]
      allowed_headers    = ["*"]
      exposed_headers    = ["*"]
      max_age_in_seconds = 3600
    }
  }
}

resource "azurerm_storage_container" "ch_media" {
  for_each              = toset(var.storage_folders)
  name                  = each.value
  storage_account_id    = azurerm_storage_account.ch_images.id
  container_access_type = "blob"
}