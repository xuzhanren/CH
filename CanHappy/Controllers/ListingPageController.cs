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
        var listingCardAdWaitSeconds = Math.Max(0, configuration.GetValue<int?>("ListingCardAdWaitSeconds") ?? 3);

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
        ViewData["ListingCardAdWaitSeconds"] = listingCardAdWaitSeconds;

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

    [HttpGet("TopCardAd/{listingGuid:guid}")]
    public async Task<IActionResult> TopCardAd(Guid listingGuid, string? keywords)
    {
        void SetDebugHeader(string message)
        {
            if (!environment.IsDevelopment())
            {
                return;
            }

            var compact = message.Length > 500 ? message[..500] : message;
            Response.Headers["X-TopCardAd-Debug"] = compact;
        }

        var listing = await context.Listings
            .AsNoTracking()
            .Include(item => item.Category)
            .Include(item => item.Subcategory)
            .Include(item => item.Province)
            .Include(item => item.City)
            .FirstOrDefaultAsync(item => item.ListingGUID == listingGuid && !item.DeletedInd);

        if (listing is null)
        {
            SetDebugHeader($"listing-not-found:{listingGuid}");
            logger.LogInformation("TopCardAd listing not found for ListingGUID={ListingGuid}", listingGuid);
            return NotFound();
        }

        var today = CanHappy.Common.EasternTime.Now.Date;
        var allEligibleAds = await context.Ads
            .AsNoTracking()
            .Include(item => item.AdSizeOption)
            .Include(item => item.Category)
            .Include(item => item.Subcategory)
            .Include(item => item.Province)
            .Include(item => item.City)
            .Where(item => !item.DeletedInd
                && item.ActiveInd
                && item.AdStatusId == 3
                && item.PublishDate < today
                && (!item.ExpiryDate.HasValue || item.ExpiryDate.Value > today)
                && !string.IsNullOrWhiteSpace(item.ImageURL)
                && !string.IsNullOrWhiteSpace(item.TargetURL))
            .ToListAsync();

        static string NormalizeAdSizeName(string? adSizeName)
        {
            if (string.IsNullOrWhiteSpace(adSizeName))
            {
                return string.Empty;
            }

            return new string(adSizeName
                .Where(character => !char.IsWhiteSpace(character) && character != '-' && character != '_')
                .ToArray());
        }

        var targetAdSizeName = "800x530InListingCard";
        var candidateAds = allEligibleAds
            .Where(item =>
            {
                var normalizedSizeName = NormalizeAdSizeName(item.AdSizeOption?.Name);
                return normalizedSizeName.Equals(targetAdSizeName, StringComparison.OrdinalIgnoreCase)
                    || normalizedSizeName.Contains("800x530", StringComparison.OrdinalIgnoreCase)
                    || normalizedSizeName.Contains("ListingCard", StringComparison.OrdinalIgnoreCase);
            })
            .ToList();

        if (candidateAds.Count == 0)
        {
            var reason = $"no-ad-size-match eligible={allEligibleAds.Count} listing={listing.ListingGUID:N} city={listing.CityId} province={listing.ProvinceId} category={listing.CategoryId} subcategory={listing.SubcategoryId}";
            SetDebugHeader(reason);
            logger.LogInformation("TopCardAd no AdSize match for ListingGUID={ListingGuid}. EligibleAds={EligibleAds}. TargetAdSize={TargetAdSize}.", listing.ListingGUID, allEligibleAds.Count, targetAdSizeName);
            return NoContent();
        }

        var scoredAds = candidateAds
            .Select(item => new
            {
                Ad = item,
                Match = EvaluateListingCardAdMatch(listing, item, keywords)
            })
            .OrderByDescending(item => item.Match.Score)
            .ThenByDescending(item => item.Ad.IsFeatured)
            .ThenByDescending(item => item.Ad.PublishDate)
            .ToList();

        var bestAd = scoredAds.FirstOrDefault();

        if (bestAd is null)
        {
            var reason = $"no-scored-ad candidates={candidateAds.Count} listing={listing.ListingGUID:N}";
            SetDebugHeader(reason);
            logger.LogInformation("TopCardAd no scored ad for ListingGUID={ListingGuid}. Candidates={CandidateCount}.", listing.ListingGUID, candidateAds.Count);
            return NoContent();
        }

        var topThree = string.Join(", ",
            scoredAds
                .Take(3)
                .Select(item => $"{item.Ad.AdGUID:N}:{item.Match.Score}"));

        var matchedKeywordsText = string.Join("|",
            bestAd.Match.MatchedKeywords
                .Take(20));

        await context.Ads
            .Where(item => item.AdGUID == bestAd.Ad.AdGUID)
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(item => item.ViewCount, item => item.ViewCount + 1));

        SetDebugHeader($"selected={bestAd.Ad.AdGUID:N};score={bestAd.Match.Score};eligible={allEligibleAds.Count};sizeMatch={candidateAds.Count};keywords={keywords};matched={matchedKeywordsText};top={topThree}");
        logger.LogInformation(
            "TopCardAd selected AdGUID={AdGuid} score={Score} matchedKeywords={MatchedKeywords} for ListingGUID={ListingGuid}. QueryKeywords={QueryKeywords}. Eligible={EligibleCount}, AdSizeMatched={SizeMatchCount}, TopCandidates={TopCandidates}",
            bestAd.Ad.AdGUID,
            bestAd.Match.Score,
            matchedKeywordsText,
            listing.ListingGUID,
            keywords,
            allEligibleAds.Count,
            candidateAds.Count,
            topThree);

        return Json(new
        {
            adGuid = bestAd.Ad.AdGUID,
            subject = bestAd.Ad.Subject,
            imageURL = bestAd.Ad.ImageURL,
            targetURL = bestAd.Ad.TargetURL,
            score = bestAd.Match.Score,
            matchedKeywords = bestAd.Match.MatchedKeywords
        });
    }

    [HttpPost("TopCardAdClick/{adGuid:guid}")]
    [HttpGet("TopCardAdClick/{adGuid:guid}")]
    public async Task<IActionResult> TopCardAdClick(Guid adGuid)
    {
        var affectedRows = await context.Ads
            .Where(item => item.AdGUID == adGuid && !item.DeletedInd && item.ActiveInd)
            .ExecuteUpdateAsync(setters =>
                setters.SetProperty(item => item.ClickCount, item => item.ClickCount + 1));

        if (affectedRows <= 0)
        {
            return NotFound();
        }

        if (environment.IsDevelopment())
        {
            logger.LogInformation("TopCardAdClick tracked for AdGUID={AdGuid}", adGuid);
        }

        return NoContent();
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
            var isCarPoolCategory = string.Equals(listing.CategoryName, "Car Pool", StringComparison.OrdinalIgnoreCase);
            var isBusinessYellowPageCategory = string.Equals(listing.CategoryName, "Business Yellow Page", StringComparison.OrdinalIgnoreCase);

            var detailUrl = isBuySellCategory
                ? Url.Action("Index", "BuySellDetailPage", new { listingGuid = listing.ListingGUID })
                : isCarVehicleCategory
                    ? Url.Action("Index", "CarVehicleDetailPage", new { listingGuid = listing.ListingGUID })
                    : isHomeRentalCategory
                        ? Url.Action("Index", "HomeRentalDetailPage", new { listingGuid = listing.ListingGUID })
                        : isEstateSaleCategory
                            ? Url.Action("Index", "EstateSaleDetailPage", new { listingGuid = listing.ListingGUID })
                            : isCarPoolCategory
                                ? Url.Action("Index", "CarPoolDetailPage", new { listingGuid = listing.ListingGUID })
                                : isBusinessYellowPageCategory
                                    ? Url.Action("Index", "BusinessYellowPageDetailPage", new { listingGuid = listing.ListingGUID })
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
    public async Task<IActionResult> Create([Bind("CategoryId,SubcategoryId,Subject,Description,KeyWords,ProvinceId,CityId,Address,PostalCode,ContactPhone,ContactName,ShowContactInd,Price,DiscountPercent,DiscountBeginDate,DiscountEndDate,Brand,Model,Condition,ThumbnailURL")] Listing listing, string? croppedThumbnailData, string? clearedThumbnailUrl)
    {
        TryDeleteWebRootFile(clearedThumbnailUrl);

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
    public async Task<IActionResult> Edit(Guid id, [Bind("ListingGUID,CategoryId,SubcategoryId,Subject,Description,KeyWords,ProvinceId,CityId,Address,PostalCode,ContactPhone,ContactName,ShowContactInd,Price,DiscountPercent,DiscountBeginDate,DiscountEndDate,Brand,Model,Condition,ThumbnailURL")] Listing listing, string? croppedThumbnailData, string? clearedThumbnailUrl, string? categoryName, string? subcategoryName)
    {
        if (id != listing.ListingGUID)
        {
            return NotFound();
        }

        TryDeleteWebRootFile(clearedThumbnailUrl);

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

        var originalThumbnailUrl = existingListing.ThumbnailURL;

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

            if (!string.Equals(originalThumbnailUrl, existingListing.ThumbnailURL, StringComparison.OrdinalIgnoreCase))
            {
                TryDeleteWebRootFile(originalThumbnailUrl);
            }
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

        var resolvedSubcategoryName = existingListing.SubcategoryId.HasValue
            ? await context.Subcategories
                .Where(subcategory => subcategory.SubcategoryId == existingListing.SubcategoryId.Value)
                .Select(subcategory => subcategory.Name)
                .FirstOrDefaultAsync()
            : null;

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

    private static AdMatchEvaluation EvaluateListingCardAdMatch(Listing listing, Ad ad, string? queryKeywords)
    {
        var score = 0;

        if (ad.ProvinceId.HasValue && ad.ProvinceId.Value == listing.ProvinceId)
        {
            score += 20;
        }

        if (ad.CityId == listing.CityId)
        {
            score += 30;
        }

        if (ad.CategoryId.HasValue && ad.CategoryId.Value == listing.CategoryId)
        {
            score += 25;
        }

        if (ad.SubcategoryId.HasValue && ad.SubcategoryId.Value == listing.SubcategoryId)
        {
            score += 35;
        }

        var queryWords = BuildWordSet(queryKeywords);
        var listingKeywordWords = BuildWordSet(listing.KeyWords);
        var listingSubjectWords = BuildWordSet(listing.Subject);
        var listingDescriptionWords = BuildWordSet(listing.Description);
        var preferredListingWords = queryWords
            .Concat(listingKeywordWords)
            .Concat(listingSubjectWords)
            .Concat(listingDescriptionWords)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(listing.Subject) && !string.IsNullOrWhiteSpace(ad.Subject))
        {
            score += ComputePairTextScore(listing.Subject, ad.Subject, exactWeight: 4, partialWeight: 2);
        }

        if (!string.IsNullOrWhiteSpace(listing.Description) && !string.IsNullOrWhiteSpace(ad.Description))
        {
            score += ComputePairTextScore(listing.Description, ad.Description, exactWeight: 3, partialWeight: 1);
        }

        if (!string.IsNullOrWhiteSpace(listing.KeyWords) && !string.IsNullOrWhiteSpace(ad.KeyWords))
        {
            score += ComputePairTextScore(listing.KeyWords, ad.KeyWords, exactWeight: 4, partialWeight: 2);
        }

        var listingWords = BuildWordSet(
            listing.Subject,
            listing.Description,
            listing.KeyWords,
            listing.Category?.Name,
            listing.Subcategory?.Name,
            listing.Province?.Name,
            listing.City?.Name,
            listing.ProvinceId.ToString(CultureInfo.InvariantCulture),
            listing.CityId.ToString(CultureInfo.InvariantCulture));

        var adWords = BuildWordSet(
            ad.Subject,
            ad.Description,
            ad.KeyWords,
            ad.Category?.Name,
            ad.Subcategory?.Name,
            ad.Province?.Name,
            ad.City?.Name,
            ad.ProvinceId?.ToString(CultureInfo.InvariantCulture),
            ad.CityId?.ToString(CultureInfo.InvariantCulture));

        var matchedKeywords = preferredListingWords
            .Intersect(adWords, StringComparer.OrdinalIgnoreCase)
            .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (preferredListingWords.Count > 0 && adWords.Count > 0)
        {
            score += matchedKeywords.Count * 8;
        }

        if (listingWords.Count == 0 || adWords.Count == 0)
        {
            return new AdMatchEvaluation(score, matchedKeywords);
        }

        var wordMatchCount = listingWords.Intersect(adWords, StringComparer.OrdinalIgnoreCase).Count();
        score += wordMatchCount * 4;

        if (queryWords.Count > 0)
        {
            score += ComputeKeywordMatchScore(ad.Subject, ad.Description, ad.KeyWords, queryWords);
        }

        score += 1;

        return new AdMatchEvaluation(score, matchedKeywords);
    }

    private sealed record AdMatchEvaluation(int Score, IReadOnlyList<string> MatchedKeywords);

    private static int ComputePairTextScore(string leftText, string rightText, int exactWeight, int partialWeight)
    {
        var leftWords = BuildWordSet(leftText);
        var rightWords = BuildWordSet(rightText);
        if (leftWords.Count == 0 || rightWords.Count == 0)
        {
            return 0;
        }

        var score = 0;
        foreach (var leftWord in leftWords)
        {
            if (rightWords.Contains(leftWord))
            {
                score += exactWeight;
                continue;
            }

            if (leftWord.Length < 3)
            {
                continue;
            }

            if (rightWords.Any(rightWord => rightWord.Contains(leftWord, StringComparison.OrdinalIgnoreCase)
                || leftWord.Contains(rightWord, StringComparison.OrdinalIgnoreCase)))
            {
                score += partialWeight;
            }
        }

        return score;
    }

    private static HashSet<string> BuildWordSet(params string?[] values)
    {
        var words = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            foreach (var token in value.Split([' ', ',', ';', '|', '/', '\\', '\t', '\r', '\n', '-', '_'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (token == "&")
                {
                    continue;
                }

                words.Add(token);
            }
        }

        return words;
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

    private void TryDeleteWebRootFile(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl)
            || imageUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (Uri.TryCreate(imageUrl, UriKind.Absolute, out _))
        {
            return;
        }

        var trimmedPath = imageUrl;
        var queryOrFragmentIndex = trimmedPath.IndexOfAny(['?', '#']);
        if (queryOrFragmentIndex >= 0)
        {
            trimmedPath = trimmedPath[..queryOrFragmentIndex];
        }

        var relativePath = trimmedPath
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar)
            .TrimStart(Path.DirectorySeparatorChar);

        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return;
        }

        try
        {
            var webRootPath = Path.GetFullPath(environment.WebRootPath);
            var fullPath = Path.GetFullPath(Path.Combine(webRootPath, relativePath));
            if (!fullPath.StartsWith(webRootPath, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }
        catch
        {
        }
    }
}

