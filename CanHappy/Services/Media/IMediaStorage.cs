namespace CanHappy.Services.Media;

public interface IMediaStorage
{
    /// <summary>
    /// Saves a file into a media folder. <paramref name="webFolder"/> is either a web-root relative
    /// folder (e.g. "/ListingImages") or an Azure Blob container URL (e.g. "https://account.blob.core.windows.net/listingimages").
    /// </summary>
    Task SaveAsync(string webFolder, string fileName, Stream content, string contentType, CancellationToken cancellationToken = default);

    Task SaveAsync(string webFolder, string fileName, byte[] content, string contentType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the blob behind an absolute URL, but only when it sits inside one of <paramref name="allowedWebFolders"/>
    /// (blob folder URLs of the configured storage account). Returns false when the URL is not such a blob,
    /// so the caller can handle it as a local file.
    /// </summary>
    Task<bool> TryDeleteBlobAsync(string? mediaUrl, IEnumerable<string> allowedWebFolders, CancellationToken cancellationToken = default);
}
