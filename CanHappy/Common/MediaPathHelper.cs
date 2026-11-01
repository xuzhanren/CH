using Microsoft.Extensions.Configuration;

namespace CanHappy.Common;

public static class MediaPathHelper
{
    public static bool IsAbsoluteUrl(string? value)
    {
        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
    }

    public static string ResolveWebFolder(IConfiguration configuration, string settingKey, string defaultFolder)
    {
        var configured = configuration.GetValue<string>(settingKey);
        var folder = string.IsNullOrWhiteSpace(configured) ? defaultFolder : configured;
        folder = folder.Replace('\\', '/').Trim();

        // Absolute URLs (e.g. Azure Blob Storage containers) are kept as-is.
        if (IsAbsoluteUrl(folder))
        {
            return folder.TrimEnd('/');
        }

        folder = folder.Trim('/');

        return string.IsNullOrWhiteSpace(folder) ? string.Empty : $"/{folder}";
    }

    public static string? ToStoredFileName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return value;
        }

        // Media columns hold only the file name; pages may post back the resolved URL.
        var path = value.Trim().Replace('\\', '/');
        var queryOrFragmentIndex = path.IndexOfAny(['?', '#']);
        if (queryOrFragmentIndex >= 0)
        {
            path = path[..queryOrFragmentIndex];
        }

        var fileName = path[(path.LastIndexOf('/') + 1)..];
        return string.IsNullOrEmpty(fileName) ? value : fileName;
    }

    public static string? ResolveReviewImageUrl(IConfiguration configuration, string? value)
    {
        var folder = ResolveWebFolder(configuration, "ListingReviewImagesFolder", "/ListingReviewImages");
        return BuildMediaUrl(value, folder);
    }

    public static string? BuildMediaUrl(string? value, string webFolder)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim().Replace('\\', '/');
        if (trimmed.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        if (Uri.TryCreate(trimmed, UriKind.Absolute, out _))
        {
            return trimmed;
        }

        var normalizedFolder = string.IsNullOrWhiteSpace(webFolder) ? string.Empty : webFolder.TrimEnd('/');

        if (trimmed.StartsWith('/'))
        {
            return trimmed;
        }

        if (!string.IsNullOrWhiteSpace(normalizedFolder))
        {
            var folderWithoutLeadingSlash = normalizedFolder.TrimStart('/');
            if (HasFolderPrefix(trimmed, folderWithoutLeadingSlash))
            {
                return $"/{trimmed.TrimStart('/')}";
            }
        }

        return string.IsNullOrWhiteSpace(normalizedFolder)
            ? $"/{trimmed.TrimStart('/')}"
            : $"{normalizedFolder}/{trimmed.TrimStart('/')}";
    }

    private static bool HasFolderPrefix(string pathValue, string folderValue)
    {
        if (string.IsNullOrWhiteSpace(pathValue) || string.IsNullOrWhiteSpace(folderValue))
        {
            return false;
        }

        if (!pathValue.StartsWith(folderValue, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (pathValue.Length == folderValue.Length)
        {
            return true;
        }

        var nextChar = pathValue[folderValue.Length];
        return nextChar == '/' || nextChar == '?' || nextChar == '#';
    }

    public static string BuildRelativeMediaPath(string webFolder, string fileName)
    {
        var normalizedFolder = string.IsNullOrWhiteSpace(webFolder) ? string.Empty : webFolder.TrimEnd('/');
        var normalizedFileName = fileName.TrimStart('/');

        return string.IsNullOrWhiteSpace(normalizedFolder)
            ? $"/{normalizedFileName}"
            : $"{normalizedFolder}/{normalizedFileName}";
    }

    public static string BuildPhysicalFolderPath(string webRootPath, string webFolder)
    {
        var folderRelative = webFolder.Trim('/').Replace('/', Path.DirectorySeparatorChar);
        return string.IsNullOrWhiteSpace(folderRelative)
            ? webRootPath
            : Path.Combine(webRootPath, folderRelative);
    }
}
