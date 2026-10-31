<#
.SYNOPSIS
    Downloads every container (folder) and blob from the stchimages Azure storage account to a local folder.

.DESCRIPTION
    - Each blob container becomes a sub-folder of -LocalRoot (same names, same nested folders).
    - Existing local files with the same name are overwritten.
    - Nothing is deleted in Azure, and nothing is deleted locally (files that exist only locally are left alone).

.PARAMETER StorageAccountName
    Storage account to read from. Default: stchimages.

.PARAMETER ResourceGroupName
    Resource group of the storage account. Only used with -AuthMode key. Default: chAksResourceGroup.

.PARAMETER SubscriptionId
    Azure subscription. Default: 0c125355-a538-4fcc-b71b-92863bcde349.

.PARAMETER LocalRoot
    Destination folder. Default: C:\DR\Projects\VSC\CH\AzureCHImagesStorageAcct.

.PARAMETER AuthMode
    key   (default) uses the storage account key, looked up with your signed-in az account.
    login uses your Entra ID account; it needs the "Storage Blob Data Reader" role (or higher) on the account.

.EXAMPLE
    .\PSDownloadImagesFromAzureToLocal.ps1

.EXAMPLE
    .\PSDownloadImagesFromAzureToLocal.ps1 -AuthMode login
#>
[CmdletBinding()]
param(
    [string]$StorageAccountName = "stchimages",
    [string]$ResourceGroupName  = "chAksResourceGroup",
    [string]$SubscriptionId     = "0c125355-a538-4fcc-b71b-92863bcde349",
    [string]$LocalRoot          = "C:\DR\Projects\VSC\CH\AzureCHImagesStorageAcct",
    [ValidateSet("key", "login")]
    [string]$AuthMode           = "key"
)

$ErrorActionPreference = "Stop"

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
    $containers = @(az storage container list @authArgs --query "[].name" -o tsv --only-show-errors)
    if ($LASTEXITCODE -ne 0) { throw "Could not list containers in '$StorageAccountName'." }

    if (-not $containers -or $containers.Count -eq 0) {
        Write-Warning "No containers found in '$StorageAccountName'. Nothing to download."
        return
    }

    New-Item -ItemType Directory -Path $LocalRoot -Force | Out-Null
    Write-Host "Downloading $($containers.Count) container(s) from '$StorageAccountName' to '$LocalRoot'" -ForegroundColor Cyan

    $totalFiles = 0
    foreach ($container in $containers) {
        $target = Join-Path $LocalRoot $container
        New-Item -ItemType Directory -Path $target -Force | Out-Null

        Write-Host "  [$container] -> $target"
        az storage blob download-batch @authArgs --source $container --destination $target --overwrite true --no-progress --only-show-errors -o none
        if ($LASTEXITCODE -ne 0) { throw "Download failed for container '$container'." }

        $count = (Get-ChildItem -Path $target -Recurse -File | Measure-Object).Count
        $totalFiles += $count
        Write-Host "      $count file(s) now in local folder"
    }

    Write-Host "Done. $totalFiles file(s) in '$LocalRoot'. Nothing was deleted in Azure or locally." -ForegroundColor Green
}
finally {
    Remove-Item Env:AZURE_STORAGE_KEY -ErrorAction SilentlyContinue
}
