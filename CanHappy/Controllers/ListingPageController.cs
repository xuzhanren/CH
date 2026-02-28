using CanHappy.Data;
using CanHappy.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;

namespace CanHappy.Controllers;

[Route("Listing")]
public class ListingPageController(
    ApplicationDbContext context,
    IWebHostEnvironment environment,
    IHttpClientFactory httpClientFactory,
    ILogger<ListingPageController> logger,
    IConfiguration configuration) : Controller
{
    private static readonly ConcurrentDictionary<string, (double Latitude, double Longitude)?> PostalCodeCoordinateCache = new(StringComparer.OrdinalIgnoreCase);

    [HttpGet("", Name = "ListingIndex")]
    public async Task<IActionResult> Index(string? categoryName, string? subcategoryName, string? keywords, int? provinceId, int? cityId, Guid? focusListingId, int page = 1)
    {
        page = Math.Max(1, page);
        var pageSize = Math.Max(1, configuration.GetValue<int?>("NumberOfListingsPerPage") ?? 5);

        var query = context.Listings
            .Include(listing => listing.Category)
            .Include(listing => listing.Subcategory)
            .Include(listing => listing.Province)
            .Include(listing => listing.City)
            .AsNoTracking()
            .Where(listing => !listing.DeletedInd)
            .AsQueryable();
        query = ApplyListingFilters(query, categoryName, subcategoryName, provinceId, cityId);

        var searchWords = TokenizeSearchWords(keywords);
        List<Listing> listings;
        int totalItemCount;
        int totalPages;

        if (searchWords.Length > 0)
        {
            var rankedListings = (await query.ToListAsync())
                .Select(listing => new
                {
                    Listing = listing,
                    Score = ComputeKeywordMatchScore(listing, searchWords)
                })
                .Where(item => item.Score > 0)
                .OrderByDescending(item => item.Score)
                .ThenByDescending(item => item.Listing.CreatedDate)
                .ToList();

            totalItemCount = rankedListings.Count;
            totalPages = Math.Max(1, (int)Math.Ceiling(totalItemCount / (double)pageSize));
            page = Math.Min(page, totalPages);

            listings = rankedListings
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(item => item.Listing)
                .ToList();
        }
        else
        {
            totalItemCount = await query.CountAsync();
            totalPages = Math.Max(1, (int)Math.Ceiling(totalItemCount / (double)pageSize));
            page = Math.Min(page, totalPages);

            listings = await query
                .OrderByDescending(listing => listing.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        ViewData["CategoryName"] = categoryName;
        ViewData["SubcategoryName"] = subcategoryName;
        ViewData["Keywords"] = keywords;
        ViewData["ProvinceId"] = provinceId;
        ViewData["CityId"] = cityId;
        ViewData["CurrentPage"] = page;
        ViewData["TotalPages"] = totalPages;
        ViewData["PageSize"] = pageSize;
        ViewData["TotalItemCount"] = totalItemCount;

        if (focusListingId.HasValue)
        {
            ViewData["FocusListingId"] = focusListingId.Value;
        }

        var listingIds = listings.Select(item => item.ListingGUID).ToList();
        var reviewCounts = listingIds.Count == 0
            ? new Dictionary<Guid, int>()
            : await context.ListingReviews
                .AsNoTracking()
                .Where(item => listingIds.Contains(item.ListingGUID) && !item.DeletedInd)
                .GroupBy(item => item.ListingGUID)
                .Select(group => new { ListingGUID = group.Key, Count = group.Count() })
                .ToDictionaryAsync(item => item.ListingGUID, item => item.Count);

        ViewData["ReviewCountByListing"] = reviewCounts;

        return View("~/Views/Listing/Index.cshtml", listings);
    }

    [HttpGet("Map")]
    public async Task<IActionResult> Map(string? categoryName, string? subcategoryName, string? keywords, int? provinceId, int? cityId, CancellationToken cancellationToken)
    {
        var query = context.Listings
            .AsNoTracking()
            .Include(listing => listing.Category)
            .Include(listing => listing.Subcategory)
            .Include(listing => listing.Province)
            .Include(listing => listing.City)
            .Where(listing => !listing.DeletedInd && !string.IsNullOrWhiteSpace(listing.PostalCode))
            .AsQueryable();

        query = ApplyListingFilters(query, categoryName, subcategoryName, provinceId, cityId);

        ViewData["CategoryName"] = categoryName;
        ViewData["SubcategoryName"] = subcategoryName;
        ViewData["Keywords"] = keywords;
        ViewData["ProvinceId"] = provinceId;
        ViewData["CityId"] = cityId;

        var listings = await query
            .Select(listing => new
            {
                listing.ListingGUID,
                listing.Subject,
                listing.Description,
                listing.KeyWords,
                listing.PostalCode,
                listing.Price,
                listing.CreatedDate,
                CategoryName = listing.Category != null ? listing.Category.Name : null
            })
            .ToListAsync();

        var searchWords = TokenizeSearchWords(keywords);
        if (searchWords.Length > 0)
        {
            listings = listings
                .Select(listing => new
                {
                    Listing = listing,
                    Score = ComputeKeywordMatchScore(listing.Subject, listing.Description, listing.KeyWords, searchWords)
                })
                .Where(item => item.Score > 0)
                .OrderByDescending(item => item.Score)
                .ThenByDescending(item => item.Listing.CreatedDate)
                .Select(item => item.Listing)
                .ToList();
        }
        else
        {
            listings = listings
                .OrderByDescending(listing => listing.CreatedDate)
                .ToList();
        }

        var distinctPostalCodes = listings
            .Select(listing => NormalizePostalCode(listing.PostalCode))
            .Where(postalCode => !string.IsNullOrWhiteSpace(postalCode))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var coordinatesByPostalCode = new Dictionary<string, (double Latitude, double Longitude)?>(StringComparer.OrdinalIgnoreCase);
        foreach (var normalizedPostalCode in distinctPostalCodes)
        {
            var coordinate = await GeocodePostalCodeWithGeocoderCaAsync(normalizedPostalCode, cancellationToken);
            coordinatesByPostalCode[normalizedPostalCode] = coordinate;
        }

        var model = listings.Select(listing =>
        {
            var normalizedPostalCode = NormalizePostalCode(listing.PostalCode);
            coordinatesByPostalCode.TryGetValue(normalizedPostalCode, out var coordinate);

            var isBuySellCategory = string.Equals(listing.CategoryName, "Buy & Sell", StringComparison.OrdinalIgnoreCase);
            var isCarVehicleCategory = string.Equals(listing.CategoryName, "Car & Vehicle", StringComparison.OrdinalIgnoreCase);
            var isHomeRentalCategory = string.Equals(listing.CategoryName, "Home Rental", StringComparison.OrdinalIgnoreCase);
            var isEstateSaleCategory = string.Equals(listing.CategoryName, "Estate Sale", StringComparison.OrdinalIgnoreCase);

            var detailUrl = isBuySellCategory
                ? Url.Action("Index", "BuySellDetailPage", new { listingGuid = listing.ListingGUID })
                : isCarVehicleCategory
                    ? Url.Action("Index", "CarVehicleDetailPage", new { listingGuid = listing.ListingGUID })
                    : isHomeRentalCategory
                        ? Url.Action("Index", "HomeRentalDetailPage", new { listingGuid = listing.ListingGUID })
                        : isEstateSaleCategory
                            ? Url.Action("Index", "EstateSaleDetailPage", new { listingGuid = listing.ListingGUID })
                            : Url.Action("Details", "ListingPage", new { id = listing.ListingGUID });

            return new ListingMapMarkerViewModel
            {
                ListingGUID = listing.ListingGUID,
                Subject = string.IsNullOrWhiteSpace(listing.Subject) ? "Listing" : listing.Subject,
                PostalCode = listing.PostalCode ?? string.Empty,
                Price = listing.Price,
                DetailUrl = detailUrl ?? "#",
                Latitude = coordinate?.Latitude,
                Longitude = coordinate?.Longitude
            };
        }).ToList();

        return View("~/Views/Listing/Map.cshtml", model);
    }

    [HttpGet("Details/{id:guid}")]
    public async Task<IActionResult> Details(Guid? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var listing = await context.Listings
            .Include(listing => listing.Category)
            .Include(listing => listing.Subcategory)
            .Include(listing => listing.Province)
            .Include(listing => listing.City)
            .FirstOrDefaultAsync(listing => listing.ListingGUID == id);

        if (listing is null)
        {
            return NotFound();
        }

        var reviewCount = await context.ListingReviews
            .AsNoTracking()
            .CountAsync(item => item.ListingGUID == listing.ListingGUID && !item.DeletedInd);

        ViewData["ReviewCount"] = reviewCount;

        return View("~/Views/Listing/Details.cshtml", listing);
    }

    [HttpGet("Create")]
    public IActionResult Create()
    {
        PopulateSelectLists();
        return View("~/Views/Listing/Create.cshtml");
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("CategoryId,SubcategoryId,Subject,Description,KeyWords,ProvinceId,CityId,Address,PostalCode,ContactPhone,ContactName,ShowContactInd,Price,DiscountPercent,DiscountBeginDate,DiscountEndDate,Brand,Model,Condition,ThumbnailURL")] Listing listing, string? croppedThumbnailData)
    {
        if (!ModelState.IsValid)
        {
            PopulateSelectLists(listing.CategoryId, listing.SubcategoryId, listing.ProvinceId, listing.CityId);
            return View("~/Views/Listing/Create.cshtml", listing);
        }

        listing.ListingGUID = Guid.NewGuid();
        var currentUserIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(currentUserIdText) && Guid.TryParse(currentUserIdText, out var currentUserId))
        {
            listing.UserId = currentUserId;
        }
        listing.CreatedDate = CanHappy.Common.EasternTime.Now;
        listing.ModifiedDate = null;

        if (!string.IsNullOrWhiteSpace(croppedThumbnailData))
        {
            listing.ThumbnailURL = await SaveThumbnailFromDataUrlAsync(croppedThumbnailData);
        }

        context.Add(listing);
        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Edit/{id:guid}")]
    public async Task<IActionResult> Edit(Guid? id, string? categoryName, string? subcategoryName)
    {
        if (id is null)
        {
            return NotFound();
        }

        var listing = await context.Listings
            .Include(item => item.Category)
            .Include(item => item.Subcategory)
            .FirstOrDefaultAsync(item => item.ListingGUID == id);
        if (listing is null)
        {
            return NotFound();
        }

        ViewData["CategoryName"] = !string.IsNullOrWhiteSpace(categoryName)
            ? categoryName
            : listing.Category?.Name;
        ViewData["SubcategoryName"] = !string.IsNullOrWhiteSpace(subcategoryName)
            ? subcategoryName
            : listing.Subcategory?.Name;

        PopulateSelectLists(listing.CategoryId, listing.SubcategoryId, listing.ProvinceId, listing.CityId);
        return View("~/Views/Listing/Edit.cshtml", listing);
    }

    [HttpPost("Edit/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, [Bind("ListingGUID,CategoryId,SubcategoryId,Subject,Description,KeyWords,ProvinceId,CityId,Address,PostalCode,ContactPhone,ContactName,ShowContactInd,Price,DiscountPercent,DiscountBeginDate,DiscountEndDate,Brand,Model,Condition,ThumbnailURL")] Listing listing, string? croppedThumbnailData, string? categoryName, string? subcategoryName)
    {
        if (id != listing.ListingGUID)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            ViewData["CategoryName"] = categoryName;
            ViewData["SubcategoryName"] = subcategoryName;
            PopulateSelectLists(listing.CategoryId, listing.SubcategoryId, listing.ProvinceId, listing.CityId);
            return View("~/Views/Listing/Edit.cshtml", listing);
        }

        var existingListing = await context.Listings.FirstOrDefaultAsync(item => item.ListingGUID == id);
        if (existingListing is null)
        {
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(croppedThumbnailData))
        {
            listing.ThumbnailURL = await SaveThumbnailFromDataUrlAsync(croppedThumbnailData);
        }

        try
        {
            existingListing.CategoryId = listing.CategoryId;
            existingListing.SubcategoryId = listing.SubcategoryId;
            existingListing.Subject = listing.Subject;
            existingListing.Description = listing.Description;
            existingListing.KeyWords = listing.KeyWords;
            existingListing.ProvinceId = listing.ProvinceId;
            existingListing.CityId = listing.CityId;
            existingListing.Address = listing.Address;
            existingListing.PostalCode = listing.PostalCode;
            existingListing.ContactPhone = listing.ContactPhone;
            existingListing.ContactName = listing.ContactName;
            existingListing.ShowContactInd = listing.ShowContactInd;
            existingListing.Price = listing.Price;
            existingListing.DiscountPercent = listing.DiscountPercent;
            existingListing.DiscountBeginDate = listing.DiscountBeginDate;
            existingListing.DiscountEndDate = listing.DiscountEndDate;
            existingListing.Brand = listing.Brand;
            existingListing.Model = listing.Model;
            existingListing.Condition = listing.Condition;
            existingListing.ThumbnailURL = listing.ThumbnailURL;
            existingListing.ModifiedDate = CanHappy.Common.EasternTime.Now;

            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ListingExists(listing.ListingGUID))
            {
                return NotFound();
            }

            throw;
        }

        var resolvedCategoryName = await context.Categories
            .Where(category => category.CategoryId == existingListing.CategoryId)
            .Select(category => category.Name)
            .FirstOrDefaultAsync();

        var resolvedSubcategoryName = await context.Subcategories
            .Where(subcategory => subcategory.SubcategoryId == existingListing.SubcategoryId)
            .Select(subcategory => subcategory.Name)
            .FirstOrDefaultAsync();

        return RedirectToAction(nameof(Index), new
        {
            categoryName = resolvedCategoryName,
            subcategoryName = resolvedSubcategoryName,
            focusListingId = existingListing.ListingGUID
        });
    }

    [HttpGet("Delete/{id:guid}")]
    public async Task<IActionResult> Delete(Guid? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var listing = await context.Listings
            .Include(listing => listing.Category)
            .Include(listing => listing.Subcategory)
            .Include(listing => listing.Province)
            .Include(listing => listing.City)
            .FirstOrDefaultAsync(listing => listing.ListingGUID == id);

        if (listing is null)
        {
            return NotFound();
        }

        return View("~/Views/Listing/Delete.cshtml", listing);
    }

    [HttpPost("Delete/{id:guid}"), ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var listing = await context.Listings.FindAsync(id);
        if (listing is not null)
        {
            context.Listings.Remove(listing);
            await context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private void PopulateSelectLists(int? categoryId = null, int? subcategoryId = null, int? provinceId = null, int? cityId = null)
    {
        ViewData["CategoryId"] = new SelectList(context.Categories.OrderBy(category => category.Name), "CategoryId", "Name", categoryId);
        ViewData["SubcategoryId"] = new SelectList(context.Subcategories.OrderBy(subcategory => subcategory.Name), "SubcategoryId", "Name", subcategoryId);
        ViewData["ProvinceId"] = new SelectList(context.Provinces.OrderBy(province => province.Name), "ProvinceId", "Name", provinceId);
        ViewData["CityId"] = new SelectList(context.Cities.OrderBy(city => city.Name), "CityId", "Name", cityId);
        ViewData["SubcategoryLookup"] = context.Subcategories
            .AsNoTracking()
            .OrderBy(subcategory => subcategory.Name)
            .Select(subcategory => new
            {
                id = subcategory.SubcategoryId,
                categoryId = subcategory.CategoryId,
                name = subcategory.Name
            })
            .ToList();
        ViewData["CityLookup"] = context.Cities
            .AsNoTracking()
            .OrderBy(city => city.Name)
            .Select(city => new
            {
                id = city.CityId,
                provinceId = city.ProvinceId,
                name = city.Name
            })
            .ToList();
    }

    private bool ListingExists(Guid id)
    {
        return context.Listings.Any(listing => listing.ListingGUID == id);
    }

    private IQueryable<Listing> ApplyListingFilters(
        IQueryable<Listing> query,
        string? categoryName,
        string? subcategoryName,
        int? provinceId,
        int? cityId)
    {
        if (!string.IsNullOrWhiteSpace(categoryName))
        {
            query = query.Where(listing => listing.Category != null && listing.Category.Name == categoryName);
        }

        if (!string.IsNullOrWhiteSpace(subcategoryName))
        {
            query = query.Where(listing => listing.Subcategory != null && listing.Subcategory.Name == subcategoryName);
        }

        if (provinceId.HasValue)
        {
            query = query.Where(listing => listing.ProvinceId == provinceId.Value);
        }

        if (cityId.HasValue)
        {
            query = query.Where(listing => listing.CityId == cityId.Value);
        }

        return query;
    }

    private static string[] TokenizeSearchWords(string? keywords)
    {
        if (string.IsNullOrWhiteSpace(keywords))
        {
            return [];
        }

        return keywords
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(term => !string.IsNullOrWhiteSpace(term))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static int ComputeKeywordMatchScore(Listing listing, IReadOnlyCollection<string> words)
    {
        return ComputeKeywordMatchScore(listing.Subject, listing.Description, listing.KeyWords, words);
    }

    private static int ComputeKeywordMatchScore(string? subject, string? description, string? keyWords, IReadOnlyCollection<string> words)
    {
        if (words.Count == 0)
        {
            return 0;
        }

        var score = 0;
        foreach (var word in words)
        {
            score += CountOccurrences(subject, word);
            score += CountOccurrences(description, word);
            score += CountOccurrences(keyWords, word);
        }

        return score;
    }

    private static int CountOccurrences(string? source, string word)
    {
        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(word))
        {
            return 0;
        }

        var count = 0;
        var index = 0;
        while (index < source.Length)
        {
            var foundIndex = source.IndexOf(word, index, StringComparison.OrdinalIgnoreCase);
            if (foundIndex < 0)
            {
                break;
            }

            count++;
            index = foundIndex + word.Length;
        }

        return count;
    }

    private static string NormalizePostalCode(string? postalCode)
    {
        if (string.IsNullOrWhiteSpace(postalCode))
        {
            return string.Empty;
        }

        return new string(postalCode.Where(char.IsLetterOrDigit).ToArray()).ToUpperInvariant();
    }

    private async Task<(double Latitude, double Longitude)?> GeocodePostalCodeWithGeocoderCaAsync(string normalizedPostalCode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(normalizedPostalCode))
        {
            return null;
        }

        if (PostalCodeCoordinateCache.TryGetValue(normalizedPostalCode, out var cachedCoordinate))
        {
            return cachedCoordinate;
        }

        var requestUrl = $"https://geocoder.ca/?locate={Uri.EscapeDataString(normalizedPostalCode)}&json=1";

        try
        {
            var httpClient = httpClientFactory.CreateClient();
            using var response = await httpClient.GetAsync(requestUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                PostalCodeCoordinateCache[normalizedPostalCode] = null;
                return null;
            }

            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var payload = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);
            if (TryReadCoordinate(payload.RootElement, "latt", out var latitude)
                && TryReadCoordinate(payload.RootElement, "longt", out var longitude))
            {
                var coordinate = (Latitude: latitude, Longitude: longitude);
                PostalCodeCoordinateCache[normalizedPostalCode] = coordinate;
                return coordinate;
            }
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to geocode postal code {PostalCode} via geocoder.ca", normalizedPostalCode);
        }

        PostalCodeCoordinateCache[normalizedPostalCode] = null;
        return null;
    }

    private static bool TryReadCoordinate(JsonElement root, string propertyName, out double coordinate)
    {
        coordinate = 0;
        if (!root.TryGetProperty(propertyName, out var property))
        {
            return false;
        }

        if (property.ValueKind == JsonValueKind.Number)
        {
            return property.TryGetDouble(out coordinate) && double.IsFinite(coordinate);
        }

        if (property.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var rawValue = property.GetString();
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return false;
        }

        var normalizedValue = rawValue.Trim().Replace(',', '.');
        if (!double.TryParse(normalizedValue, NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out coordinate))
        {
            return false;
        }

        return double.IsFinite(coordinate);
    }

    private async Task<string?> SaveThumbnailFromDataUrlAsync(string dataUrl)
    {
        var commaIndex = dataUrl.IndexOf(',');
        if (commaIndex <= 0)
        {
            return null;
        }

        var metadata = dataUrl[..commaIndex];
        var base64Data = dataUrl[(commaIndex + 1)..];

        if (!metadata.Contains("base64", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        byte[] imageBytes;
        try
        {
            imageBytes = Convert.FromBase64String(base64Data);
        }
        catch (FormatException)
        {
            return null;
        }

        var extension = metadata.Contains("image/png", StringComparison.OrdinalIgnoreCase) ? ".png" : ".jpg";
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var relativePath = $"/ListingImages/{fileName}";
        var folderPath = Path.Combine(environment.WebRootPath, "ListingImages");

        Directory.CreateDirectory(folderPath);

        var filePath = Path.Combine(folderPath, fileName);
        await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);

        return relativePath;
    }
}

