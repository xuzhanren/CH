# Azure Infrastructure Creation using Terraform 

Helpful Terraform Links:
- [Terraform Language Documentation](https://www.terraform.io/docs/language/index.html)
- [Resource: azure_registry](https://registry.terraform.io/namespaces/Azure)

## Step 0: Initialize Terraform
```
terraform init
```

## Step 1: Plan Resources
```
terraform plan -var-file="vars/canadacentral.tfvars"
```

## Step 2: Apply Resources
```
terraform apply -var-file="vars/canadacentral.tfvars"
```

## Step 3: Cleanup Terraform Resources
```
terraform destroy -var-file="vars/canadacentral.tfvars"
