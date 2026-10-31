<#
.SYNOPSIS
    Uploads every folder and file under a local folder to the stchimages Azure storage account.

.DESCRIPTION
    - Each first-level sub-folder of -LocalRoot is a blob container (same name, lower-cased because
      Azure container names must be lowercase); nested folders and files keep their relative paths.
    - Existing blobs with the same name are overwritten.
    - Nothing is deleted in Azure (blobs that exist only in Azure are left alone), and nothing is deleted locally.
    - Files placed directly in -LocalRoot (not inside a folder) are skipped with a warning.
    - A folder with no matching container is skipped with a warning, unless -CreateMissingContainers is given.

.PARAMETER StorageAccountName
    Storage account to write to. Default: stchimages.

.PARAMETER ResourceGroupName
    Resource group of the storage account. Only used with -AuthMode key. Default: chAksResourceGroup.

.PARAMETER SubscriptionId
    Azure subscription. Default: 0c125355-a538-4fcc-b71b-92863bcde349.

.PARAMETER LocalRoot
    Source folder. Default: C:\DR\Projects\VSC\CH\AzureCHImagesStorageAcct.

.PARAMETER AuthMode
    key   (default) uses the storage account key, looked up with your signed-in az account.
    login uses your Entra ID account; it needs the "Storage Blob Data Contributor" role on the account.

.PARAMETER CreateMissingContainers
    Creates containers that do not exist yet (with anonymous blob read access, like the Terraform-managed ones).
    Containers created this way are not tracked by Terraform.

.EXAMPLE
    .\PSUploadImagesFromLocalToAzure.ps1

.EXAMPLE
    .\PSUploadImagesFromLocalToAzure.ps1 -AuthMode login -CreateMissingContainers
#>
[CmdletBinding()]
param(
    [string]$StorageAccountName = "stchimages",
    [string]$ResourceGroupName  = "chAksResourceGroup",
    [string]$SubscriptionId     = "0c125355-a538-4fcc-b71b-92863bcde349",
    [string]$LocalRoot          = "C:\DR\Projects\VSC\CH\AzureCHImagesStorageAcct",
    [ValidateSet("key", "login")]
    [string]$AuthMode           = "key",
    [switch]$CreateMissingContainers
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $LocalRoot -PathType Container)) {
    throw "Local folder '$LocalRoot' does not exist."
}

if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    throw "Azure CLI ('az') was not found. Install it from https://aka.ms/installazurecliwindows and run 'az login'."
}

az account show --only-show-errors -o none 2>$null
if ($LASTEXITCODE -ne 0) {
    throw "You are not signed in to Azure. Run 'az login' first."
}

az account set --subscription $SubscriptionId --only-show-errors
if ($LASTEXITCODE -ne 0) { throw "Could not select subscription $SubscriptionId." }

$authArgs = @("--account-name", $StorageAccountName, "--auth-mode", $AuthMode)
if ($AuthMode -eq "key") {
    $key = az storage account keys list --account-name $StorageAccountName --resource-group $ResourceGroupName --query "[0].value" -o tsv --only-show-errors
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($key)) {
        throw "Could not read the storage account key. Check the account/resource group names and your permissions, or use -AuthMode login."
    }
    $env:AZURE_STORAGE_KEY = $key
}

try {
    $existing = @(az storage container list @authArgs --query "[].name" -o tsv --only-show-errors)
    if ($LASTEXITCODE -ne 0) { throw "Could not list containers in '$StorageAccountName'." }

    $rootFiles = @(Get-ChildItem -LiteralPath $LocalRoot -File)
    if ($rootFiles.Count -gt 0) {
        Write-Warning "Skipping $($rootFiles.Count) file(s) directly in '$LocalRoot'; put them inside a container folder."
    }

    $folders = @(Get-ChildItem -LiteralPath $LocalRoot -Directory)
    if ($folders.Count -eq 0) {
        Write-Warning "No sub-folders found in '$LocalRoot'. Nothing to upload."
        return
    }

    Write-Host "Uploading $($folders.Count) folder(s) from '$LocalRoot' to '$StorageAccountName'" -ForegroundColor Cyan

    $totalFiles = 0
    foreach ($folder in $folders) {
        $container = $folder.Name.ToLowerInvariant()

        if ($existing -notcontains $container) {
            if (-not $CreateMissingContainers) {
                Write-Warning "Container '$container' does not exist in '$StorageAccountName'; skipping '$($folder.FullName)'. Use -CreateMissingContainers to create it."
                continue
            }

            az storage container create @authArgs --name $container --public-access blob --only-show-errors -o none
            if ($LASTEXITCODE -ne 0) { throw "Could not create container '$container'." }
            Write-Host "  Created container '$container'"
        }

        $count = (Get-ChildItem -LiteralPath $folder.FullName -Recurse -File | Measure-Object).Count
        Write-Host "  [$($folder.Name)] -> $container ($count file(s))"
        if ($count -eq 0) { continue }

        az storage blob upload-batch @authArgs --source $folder.FullName --destination $container --overwrite true --no-progress --only-show-errors -o none
        if ($LASTEXITCODE -ne 0) { throw "Upload failed for folder '$($folder.FullName)'." }

        $totalFiles += $count
    }

    Write-Host "Done. Uploaded $totalFiles file(s). Nothing was deleted in Azure or locally." -ForegroundColor Green
}
finally {
    Remove-Item Env:AZURE_STORAGE_KEY -ErrorAction SilentlyContinue
}
