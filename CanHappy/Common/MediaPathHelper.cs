using Microsoft.Extensions.Configuration;

namespace CanHappy.Common;

public static class MediaPathHelper
{
    public static string ResolveWebFolder(IConfiguration configuration, string settingKey, string defaultFolder)
    {
        var configured = configuration.GetValue<string>(settingKey);
        var folder = string.IsNullOrWhiteSpace(configured) ? defaultFolder : configured;
        folder = folder.Replace('\\', '/').Trim();
        folder = folder.Trim('/');

        return string.IsNullOrWhiteSpace(folder) ? string.Empty : $"/{folder}";
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
