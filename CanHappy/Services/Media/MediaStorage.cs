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

    public async Task DeleteAsync(string webFolder, string? storedValue, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storedValue)
            || storedValue.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var path = storedValue.Trim();
        var queryOrFragmentIndex = path.IndexOfAny(['?', '#']);
        if (queryOrFragmentIndex >= 0)
        {
            path = path[..queryOrFragmentIndex];
        }

        var mediaUrl = MediaPathHelper.BuildMediaUrl(path, webFolder);
        if (string.IsNullOrWhiteSpace(mediaUrl))
        {
            return;
        }

        if (MediaPathHelper.IsAbsoluteUrl(mediaUrl))
        {
            await TryDeleteBlobAsync(mediaUrl, [webFolder], cancellationToken);
            return;
        }

        try
        {
            var webRootPath = Path.GetFullPath(environment.WebRootPath);
            var fullPath = Path.GetFullPath(Path.Combine(webRootPath, mediaUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)));
            if (fullPath.StartsWith(webRootPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                && File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to delete local media {MediaUrl}.", mediaUrl);
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

    private static string? FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }

    private static BlobServiceClient CreateServiceClient(IConfiguration configuration)
    {
        // AKS sets the env var AzureStorage__ConnectionString (bound as AzureStorage:ConnectionString).
        // Local runs put the same value in appsettings(.Development).json under the flat key "AzureStorage__ConnectionString".
        var connectionString = FirstNonEmpty(
            configuration["AzureStorage:ConnectionString"],
            configuration["AzureStorage__ConnectionString"]);
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return new BlobServiceClient(connectionString);
        }

        var accountName = configuration["AzureStorage:AccountName"];
        if (string.IsNullOrWhiteSpace(accountName))
        {
            throw new InvalidOperationException("Set AzureStorage__ConnectionString (appsettings.Development.json locally, or the ch-secrets secret in AKS), or AzureStorage:AccountName with an Azure login, to use Azure Blob media folders.");
        }

        return new BlobServiceClient(new Uri($"https://{accountName}.blob.core.windows.net"), new DefaultAzureCredential());
    }
}
