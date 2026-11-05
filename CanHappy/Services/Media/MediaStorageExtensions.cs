namespace CanHappy.Services.Media;

public static class MediaStorageExtensions
{
    /// <summary>
    /// Deletes an image and its thumbnail (the ImageURL / ThumbnailURL pair) from a media folder.
    /// The two values are often the same file, which is then deleted only once.
    /// </summary>
    public static async Task DeleteImageAndThumbnailAsync(this IMediaStorage mediaStorage, string webFolder, string? imageUrl, string? thumbnailUrl)
    {
        await mediaStorage.DeleteAsync(webFolder, imageUrl);

        if (!string.Equals(imageUrl, thumbnailUrl, StringComparison.OrdinalIgnoreCase))
        {
            await mediaStorage.DeleteAsync(webFolder, thumbnailUrl);
        }
    }
}
