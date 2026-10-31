# variables.tf

variable "resource_group_name" {
  description = "The name of the resource group"
  type        = string
}

variable "resource_group_location" {
  description = "The location of the resource group"
  type        = string
}

variable "aks_cluster_name" {
  description = "The name of the AKS cluster"
  type        = string
}

variable "dns_prefix" {
  description = "DNS prefix for the AKS cluster"
  type        = string
}

variable "storage_account_name" {
  description = "Globally unique name of the storage account holding the CH media folders"
  type        = string
  default     = "stchimages"
}

variable "storage_folders" {
  description = "Blob container (folder) names; lowercase because Azure container names must be lowercase"
  type        = list(string)
  default = [
    "images",
    "estateroomimages",
    "listingimages",
    "listingreviewimages",
    "listingvideos",
    "listingvideothumbnails",
  ]
}

variable "vm_size" {
  description = "The size of the VM instances in the AKS cluster"
  type        = string
}
