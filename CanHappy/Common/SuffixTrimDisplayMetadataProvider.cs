using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace CanHappy.Common;

public sealed class SuffixTrimDisplayMetadataProvider : IDisplayMetadataProvider
{
    public void CreateDisplayMetadata(DisplayMetadataProviderContext context)
    {
        if (context.Key.MetadataKind != ModelMetadataKind.Property)
        {
            return;
        }

        var propertyName = context.Key.Name;
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return;
        }

        if (context.DisplayMetadata.DisplayName is not null)
        {
            return;
        }

        var trimmedName = propertyName;
        if (trimmedName.EndsWith("Ind", StringComparison.Ordinal) && trimmedName.Length > 3)
        {
            trimmedName = trimmedName[..^3];
        }
        else if (trimmedName.EndsWith("Id", StringComparison.Ordinal) && trimmedName.Length > 2)
        {
            trimmedName = trimmedName[..^2];
        }

        if (trimmedName == propertyName)
        {
            return;
        }

        var displayName = Regex.Replace(trimmedName, "([a-z0-9])([A-Z])", "$1 $2");
        context.DisplayMetadata.DisplayName = () => displayName;
    }
}