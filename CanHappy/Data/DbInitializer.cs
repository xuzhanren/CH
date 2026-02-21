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

    private static readonly string[] BuyAndSellSubcategoryNames =
    [
        "Computers & Laptops",
        "Cameras & Camcorders",
        "Furniture",
        "Arts & Music",
        "Home Appliances",
        "Cell Phones",
        "Tools",
        "Building Materials",
        "Sports",
        "Garden & Plants",
        "Kids Stuff & Toys",
        "Industrial Equipment",
        "Other"
    ];

    private static readonly string[] CarAndVehicleSubcategoryNames =
    [
        "Cars & Trucks",
        "Parts and Materials",
        "Auto Services"
    ];

    private static readonly string[] HomeRentalSubcategoryNames =
    [
        "Apartments & Condos",
        "Houses for Rent",
        "Room Rentals",
        "Vacation Rentals",
        "Storage & Parking",
        "Other"
    ];

    private static readonly string[] EstateSaleSubcategoryNames =
    [
        "Garage Sales",
        "Moving Sales",
        "Estate Auctions",
        "Antiques & Collectibles",
        "Other"
    ];

    private static readonly string[] CarPoolSubcategoryNames =
    [
        "Daily Commute",
        "Long Distance",
        "Airport Rides",
        "Student Carpool",
        "Other"
    ];

    private static readonly string[] BusinessYellowPageSubcategoryNames =
    [
        "Restaurants",
        "Health & Wellness",
        "Home Services",
        "Professional Services",
        "Education & Training",
        "Other"
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
                if (name == "Buy & Sell")
                {
                    category.KeyWords = "buy sell sale computer laptop used new refurbished Camera Camcorders Furniture desk table chair bed box sofa music Arts Music Home  Appliances Cell Phones Tools Building Materials Sports Garden Plants Kids Toys game Industrial  Equipment Samsung iPhone fridge refridgerator washer dryer stove range electronics TV bag luggage case LG Fridgedare parts";
                }
                category.ModifiedBy = "system";
                category.ModifiedDate = CanHappy.Common.EasternTime.Now;
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
                    CreatedDate = CanHappy.Common.EasternTime.Now,
                    KeyWords = name == "Buy & Sell" ? "buy sell sale computer laptop used new refurbished Camera Camcorders Furniture desk table chair bed box sofa music Arts Music Home  Appliances Cell Phones Tools Building Materials Sports Garden Plants Kids Toys game Industrial  Equipment Samsung iPhone fridge refridgerator washer dryer stove range electronics TV bag luggage case LG Fridgedare parts" : null
                });
            }
        }

        await context.SaveChangesAsync();
        await SeedCoreSubcategoriesAsync(context);
        await SeedCanadaHierarchyAsync(context);
        await SeedOttawaSampleListingsAsync(context);
    }

    private static async Task SeedOttawaSampleListingsAsync(ApplicationDbContext context)
    {
        const string targetProvinceCode = "ON";
        const string targetCityName = "Ottawa";
        const int sampleListingsPerCombination = 2;
        var now = CanHappy.Common.EasternTime.Now;

        var ottawaCity = await context.Cities
            .AsNoTracking()
            .Where(city => !city.DeletedInd && city.Name == targetCityName)
            .Join(
                context.Provinces.Where(province => !province.DeletedInd && province.Code == targetProvinceCode),
                city => city.ProvinceId,
                province => province.ProvinceId,
                (city, _) => city)
            .FirstOrDefaultAsync();

        if (ottawaCity is null)
        {
            return;
        }

        var combinations = await context.Subcategories
            .AsNoTracking()
            .Where(subcategory => !subcategory.DeletedInd)
            .Join(
                context.Categories.Where(category => !category.DeletedInd),
                subcategory => subcategory.CategoryId,
                category => category.CategoryId,
                (subcategory, category) => new
                {
                    category.CategoryId,
                    CategoryName = category.Name,
                    subcategory.SubcategoryId,
                    SubcategoryName = subcategory.Name
                })
            .OrderBy(item => item.CategoryName)
            .ThenBy(item => item.SubcategoryName)
            .ToListAsync();

        if (combinations.Count == 0)
        {
            return;
        }

        var categoryIds = combinations.Select(item => item.CategoryId).Distinct().ToHashSet();
        var subcategoryIds = combinations.Select(item => item.SubcategoryId).Distinct().ToHashSet();

        var existingSampleCounts = await context.Listings
            .AsNoTracking()
            .Where(listing =>
                listing.CityId == ottawaCity.CityId &&
                !listing.DeletedInd &&
                listing.SampleInd &&
                listing.CreatedBy == "system" &&
                categoryIds.Contains(listing.CategoryId) &&
                subcategoryIds.Contains(listing.SubcategoryId))
            .GroupBy(listing => new { listing.CategoryId, listing.SubcategoryId })
            .Select(group => new
            {
                group.Key.CategoryId,
                group.Key.SubcategoryId,
                Count = group.Count()
            })
            .ToListAsync();

        var existingCountByCombination = existingSampleCounts.ToDictionary(
            item => (item.CategoryId, item.SubcategoryId),
            item => item.Count);

        var listingsToAdd = new List<Listing>();

        foreach (var combination in combinations)
        {
            existingCountByCombination.TryGetValue((combination.CategoryId, combination.SubcategoryId), out var existingCount);
            var remaining = sampleListingsPerCombination - existingCount;

            for (var index = 0; index < remaining; index++)
            {
                var sequence = existingCount + index + 1;
                var subject = TruncateToMaxLength($"{combination.SubcategoryName} Ottawa Sample {sequence}", 50);
                var keyWords = TruncateToMaxLength($"{combination.CategoryName}, {combination.SubcategoryName}, Ottawa", 50);

                listingsToAdd.Add(new Listing
                {
                    ListingGUID = Guid.NewGuid(),
                    CategoryId = combination.CategoryId,
                    SubcategoryId = combination.SubcategoryId,
                    Subject = subject,
                    Description = $"Sample listing {sequence} for {combination.CategoryName} > {combination.SubcategoryName} in Ottawa.",
                    KeyWords = keyWords,
                    ProvinceId = ottawaCity.ProvinceId,
                    CityId = ottawaCity.CityId,
                    PostalCode = "K1A0A6",
                    ViewCount = 0,
                    ClickCount = 0,
                    DeletedInd = false,
                    SampleInd = true,
                    UserId = Guid.Empty,
                    CreatedBy = "system",
                    CreatedDate = now
                });
            }
        }

        if (listingsToAdd.Count == 0)
        {
            return;
        }

        context.Listings.AddRange(listingsToAdd);
        await context.SaveChangesAsync();
    }

    private static async Task SeedCoreSubcategoriesAsync(ApplicationDbContext context)
    {
        await SeedSubcategoriesForCategoryAsync(context, "Buy & Sell", BuyAndSellSubcategoryNames);
        await SeedSubcategoriesForCategoryAsync(context, "Car & Vehicle", CarAndVehicleSubcategoryNames);
        await SeedSubcategoriesForCategoryAsync(context, "Home Rental", HomeRentalSubcategoryNames);
        await SeedSubcategoriesForCategoryAsync(context, "Estate Sale", EstateSaleSubcategoryNames);
        await SeedSubcategoriesForCategoryAsync(context, "Car Pool", CarPoolSubcategoryNames);
        await SeedSubcategoriesForCategoryAsync(context, "Business Yellow Page", BusinessYellowPageSubcategoryNames);
    }

    private static async Task SeedSubcategoriesForCategoryAsync(
        ApplicationDbContext context,
        string categoryName,
        IReadOnlyList<string> subcategoryNames)
    {
        var category = await context.Categories
            .FirstOrDefaultAsync(item => item.Name == categoryName && !item.DeletedInd);

        if (category is null)
        {
            return;
        }

        var now = CanHappy.Common.EasternTime.Now;
        var existingSubcategories = await context.Subcategories
            .Where(subcategory => subcategory.CategoryId == category.CategoryId)
            .ToDictionaryAsync(subcategory => subcategory.Name, StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < subcategoryNames.Count; index++)
        {
            var name = subcategoryNames[index];

            if (existingSubcategories.TryGetValue(name, out var subcategory))
            {
                subcategory.SortOrder = index + 1;
                subcategory.Code = BuildCode(name);
                subcategory.Description = name;
                subcategory.DeletedInd = false;
                subcategory.ModifiedBY = "system";
                subcategory.ModifiedDate = now;
            }
            else
            {
                context.Subcategories.Add(new Subcategory
                {
                    CategoryId = category.CategoryId,
                    Name = name,
                    Code = BuildCode(name),
                    Description = name,
                    SortOrder = index + 1,
                    DeletedInd = false,
                    SampleInd = false,
                    CreatedBy = "system",
                    CreatedDate = now
                });
            }
        }

        await context.SaveChangesAsync();
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
        var now = CanHappy.Common.EasternTime.Now;

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

    private static string TruncateToMaxLength(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
