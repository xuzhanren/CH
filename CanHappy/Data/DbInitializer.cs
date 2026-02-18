using CanHappy.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CanHappy.Data;

public static class DbInitializer
{
    private const string CanadaName = "Canada";
    private const string CanadaCode = "CA";
    private const string AdminRoleName = "Admin";
    private const string RegisteredUserRoleName = "RegisteredUser";

    private readonly record struct ProvinceSeed(string Name, string Code);
    private readonly record struct CitySeed(string Name, string ProvinceCode);

    private static readonly string[] CategoryNames =
    [
        "Buy & Sell",
        "Car & Vehicle",
        "Home Rental",
        "Estate Sale",
        "Car Pool",
        "Business Yellow Page"
    ];

    private static readonly ProvinceSeed[] Provinces =
    [
        new("Alberta", "AB"),
        new("British Columbia", "BC"),
        new("Manitoba", "MB"),
        new("New Brunswick", "NB"),
        new("Newfoundland and Labrador", "NL"),
        new("Nova Scotia", "NS"),
        new("Northwest Territories", "NT"),
        new("Nunavut", "NU"),
        new("Ontario", "ON"),
        new("Prince Edward Island", "PE"),
        new("Quebec", "QC"),
        new("Saskatchewan", "SK"),
        new("Yukon", "YT")
    ];

    private static readonly CitySeed[] CanadaTop100Cities =
    [
        new("Toronto", "ON"),
        new("Montreal", "QC"),
        new("Calgary", "AB"),
        new("Ottawa", "ON"),
        new("Edmonton", "AB"),
        new("Mississauga", "ON"),
        new("Winnipeg", "MB"),
        new("Vancouver", "BC"),
        new("Brampton", "ON"),
        new("Hamilton", "ON"),
        new("Quebec City", "QC"),
        new("Surrey", "BC"),
        new("Laval", "QC"),
        new("Halifax", "NS"),
        new("London", "ON"),
        new("Markham", "ON"),
        new("Vaughan", "ON"),
        new("Gatineau", "QC"),
        new("Longueuil", "QC"),
        new("Burnaby", "BC"),
        new("Saskatoon", "SK"),
        new("Kitchener", "ON"),
        new("Windsor", "ON"),
        new("Regina", "SK"),
        new("Richmond", "BC"),
        new("Richmond Hill", "ON"),
        new("Oakville", "ON"),
        new("Burlington", "ON"),
        new("Sherbrooke", "QC"),
        new("Oshawa", "ON"),
        new("Saguenay", "QC"),
        new("Levis", "QC"),
        new("Barrie", "ON"),
        new("Abbotsford", "BC"),
        new("Coquitlam", "BC"),
        new("Trois-Rivieres", "QC"),
        new("St. Catharines", "ON"),
        new("Sudbury", "ON"),
        new("Kelowna", "BC"),
        new("Kingston", "ON"),
        new("Langley", "BC"),
        new("Ajax", "ON"),
        new("Guelph", "ON"),
        new("Saanich", "BC"),
        new("Terrebonne", "QC"),
        new("Milton", "ON"),
        new("Cambridge", "ON"),
        new("Whitby", "ON"),
        new("Delta", "BC"),
        new("Waterloo", "ON"),
        new("Red Deer", "AB"),
        new("Kamloops", "BC"),
        new("Lethbridge", "AB"),
        new("Brantford", "ON"),
        new("Nanaimo", "BC"),
        new("Victoria", "BC"),
        new("Chilliwack", "BC"),
        new("Maple Ridge", "BC"),
        new("Saint John", "NB"),
        new("Moncton", "NB"),
        new("Fredericton", "NB"),
        new("Dieppe", "NB"),
        new("Thunder Bay", "ON"),
        new("Peterborough", "ON"),
        new("Niagara Falls", "ON"),
        new("Sarnia", "ON"),
        new("Prince George", "BC"),
        new("New Westminster", "BC"),
        new("North Vancouver", "BC"),
        new("West Vancouver", "BC"),
        new("Port Coquitlam", "BC"),
        new("St. Albert", "AB"),
        new("Medicine Hat", "AB"),
        new("Grande Prairie", "AB"),
        new("Airdrie", "AB"),
        new("Spruce Grove", "AB"),
        new("Leduc", "AB"),
        new("Repentigny", "QC"),
        new("Brossard", "QC"),
        new("Drummondville", "QC"),
        new("Saint-Jean-sur-Richelieu", "QC"),
        new("Granby", "QC"),
        new("North Bay", "ON"),
        new("Brandon", "MB"),
        new("Steinbach", "MB"),
        new("Thompson", "MB"),
        new("Portage la Prairie", "MB"),
        new("Prince Albert", "SK"),
        new("Moose Jaw", "SK"),
        new("Swift Current", "SK"),
        new("Sydney", "NS"),
        new("Truro", "NS"),
        new("St. John's", "NL"),
        new("Mount Pearl", "NL"),
        new("Corner Brook", "NL"),
        new("Charlottetown", "PE"),
        new("Summerside", "PE"),
        new("Yellowknife", "NT"),
        new("Whitehorse", "YT"),
        new("Iqaluit", "NU")
    ];

    public static async Task InitializeAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();
        await EnsureRoleExistsAsync(context, RegisteredUserRoleName);
        await SeedAdminRoleAssignmentAsync(context, services);

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
        await SeedCanadaHierarchyAsync(context);
    }

    private static async Task SeedAdminRoleAssignmentAsync(ApplicationDbContext context, IServiceProvider services)
    {
        var adminRole = await EnsureRoleExistsAsync(context, AdminRoleName);

        var configuration = services.GetService<IConfiguration>();
        var configuredAdminEmail = configuration?["SeedAdmin:Email"];

        IdentityUser? adminUser = null;

        if (!string.IsNullOrWhiteSpace(configuredAdminEmail))
        {
            var normalizedEmail = configuredAdminEmail.Trim().ToUpperInvariant();
            adminUser = await context.Users.FirstOrDefaultAsync(user => user.NormalizedEmail == normalizedEmail);
        }

        adminUser ??= await context.Users.OrderBy(user => user.Id).FirstOrDefaultAsync();
        if (adminUser is null)
        {
            return;
        }

        var alreadyMapped = await context.UserRoles
            .AnyAsync(userRole => userRole.UserId == adminUser.Id && userRole.RoleId == adminRole.Id);

        if (alreadyMapped)
        {
            return;
        }

        context.UserRoles.Add(new IdentityUserRole<string>
        {
            UserId = adminUser.Id,
            RoleId = adminRole.Id
        });

        await context.SaveChangesAsync();
    }

    private static async Task<IdentityRole> EnsureRoleExistsAsync(ApplicationDbContext context, string roleName)
    {
        var role = await context.Roles.FirstOrDefaultAsync(item => item.Name == roleName);
        if (role is not null)
        {
            return role;
        }

        role = new IdentityRole(roleName)
        {
            NormalizedName = roleName.ToUpperInvariant()
        };

        context.Roles.Add(role);
        await context.SaveChangesAsync();
        return role;
    }

    private static async Task SeedCanadaHierarchyAsync(ApplicationDbContext context)
    {
        var now = DateTime.UtcNow;

        var country = await context.Countries.FirstOrDefaultAsync(c => c.Code == CanadaCode || c.Name == CanadaName);
        if (country is null)
        {
            country = new Country
            {
                Name = CanadaName,
                Code = CanadaCode,
                Description = CanadaName,
                DeletedInd = false,
                SampleInd = false,
                CreatedBy = "system",
                CreatedDate = now
            };
            context.Countries.Add(country);
            await context.SaveChangesAsync();
        }
        else
        {
            country.Name = CanadaName;
            country.Code = CanadaCode;
            country.Description = CanadaName;
            country.DeletedInd = false;
            country.ModifiedBY = "system";
            country.ModifiedDate = now;
            await context.SaveChangesAsync();
        }

        var existingProvinces = await context.Provinces
            .Where(p => p.CountryId == country.CountryId)
            .ToListAsync();

        var provinceLookup = existingProvinces
            .Where(p => !string.IsNullOrWhiteSpace(p.Code))
            .ToDictionary(p => p.Code!, StringComparer.OrdinalIgnoreCase);

        foreach (var provinceSeed in Provinces)
        {
            if (provinceLookup.TryGetValue(provinceSeed.Code, out var province))
            {
                province.Name = provinceSeed.Name;
                province.Description = provinceSeed.Name;
                province.DeletedInd = false;
                province.ModifiedBY = "system";
                province.ModifiedDate = now;
            }
            else
            {
                context.Provinces.Add(new Province
                {
                    CountryId = country.CountryId,
                    Name = provinceSeed.Name,
                    Code = provinceSeed.Code,
                    Description = provinceSeed.Name,
                    DeletedInd = false,
                    SampleInd = false,
                    CreatedBy = "system",
                    CreatedDate = now
                });
            }
        }

        await context.SaveChangesAsync();

        var provincesByCode = await context.Provinces
            .Where(p => p.CountryId == country.CountryId && p.Code != null)
            .ToDictionaryAsync(p => p.Code!, StringComparer.OrdinalIgnoreCase);

        var provinceIds = provincesByCode.Values.Select(p => p.ProvinceId).ToHashSet();
        var existingCities = await context.Cities
            .Where(c => provinceIds.Contains(c.ProvinceId))
            .ToListAsync();

        var cityLookup = existingCities.ToDictionary(
            city => BuildLocationKey(city.ProvinceId, city.Name),
            StringComparer.OrdinalIgnoreCase);

        foreach (var citySeed in CanadaTop100Cities)
        {
            if (!provincesByCode.TryGetValue(citySeed.ProvinceCode, out var province))
            {
                continue;
            }

            var cityKey = BuildLocationKey(province.ProvinceId, citySeed.Name);
            if (cityLookup.TryGetValue(cityKey, out var city))
            {
                city.Code = BuildCode(citySeed.Name);
                city.Description = citySeed.Name;
                city.DeletedInd = false;
                city.ModifiedBY = "system";
                city.ModifiedDate = now;
            }
            else
            {
                context.Cities.Add(new City
                {
                    ProvinceId = province.ProvinceId,
                    Name = citySeed.Name,
                    Code = BuildCode(citySeed.Name),
                    Description = citySeed.Name,
                    DeletedInd = false,
                    SampleInd = false,
                    CreatedBy = "system",
                    CreatedDate = now
                });
            }
        }

        await context.SaveChangesAsync();

        var citiesByKey = await context.Cities
            .Where(c => provinceIds.Contains(c.ProvinceId))
            .ToDictionaryAsync(c => BuildLocationKey(c.ProvinceId, c.Name), StringComparer.OrdinalIgnoreCase);

        var cityIds = citiesByKey.Values.Select(c => c.CityId).ToHashSet();
        var existingAreas = await context.Areas
            .Where(a => cityIds.Contains(a.CityId))
            .ToListAsync();

        var areaLookup = existingAreas.ToDictionary(
            area => BuildLocationKey(area.CityId, area.Name),
            StringComparer.OrdinalIgnoreCase);

        foreach (var citySeed in CanadaTop100Cities)
        {
            if (!provincesByCode.TryGetValue(citySeed.ProvinceCode, out var province))
            {
                continue;
            }

            var cityKey = BuildLocationKey(province.ProvinceId, citySeed.Name);
            if (!citiesByKey.TryGetValue(cityKey, out var city))
            {
                continue;
            }

            var areaName = $"{city.Name} Central";
            var areaKey = BuildLocationKey(city.CityId, areaName);

            if (areaLookup.TryGetValue(areaKey, out var area))
            {
                area.Code = BuildCode(areaName);
                area.Description = areaName;
                area.DeletedInd = false;
                area.ModifiedBY = "system";
                area.ModifiedDate = now;
            }
            else
            {
                context.Areas.Add(new Area
                {
                    CityId = city.CityId,
                    Name = areaName,
                    Code = BuildCode(areaName),
                    Description = areaName,
                    DeletedInd = false,
                    SampleInd = false,
                    CreatedBy = "system",
                    CreatedDate = now
                });
            }
        }

        await context.SaveChangesAsync();
    }

    private static string BuildLocationKey(int parentId, string name)
    {
        return $"{parentId}:{name.Trim().ToUpperInvariant()}";
    }

    private static string BuildCode(string name)
    {
        var code = new string(name.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
        return code.Length <= 50 ? code : code[..50];
    }
}