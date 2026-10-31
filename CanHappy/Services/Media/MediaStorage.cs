using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using CanHappy.Common;

namespace CanHappy.Services.Media;

public sealed class MediaStorage(
    IWebHostEnvironment environment,
    IConfiguration configuration,
    ILogger<MediaStorage> logger) : IMediaStorage
{
    private const string ImmutableCacheControl = "public, max-age=31536000, immutable";

    private readonly Lazy<BlobServiceClient> serviceClient = new(() => CreateServiceClient(configuration));

    public async Task SaveAsync(string webFolder, string fileName, Stream content, string contentType, CancellationToken cancellationToken = default)
    {
        if (!MediaPathHelper.IsAbsoluteUrl(webFolder))
        {
            var folderPath = MediaPathHelper.BuildPhysicalFolderPath(environment.WebRootPath, webFolder);
            Directory.CreateDirectory(folderPath);

            await using var output = File.Create(Path.Combine(folderPath, fileName));
            await content.CopyToAsync(output, cancellationToken);
            return;
        }

        var target = new BlobUriBuilder(new Uri(webFolder));
        EnsureConfiguredAccount(target.AccountName);

        var blobName = string.IsNullOrWhiteSpace(target.BlobName)
            ? fileName
            : $"{target.BlobName.Trim('/')}/{fileName}";

        var blobClient = serviceClient.Value
            .GetBlobContainerClient(target.BlobContainerName)
            .GetBlobClient(blobName);

        await blobClient.UploadAsync(
            content,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
                    CacheControl = ImmutableCacheControl
                }
            },
            cancellationToken);
    }

    public async Task SaveAsync(string webFolder, string fileName, byte[] content, string contentType, CancellationToken cancellationToken = default)
    {
        await using var stream = new MemoryStream(content, writable: false);
        await SaveAsync(webFolder, fileName, stream, contentType, cancellationToken);
    }

    public async Task<bool> TryDeleteBlobAsync(string? mediaUrl, IEnumerable<string> allowedWebFolders, CancellationToken cancellationToken = default)
    {
        if (!MediaPathHelper.IsAbsoluteUrl(mediaUrl))
        {
            return false;
        }

        // AbsoluteUri collapses "../" segments so the prefix check cannot be bypassed.
        var normalizedUrl = new Uri(mediaUrl!).AbsoluteUri;
        var withinAllowedFolder = allowedWebFolders.Any(folder =>
            MediaPathHelper.IsAbsoluteUrl(folder)
            && normalizedUrl.StartsWith(folder.TrimEnd('/') + "/", StringComparison.OrdinalIgnoreCase));

        if (!withinAllowedFolder)
        {
            return false;
        }

        try
        {
            var source = new BlobUriBuilder(new Uri(normalizedUrl));
            if (!IsConfiguredAccount(source.AccountName)
                || string.IsNullOrWhiteSpace(source.BlobContainerName)
                || string.IsNullOrWhiteSpace(source.BlobName))
            {
                return false;
            }

            await serviceClient.Value
                .GetBlobContainerClient(source.BlobContainerName)
                .GetBlobClient(source.BlobName)
                .DeleteIfExistsAsync(cancellationToken: cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to delete blob {MediaUrl}.", mediaUrl);
            return true;
        }
    }

    private bool IsConfiguredAccount(string? accountName)
    {
        try
        {
            return string.Equals(accountName, serviceClient.Value.AccountName, StringComparison.OrdinalIgnoreCase);
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private void EnsureConfiguredAccount(string? accountName)
    {
        if (!IsConfiguredAccount(accountName))
        {
            throw new InvalidOperationException($"Media folder points to storage account '{accountName}', which does not match the configured AzureStorage account.");
        }
    }

    private static BlobServiceClient CreateServiceClient(IConfiguration configuration)
    {
        var connectionString = configuration["AzureStorage:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return new BlobServiceClient(connectionString);
        }

        var accountName = configuration["AzureStorage:AccountName"];
        if (string.IsNullOrWhiteSpace(accountName))
        {
            throw new InvalidOperationException("Configure AzureStorage:ConnectionString or AzureStorage:AccountName to use Azure Blob media folders.");
        }

        return new BlobServiceClient(new Uri($"https://{accountName}.blob.core.windows.net"), new DefaultAzureCredential());
    }
}
