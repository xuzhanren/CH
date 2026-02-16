using CanHappy.Models;
using Microsoft.EntityFrameworkCore;

namespace CanHappy.Data;

public static class DbInitializer
{
    private static readonly string[] CategoryNames =
    [
        "Buy & Sell",
        "Car & Vehicle",
        "Home Rental",
        "Estate Sale",
        "Car Pool",
        "Business Yellow Page"
    ];

    public static async Task InitializeAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();

        var existing = await context.Categories.ToDictionaryAsync(c => c.Name);

        for (var index = 0; index < CategoryNames.Length; index++)
        {
            var name = CategoryNames[index];
            if (existing.TryGetValue(name, out var category))
            {
                category.SortOrder = index + 1;
                category.ModifiedBy = "system";
                category.ModifiedDate = DateTime.UtcNow;
                category.DeletedInd = false;
            }
            else
            {
                context.Categories.Add(new Category
                {
                    Name = name,
                    Code = BuildCode(name),
                    Description = name,
                    SortOrder = index + 1,
                    DeletedInd = false,
                    SampleInd = false,
                    CreatedBy = "system",
                    CreatedDate = DateTime.UtcNow
                });
            }
        }

        await context.SaveChangesAsync();
    }

    private static string BuildCode(string name)
    {
        return new string(name.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
    }
}