# Deploying ArgoCD on Azure Kubernetes Service (AKS) using Terraform 

## Introduction
This guide provides step-by-step instructions to deploy ArgoCD on AKS using Terraform, facilitating a GitOps workflow on Azure.

## Prerequisites
- **Kubectl**: Installed. Download here: [kubectl for Windows](https://dl.k8s.io/release/v1.28.9/bin/windows/amd64/kubectl.exe)
- **Azure CLI**: Installed and authenticated with `az login`.
- **AKS Credentials (AKS is up and running)**: Obtain with `az aks get-credentials --resource-group myAksResourceGroup --name myAksCluster`.
- **Visual Studio Code Base64 Decode and Encode Extension**: Installed for handling base64 decodings. Will be used to decode the initial ArgoCD admin password.

## Terraform Workflow
```bash
# Step 1: Initialize your Terraform workspace
terraform init

# Step 2: Generate and show an execution plan
terraform plan

# Step 3: Apply the changes required to reach the desired state of the configuration
terraform apply

# Step 4: Retrieve the ArgoCD default admin(username) password and decode it using Base64 extension
kubectl -n argocd get secret argocd-initial-admin-secret -o jsonpath="{.data.password}"

# Step 5: Destroy all resources created by the Terraform configuration
terraform destroy
```

Helpful Terraform Links:
- [Terraform Language Documentation](https://www.terraform.io/docs/language/index.html)
- [Resource: azure_registry](https://registry.terraform.io/namespaces/Azure)

