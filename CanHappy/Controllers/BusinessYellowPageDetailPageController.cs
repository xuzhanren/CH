using CanHappy.Data;
using CanHappy.Common;
using CanHappy.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CanHappy.Controllers;

[Route("BusinessYellowPageDetail")]
public class BusinessYellowPageDetailPageController(ApplicationDbContext context, IWebHostEnvironment environment, IConfiguration configuration) : Controller
{
    private const string BusinessYellowPageCategoryName = "Business Yellow Page";

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(Guid? listingGuid)
    {
        ViewData["BusinessYellowPageDetailSliderWaitSeconds"] = Math.Max(0, configuration.GetValue<int?>("BusinessYellowPageDetailSliderWaitSeconds") ?? 2);
        ViewData["BusinessYellowPageDetailAdSlidingInterval"] = Math.Max(1, configuration.GetValue<int?>("BusinessYellowPageDetailAdSlidingInterval") ?? 5);
        ViewData["BusinessYellowPageDetailAdSliderFadingMode"] = configuration.GetValue<string>("BusinessYellowPageDetailAdSliderFadingMode") ?? "homeAdFade";
        ViewData["BusinessYellowPageDetailAdPixelResolveTransitionPeriodMiliSeconds"] = Math.Max(100, configuration.GetValue<int?>("businessYellowPageDetailAdPixelResolveTransitionPeriodMiliSeconds") ?? 1000);
        ViewData["BusinessYellowPageDetailAdFadeTransitionMilliSeconds"] = Math.Max(100, configuration.GetValue<int?>("businessYellowPageDetailAdFadeTransitionMilliSeconds") ?? 450);
        ViewData["BusinessYellowPageDetailSlideTransitionMilliSeconds"] = Math.Max(100, configuration.GetValue<int?>("businessYellowPageDetailSlideTransitionMilliSeconds") ?? 650);
        ViewData["BusinessYellowPageDetailAdSlideDistancePercent"] = Math.Clamp(configuration.GetValue<int?>("businessYellowPageDetailAdSlideDistancePercent") ?? 8, 1, 30);
        ViewData["BusinessYellowPageDetailAdTransitionEasing"] = configuration.GetValue<string>("businessYellowPageDetailAdTransitionEasing") ?? "ease-in-out";

        var hasUserGuid = TryGetCurrentUserGuid(out var currentUserId);
        var canManageByRole = User.IsInRole("Admin") || User.IsInRole("Clerk");

        var model = new BusinessYellowPageDetailIndexPageViewModel
        {
            ListingGUIDFilter = listingGuid
        };

        if (listingGuid.HasValue)
        {
            var listing = await context.Listings
                .AsNoTracking()
                .Include(item => item.Category)
                .Include(item => item.Subcategory)
                .Include(item => item.Province)
                .Include(item => item.City)
                .FirstOrDefaultAsync(item => item.ListingGUID == listingGuid.Value && !item.DeletedInd);

            if (listing is null)
            {
                return NotFound();
            }

            if (!string.Equals(listing.Category?.Name, BusinessYellowPageCategoryName, StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Details", "ListingPage", new { id = listing.ListingGUID });
            }

            model.ListingCard = new BuySellDetailListingCardViewModel
            {
                ListingGUID = listing.ListingGUID,
                UserId = listing.UserId,
                Subject = listing.Subject,
                Description = listing.Description,
                CategoryId = listing.CategoryId,
                SubcategoryId = listing.SubcategoryId,
                ProvinceId = listing.ProvinceId,
                CityId = listing.CityId,
                KeyWords = listing.KeyWords,
                CategoryName = listing.Category?.Name,
                SubcategoryName = listing.Subcategory?.Name,
                ProvinceName = listing.Province?.Name,
                CityName = listing.City?.Name,
                Address = listing.Address,
                PostalCode = listing.PostalCode,
                Brand = listing.Brand,
                ManufactureYear = listing.ManufactureYear,
                Model = listing.Model,
                Quantity = listing.Quantity
            };

            model.CanManageFocusedListing = canManageByRole || (hasUserGuid && listing.UserId != Guid.Empty && listing.UserId == currentUserId);
            if (hasUserGuid)
            {
                model.IsFavorited = await context.FavoriteListings
                    .AsNoTracking()
                    .AnyAsync(item => item.ListingGUID == listing.ListingGUID && item.UserId == currentUserId && !item.DeletedInd);
            }

            var listingImages = await context.ListingImages
                .AsNoTracking()
                .Where(image => image.ListingGUID == listing.ListingGUID && !image.DeletedInd)
                .OrderBy(image => image.SorOrder)
                .ThenBy(image => image.CreatedDate)
                .Take(15)
                .Select(image => new BuySellDetailListingImageViewModel
                {
                    ListingImageGUID = image.ListingImageGUID,
                    ListingGUID = image.ListingGUID,
                    Title = image.Title,
                    SorOrder = image.SorOrder,
                    ThumbnailURL = image.ThumbnailURL,
                    ImageURL = image.ImageURL
                })
                .ToListAsync();

            foreach (var image in listingImages)
            {
                image.ThumbnailURL = ResolveListingImageUrl(image.ThumbnailURL);
                image.ImageURL = ResolveListingImageUrl(image.ImageURL);
            }

            model.ListingImages = listingImages;

            var similarCandidates = await context.Listings
                .AsNoTracking()
                .Where(item => !item.DeletedInd && item.ListingGUID != listing.ListingGUID)
                .Where(item => listing.ProvinceId <= 0 || item.ProvinceId == listing.ProvinceId)
                .Where(item => listing.CityId <= 0 || item.CityId == listing.CityId)
                .Where(item => listing.CategoryId <= 0 || item.CategoryId == listing.CategoryId)
                .Where(item => !listing.SubcategoryId.HasValue || item.SubcategoryId == listing.SubcategoryId)
                .ToListAsync();

            var sourceListingWords = BuildWordSet(listing.Subject, listing.KeyWords, listing.Description);
            var scoredSimilarCandidates = similarCandidates
                .Select(item => new
                {
                    Listing = item,
                    Score = sourceListingWords.Intersect(BuildWordSet(item.Subject, item.KeyWords, item.Description), StringComparer.OrdinalIgnoreCase).Count() * 10
                        + ComputePairTextScore(listing.Subject, item.Subject, exactWeight: 4, partialWeight: 2)
                        + ComputePairTextScore(listing.KeyWords, item.KeyWords, exactWeight: 5, partialWeight: 2)
                        + ComputePairTextScore(listing.Description, item.Description, exactWeight: 3, partialWeight: 1)
                })
                .OrderByDescending(item => item.Score)
                .Take(24)
                .ToList();

            var selectedSimilarListings = scoredSimilarCandidates
                .OrderBy(_ => Guid.NewGuid())
                .Take(8)
                .Select(item => item.Listing)
                .ToList();

            if (selectedSimilarListings.Count > 0)
            {
                var selectedListingGuids = selectedSimilarListings
                    .Select(item => item.ListingGUID)
                    .ToHashSet();

                var similarListingImages = await context.ListingImages
                    .AsNoTracking()
                    .Where(item => !item.DeletedInd && selectedListingGuids.Contains(item.ListingGUID))
                    .OrderBy(item => item.SorOrder)
                    .ThenBy(item => item.CreatedDate)
                    .Select(item => new
                    {
                        item.ListingGUID,
                        item.ThumbnailURL,
                        item.ImageURL
                    })
                    .ToListAsync();

                var thumbnailByListing = similarListingImages
                    .GroupBy(item => item.ListingGUID)
                    .ToDictionary(
                        group => group.Key,
                        group => group.Select(item => ResolveListingImageUrl(!string.IsNullOrWhiteSpace(item.ThumbnailURL) ? item.ThumbnailURL : item.ImageURL))
                            .FirstOrDefault(url => !string.IsNullOrWhiteSpace(url)));

                model.SimilarBusinesses = selectedSimilarListings
                    .Select(item => new BusinessYellowPageSimilarListingViewModel
                    {
                        ListingGUID = item.ListingGUID,
                        Subject = item.Subject,
                        ThumbnailURL = thumbnailByListing.TryGetValue(item.ListingGUID, out var thumbnailUrl) ? thumbnailUrl : null
                    })
                    .ToList();
            }

            var detail = await context.BusinessYellowPageDetails
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.ListingGUID == listing.ListingGUID && !item.DeletedInd);

            if (detail is not null)
            {
                model.HasExistingFocusDetail = true;
                model.FocusDetail = ToEditViewModel(detail);
            }
            else
            {
                model.FocusDetail = new BusinessYellowPageDetailEditViewModel
                {
                    ListingGUID = listing.ListingGUID
                };
            }
        }

        return View("~/Views/BusinessYellowPageDetail/Index.cshtml", model);
    }

    [HttpGet("DetailPageSliderAds/{listingGuid:guid}")]
    public async Task<IActionResult> DetailPageSliderAds(Guid listingGuid)
    {
        var listing = await context.Listings
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.ListingGUID == listingGuid && !item.DeletedInd);

        if (listing is null)
        {
            return Json(Array.Empty<object>());
        }

        var today = CanHappy.Common.EasternTime.Now.Date;
        var candidateAds = await context.Ads
            .AsNoTracking()
            .Include(item => item.AdSizeOption)
            .Where(item => !item.DeletedInd
                && item.ActiveInd
                && item.AdStatusId == 3
                && item.PublishDate < today
                && (!item.ExpiryDate.HasValue || item.ExpiryDate.Value > today)
                && item.AdSizeOption != null
                && item.AdSizeOption.Name == "1200x300DetailPageSlider2Ads"
                && !string.IsNullOrWhiteSpace(item.ImageURL)
                && !string.IsNullOrWhiteSpace(item.TargetURL)
                && (!item.ProvinceId.HasValue || item.ProvinceId.Value == listing.ProvinceId)
                && (!item.CityId.HasValue || item.CityId.Value == listing.CityId)
                && (!item.CategoryId.HasValue || item.CategoryId.Value == listing.CategoryId)
                && (!item.SubcategoryId.HasValue || item.SubcategoryId.Value == listing.SubcategoryId))
            .ToListAsync();

        if (candidateAds.Count == 0)
        {
            return Json(Array.Empty<object>());
        }

        var scoredAds = candidateAds
            .Select(item => new
            {
                Ad = item,
                Score = ComputeDetailPageAdScore(listing, item)
            })
            .OrderByDescending(item => item.Score)
            .Take(24)
            .OrderBy(_ => Guid.NewGuid())
            .Take(8)
            .Select(item => new
            {
                adGuid = item.Ad.AdGUID,
                subject = item.Ad.Subject,
                imageURL = item.Ad.ImageURL,
                targetURL = item.Ad.TargetURL,
                score = item.Score
            })
            .ToList();

        return Json(scoredAds);
    }

    [HttpPost("Favorite/{listingGuid:guid}")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Favorite(Guid listingGuid, string? returnUrl)
    {
        if (!TryGetCurrentUserGuid(out var currentUserId))
        {
            return Forbid();
        }

        var listing = await context.Listings
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.ListingGUID == listingGuid && !item.DeletedInd);
        if (listing is null)
        {
            return NotFound();
        }

        var favorite = await context.FavoriteListings
            .FirstOrDefaultAsync(item => item.ListingGUID == listingGuid && item.UserId == currentUserId);

        if (favorite is null)
        {
            var maxSortOrder = await context.FavoriteListings
                .Where(item => item.UserId == currentUserId && !item.DeletedInd)
                .Select(item => (int?)item.SortOrder)
                .MaxAsync() ?? -1;

            favorite = new FavoriteListing
            {
                FavoriteListingGUID = Guid.NewGuid(),
                ListingGUID = listingGuid,
                UserId = currentUserId,
                ListingURL = BuildListingUrl(listingGuid, returnUrl),
                ListingSubject = (listing.Subject ?? string.Empty).Trim(),
                SortOrder = maxSortOrder + 1,
                DeletedInd = false,
                CreatedBy = User.Identity?.Name ?? string.Empty,
                ModifiedBy = User.Identity?.Name ?? string.Empty,
                CreatedDate = CanHappy.Common.EasternTime.Now,
                ModifiedDate = CanHappy.Common.EasternTime.Now
            };

            if (favorite.ListingSubject.Length > 50)
            {
                favorite.ListingSubject = favorite.ListingSubject[..50];
            }

            if (favorite.ListingURL.Length > 200)
            {
                favorite.ListingURL = favorite.ListingURL[..200];
            }

            context.FavoriteListings.Add(favorite);
        }
        else
        {
            favorite.DeletedInd = false;
            favorite.ListingURL = BuildListingUrl(listingGuid, returnUrl);
            favorite.ListingSubject = (listing.Subject ?? string.Empty).Trim();
            if (favorite.ListingSubject.Length > 50)
            {
                favorite.ListingSubject = favorite.ListingSubject[..50];
            }
            if (favorite.ListingURL.Length > 200)
            {
                favorite.ListingURL = favorite.ListingURL[..200];
            }
            favorite.ModifiedBy = User.Identity?.Name ?? string.Empty;
            favorite.ModifiedDate = CanHappy.Common.EasternTime.Now;
        }

        await context.SaveChangesAsync();
        TempData["MessageSuccess"] = "Listing added to favorites.";
        return RedirectToLocalOrIndex(returnUrl, listingGuid);
    }

    [HttpPost("Unfavorite/{listingGuid:guid}")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unfavorite(Guid listingGuid, string? returnUrl)
    {
        if (!TryGetCurrentUserGuid(out var currentUserId))
        {
            return Forbid();
        }

        var favorite = await context.FavoriteListings
            .FirstOrDefaultAsync(item => item.ListingGUID == listingGuid && item.UserId == currentUserId && !item.DeletedInd);
        if (favorite is not null)
        {
            favorite.DeletedInd = true;
            favorite.ModifiedBy = User.Identity?.Name ?? string.Empty;
            favorite.ModifiedDate = CanHappy.Common.EasternTime.Now;
            await context.SaveChangesAsync();
            TempData["MessageSuccess"] = "Listing removed from favorites.";
        }

        return RedirectToLocalOrIndex(returnUrl, listingGuid);
    }

    [HttpGet("Create")]
    [Authorize]
    public async Task<IActionResult> Create(Guid? listingGuid)
    {
        if (!TryGetCurrentUserGuid(out var currentUserId) && !User.IsInRole("Admin") && !User.IsInRole("Clerk"))
        {
            return Forbid();
        }

        if (listingGuid.HasValue)
        {
            var listing = await context.Listings
                .AsNoTracking()
                .Include(item => item.Category)
                .FirstOrDefaultAsync(item => item.ListingGUID == listingGuid.Value && !item.DeletedInd);

            if (listing is null)
            {
                return NotFound();
            }

            if (!string.Equals(listing.Category?.Name, BusinessYellowPageCategoryName, StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            if (!CanManageListing(listing, currentUserId))
            {
                return Forbid();
            }

            var hasExisting = await context.BusinessYellowPageDetails
                .AsNoTracking()
                .AnyAsync(item => item.ListingGUID == listingGuid.Value && !item.DeletedInd);

            if (hasExisting)
            {
                TempData["MessageError"] = "The listing details already exist! Edit them if changes needed.";
                return RedirectToAction(nameof(Index), new { listingGuid = listingGuid.Value });
            }
        }

        await PopulateListingSelectListAsync(currentUserId, listingGuid);
        return View("~/Views/BusinessYellowPageDetail/Create.cshtml", new BusinessYellowPageDetailEditViewModel
        {
            ListingGUID = listingGuid ?? Guid.Empty
        });
    }

    [HttpPost("Create")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BusinessYellowPageDetailEditViewModel model)
    {
        if (!TryGetCurrentUserGuid(out var currentUserId) && !User.IsInRole("Admin") && !User.IsInRole("Clerk"))
        {
            return Forbid();
        }

        var listing = await context.Listings
            .AsNoTracking()
            .Include(item => item.Category)
            .FirstOrDefaultAsync(item => item.ListingGUID == model.ListingGUID && !item.DeletedInd);

        if (listing is null)
        {
            ModelState.AddModelError(nameof(model.ListingGUID), "Listing not found.");
        }
        else
        {
            if (!string.Equals(listing.Category?.Name, BusinessYellowPageCategoryName, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(model.ListingGUID), "Listing category must be Business Yellow Page.");
            }
            else if (!CanManageListing(listing, currentUserId))
            {
                return Forbid();
            }
        }

        var hasExisting = await context.BusinessYellowPageDetails.AnyAsync(item => item.ListingGUID == model.ListingGUID && !item.DeletedInd);
        if (hasExisting)
        {
            ModelState.AddModelError(nameof(model.ListingGUID), "This listing already has Business Yellow Page details.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateListingSelectListAsync(currentUserId, model.ListingGUID);
            return View("~/Views/BusinessYellowPageDetail/Create.cshtml", model);
        }

        var entity = new BusinessYellowPageDetail
        {
            BusinessYellowPageDetailGUID = Guid.NewGuid(),
            ListingGUID = model.ListingGUID,
            BusinessHours = model.BusinessHours,
            BusinessStyle = model.BusinessStyle,
            ProductsAndServices = model.ProductsAndServices,
            Specialties = model.Specialties,
            LanguagesSpoken = model.LanguagesSpoken,
            GeneralBeforeTaxPayPerPerson = model.GeneralBeforeTaxPayPerPerson,
            MethodOfPayments = model.MethodOfPayments,
            HowToGetThere = model.HowToGetThere,
            AdditionalInfo = model.AdditionalInfo,
            WebSiteURL = model.WebSiteURL,
            ActiveInd = model.ActiveInd,
            DeletedInd = false,
            CreatedBy = User.Identity?.Name,
            CreatedDate = CanHappy.Common.EasternTime.Now
        };

        context.BusinessYellowPageDetails.Add(entity);
        await context.SaveChangesAsync();

        return RedirectToAction(nameof(Index), new { listingGuid = model.ListingGUID });
    }

    [HttpGet("Edit/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Edit(Guid id)
    {
        if (!TryGetCurrentUserGuid(out var currentUserId) && !User.IsInRole("Admin") && !User.IsInRole("Clerk"))
        {
            return Forbid();
        }

        var entity = await context.BusinessYellowPageDetails
            .Include(item => item.Listing)
            .FirstOrDefaultAsync(item => item.BusinessYellowPageDetailGUID == id && !item.DeletedInd);

        if (entity is null || entity.Listing is null)
        {
            return NotFound();
        }

        if (!CanManageListing(entity.Listing, currentUserId))
        {
            return Forbid();
        }

        var model = ToEditViewModel(entity);
        await PopulateListingSelectListAsync(currentUserId, entity.ListingGUID);
        return View("~/Views/BusinessYellowPageDetail/Edit.cshtml", model);
    }

    [HttpPost("Edit/{id:guid}")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, BusinessYellowPageDetailEditViewModel model)
    {
        if (model.BusinessYellowPageDetailGUID != id)
        {
            return NotFound();
        }

        if (!TryGetCurrentUserGuid(out var currentUserId) && !User.IsInRole("Admin") && !User.IsInRole("Clerk"))
        {
            return Forbid();
        }

        var entity = await context.BusinessYellowPageDetails
            .Include(item => item.Listing)
            .FirstOrDefaultAsync(item => item.BusinessYellowPageDetailGUID == id && !item.DeletedInd);

        if (entity is null || entity.Listing is null)
        {
            return NotFound();
        }

        if (!CanManageListing(entity.Listing, currentUserId))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            await PopulateListingSelectListAsync(currentUserId, entity.ListingGUID);
            return View("~/Views/BusinessYellowPageDetail/Edit.cshtml", model);
        }

        entity.BusinessHours = model.BusinessHours;
        entity.BusinessStyle = model.BusinessStyle;
        entity.ProductsAndServices = model.ProductsAndServices;
        entity.Specialties = model.Specialties;
        entity.LanguagesSpoken = model.LanguagesSpoken;
        entity.GeneralBeforeTaxPayPerPerson = model.GeneralBeforeTaxPayPerPerson;
        entity.MethodOfPayments = model.MethodOfPayments;
        entity.HowToGetThere = model.HowToGetThere;
        entity.AdditionalInfo = model.AdditionalInfo;
        entity.WebSiteURL = model.WebSiteURL;
        entity.ActiveInd = model.ActiveInd;
        entity.ModifiedBy = User.Identity?.Name;
        entity.ModifiedDate = CanHappy.Common.EasternTime.Now;

        await context.SaveChangesAsync();

        return RedirectToAction(nameof(Index), new { listingGuid = entity.ListingGUID });
    }

    [HttpGet("Delete/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (!TryGetCurrentUserGuid(out var currentUserId) && !User.IsInRole("Admin") && !User.IsInRole("Clerk"))
        {
            return Forbid();
        }

        var entity = await context.BusinessYellowPageDetails
            .AsNoTracking()
            .Include(item => item.Listing)
            .FirstOrDefaultAsync(item => item.BusinessYellowPageDetailGUID == id && !item.DeletedInd);

        if (entity is null || entity.Listing is null)
        {
            return NotFound();
        }

        if (!CanManageListing(entity.Listing, currentUserId))
        {
            return Forbid();
        }

        return View("~/Views/BusinessYellowPageDetail/Delete.cshtml", ToEditViewModel(entity));
    }

    [HttpPost("Delete/{id:guid}"), ActionName("Delete")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        if (!TryGetCurrentUserGuid(out var currentUserId) && !User.IsInRole("Admin") && !User.IsInRole("Clerk"))
        {
            return Forbid();
        }

        var entity = await context.BusinessYellowPageDetails
            .Include(item => item.Listing)
            .FirstOrDefaultAsync(item => item.BusinessYellowPageDetailGUID == id && !item.DeletedInd);

        if (entity is null || entity.Listing is null)
        {
            return NotFound();
        }

        if (!CanManageListing(entity.Listing, currentUserId))
        {
            return Forbid();
        }

        entity.DeletedInd = true;
        entity.ModifiedBy = User.Identity?.Name;
        entity.ModifiedDate = CanHappy.Common.EasternTime.Now;
        await context.SaveChangesAsync();

        return RedirectToAction(nameof(Index), new { listingGuid = entity.ListingGUID });
    }

    [HttpGet("SalesSpecials/{listingGuid:guid}")]
    public async Task<IActionResult> SalesSpecials(Guid listingGuid)
    {
        var listing = await context.Listings
            .AsNoTracking()
            .Include(item => item.Category)
            .FirstOrDefaultAsync(item => item.ListingGUID == listingGuid && !item.DeletedInd);

        if (listing is null)
        {
            return Json(Array.Empty<object>());
        }

        if (!string.Equals(listing.Category?.Name, BusinessYellowPageCategoryName, StringComparison.OrdinalIgnoreCase))
        {
            return Json(Array.Empty<object>());
        }

        var specials = await context.SalesSpecialsImages
            .AsNoTracking()
            .Where(item => item.ListingGUID == listingGuid && !item.DeletedInd)
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.CreatedDate)
            .Take(50)
            .Select(item => new SalesSpecialsImageViewModel
            {
                SalesSpecialsImageGUID = item.SalesSpecialsImageGUID,
                ListingGUID = item.ListingGUID,
                Title = item.Title,
                SortOrder = item.SortOrder,
                Price = item.Price,
                SalePrice = item.SalePrice,
                PercentOff = item.PercentOff,
                SaleBegin = item.SaleBegin,
                SaleEnd = item.SaleEnd,
                Description = item.Description,
                ThumbnailURL = item.ThumbnailURL,
                ImageURL = item.ImageURL
            })
            .ToListAsync();

        foreach (var item in specials)
        {
            item.ThumbnailURL = ResolveListingImageUrl(item.ThumbnailURL);
            item.ImageURL = ResolveListingImageUrl(item.ImageURL);
        }

        return Json(specials);
    }

    [HttpPost("UpsertSalesSpecialPhoto/{listingGuid:guid}")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpsertSalesSpecialPhoto(
        Guid listingGuid,
        Guid? salesSpecialsImageGuid,
        string? title,
        int? sortOrder,
        decimal? price,
        decimal? salePrice,
        string? percentOff,
        DateTime? saleBegin,
        DateTime? saleEnd,
        string? description,
        string? croppedImageData,
        string? thumbnailImageData)
    {
        if (!TryGetCurrentUserGuid(out var currentUserId) && !User.IsInRole("Admin") && !User.IsInRole("Clerk"))
        {
            return Forbid();
        }

        var listing = await context.Listings
            .Include(item => item.Category)
            .FirstOrDefaultAsync(item => item.ListingGUID == listingGuid && !item.DeletedInd);

        if (listing is null)
        {
            return NotFound();
        }

        if (!string.Equals(listing.Category?.Name, BusinessYellowPageCategoryName, StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        if (!CanManageListing(listing, currentUserId))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(croppedImageData))
        {
            TempData["MessageError"] = "Image data is required.";
            return RedirectToAction(nameof(Index), new { listingGuid });
        }

        var imagePath = await SaveListingImageFromDataUrlAsync(croppedImageData);
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            TempData["MessageError"] = "Invalid image data.";
            return RedirectToAction(nameof(Index), new { listingGuid });
        }

        var thumbnailPath = string.IsNullOrWhiteSpace(thumbnailImageData)
            ? null
            : await SaveListingImageFromDataUrlAsync(thumbnailImageData);

        if (string.IsNullOrWhiteSpace(thumbnailPath))
        {
            thumbnailPath = imagePath;
        }

        SalesSpecialsImage? entity = null;
        if (salesSpecialsImageGuid.HasValue)
        {
            entity = await context.SalesSpecialsImages
                .FirstOrDefaultAsync(item => item.SalesSpecialsImageGUID == salesSpecialsImageGuid.Value && item.ListingGUID == listingGuid && !item.DeletedInd);
        }

        if (entity is null)
        {
            var imageCount = await context.SalesSpecialsImages
                .CountAsync(item => item.ListingGUID == listingGuid && !item.DeletedInd);

            if (imageCount >= 50)
            {
                TempData["MessageError"] = "You can upload up to 50 sales & specials photos for a listing.";
                return RedirectToAction(nameof(Index), new { listingGuid });
            }

            entity = new SalesSpecialsImage
            {
                SalesSpecialsImageGUID = Guid.NewGuid(),
                ListingGUID = listingGuid,
                CreatedBy = User.Identity?.Name,
                CreatedDate = CanHappy.Common.EasternTime.Now.Date,
                DeletedInd = false,
                SampleInd = false
            };
            context.SalesSpecialsImages.Add(entity);
        }

        entity.Title = string.IsNullOrWhiteSpace(title) ? null : title.Trim();
        entity.SortOrder = sortOrder.GetValueOrDefault(0);
        entity.Price = price;
        entity.SalePrice = salePrice;
        entity.PercentOff = string.IsNullOrWhiteSpace(percentOff) ? null : percentOff.Trim();
        entity.SaleBegin = saleBegin?.Date;
        entity.SaleEnd = saleEnd?.Date;
        entity.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        entity.ImageURL = imagePath;
        entity.ThumbnailURL = thumbnailPath;
        entity.ModifiedBy = User.Identity?.Name;
        entity.ModifiedDate = CanHappy.Common.EasternTime.Now.Date;

        await context.SaveChangesAsync();

        TempData["MessageSuccess"] = "Sales & specials photo saved.";
        return RedirectToAction(nameof(Index), new { listingGuid });
    }

    [HttpPost("DeleteSalesSpecialPhoto/{listingGuid:guid}/{salesSpecialsImageGuid:guid}")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSalesSpecialPhoto(Guid listingGuid, Guid salesSpecialsImageGuid)
    {
        if (!TryGetCurrentUserGuid(out var currentUserId) && !User.IsInRole("Admin") && !User.IsInRole("Clerk"))
        {
            return Forbid();
        }

        var listing = await context.Listings
            .Include(item => item.Category)
            .FirstOrDefaultAsync(item => item.ListingGUID == listingGuid && !item.DeletedInd);
        if (listing is null)
        {
            return NotFound();
        }

        if (!string.Equals(listing.Category?.Name, BusinessYellowPageCategoryName, StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        if (!CanManageListing(listing, currentUserId))
        {
            return Forbid();
        }

        var entity = await context.SalesSpecialsImages
            .FirstOrDefaultAsync(item => item.SalesSpecialsImageGUID == salesSpecialsImageGuid && item.ListingGUID == listingGuid && !item.DeletedInd);

        if (entity is null)
        {
            TempData["MessageError"] = "Sales & specials photo not found.";
            return RedirectToAction(nameof(Index), new { listingGuid });
        }

        entity.DeletedInd = true;
        entity.ModifiedBy = User.Identity?.Name;
        entity.ModifiedDate = CanHappy.Common.EasternTime.Now.Date;
        await context.SaveChangesAsync();

        TempData["MessageSuccess"] = "Sales & specials photo deleted.";
        return RedirectToAction(nameof(Index), new { listingGuid });
    }

    [HttpPost("UpsertPhoto/{listingGuid:guid}")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpsertPhoto(Guid listingGuid, Guid? listingImageGuid, string? title, int? sorOrder, string? croppedImageData, string? thumbnailImageData)
    {
        if (!TryGetCurrentUserGuid(out var currentUserId) && !User.IsInRole("Admin") && !User.IsInRole("Clerk"))
        {
            return Forbid();
        }

        var listing = await context.Listings.FirstOrDefaultAsync(item => item.ListingGUID == listingGuid && !item.DeletedInd);
        if (listing is null)
        {
            return NotFound();
        }

        if (!CanManageListing(listing, currentUserId))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(croppedImageData))
        {
            TempData["MessageError"] = "Image data is required.";
            return RedirectToAction(nameof(Index), new { listingGuid });
        }

        var imagePath = await SaveListingImageFromDataUrlAsync(croppedImageData);
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            TempData["MessageError"] = "Invalid image data.";
            return RedirectToAction(nameof(Index), new { listingGuid });
        }

        var thumbnailPath = string.IsNullOrWhiteSpace(thumbnailImageData)
            ? null
            : await SaveListingImageFromDataUrlAsync(thumbnailImageData);

        if (string.IsNullOrWhiteSpace(thumbnailPath))
        {
            thumbnailPath = imagePath;
        }

        ListingImage? listingImage = null;
        if (listingImageGuid.HasValue)
        {
            listingImage = await context.ListingImages.FirstOrDefaultAsync(item => item.ListingImageGUID == listingImageGuid.Value && item.ListingGUID == listingGuid && !item.DeletedInd);
        }

        if (listingImage is null)
        {
            var imageCount = await context.ListingImages.CountAsync(item => item.ListingGUID == listingGuid && !item.DeletedInd);
            if (imageCount >= 15)
            {
                TempData["MessageError"] = "You can upload up to 15 photos for a listing.";
                return RedirectToAction(nameof(Index), new { listingGuid });
            }

            listingImage = new ListingImage
            {
                ListingImageGUID = Guid.NewGuid(),
                ListingGUID = listingGuid,
                CreatedBy = User.Identity?.Name,
                CreatedDate = CanHappy.Common.EasternTime.Now,
                DeletedInd = false,
                SampleInd = false
            };
            context.ListingImages.Add(listingImage);
        }

        listingImage.Title = string.IsNullOrWhiteSpace(title) ? null : title.Trim();
        listingImage.SorOrder = sorOrder.GetValueOrDefault(0);
        listingImage.ThumbnailURL = thumbnailPath;
        listingImage.ImageURL = imagePath;

        await context.SaveChangesAsync();

        TempData["MessageSuccess"] = "Photo saved.";
        return RedirectToAction(nameof(Index), new { listingGuid });
    }

    [HttpPost("DeletePhoto/{listingGuid:guid}/{listingImageGuid:guid}")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeletePhoto(Guid listingGuid, Guid listingImageGuid)
    {
        if (!TryGetCurrentUserGuid(out var currentUserId) && !User.IsInRole("Admin") && !User.IsInRole("Clerk"))
        {
            return Forbid();
        }

        var listing = await context.Listings.FirstOrDefaultAsync(item => item.ListingGUID == listingGuid && !item.DeletedInd);
        if (listing is null)
        {
            return NotFound();
        }

        if (!CanManageListing(listing, currentUserId))
        {
            return Forbid();
        }

        var listingImage = await context.ListingImages.FirstOrDefaultAsync(item => item.ListingImageGUID == listingImageGuid && item.ListingGUID == listingGuid && !item.DeletedInd);
        if (listingImage is null)
        {
            TempData["MessageError"] = "Photo not found.";
            return RedirectToAction(nameof(Index), new { listingGuid });
        }

        listingImage.DeletedInd = true;
        await context.SaveChangesAsync();

        TempData["MessageSuccess"] = "Photo deleted.";
        return RedirectToAction(nameof(Index), new { listingGuid });
    }

    private BusinessYellowPageDetailEditViewModel ToEditViewModel(BusinessYellowPageDetail detail)
    {
        return new BusinessYellowPageDetailEditViewModel
        {
            BusinessYellowPageDetailGUID = detail.BusinessYellowPageDetailGUID,
            ListingGUID = detail.ListingGUID,
            BusinessHours = detail.BusinessHours,
            BusinessStyle = detail.BusinessStyle,
            ProductsAndServices = detail.ProductsAndServices,
            Specialties = detail.Specialties,
            LanguagesSpoken = detail.LanguagesSpoken,
            GeneralBeforeTaxPayPerPerson = detail.GeneralBeforeTaxPayPerPerson,
            MethodOfPayments = detail.MethodOfPayments,
            HowToGetThere = detail.HowToGetThere,
            AdditionalInfo = detail.AdditionalInfo,
            WebSiteURL = detail.WebSiteURL,
            ActiveInd = detail.ActiveInd
        };
    }

    private bool TryGetCurrentUserGuid(out Guid userId)
    {
        userId = Guid.Empty;
        var userIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return !string.IsNullOrWhiteSpace(userIdText) && Guid.TryParse(userIdText, out userId);
    }

    private bool CanManageListing(Listing listing, Guid currentUserId)
    {
        if (User.IsInRole("Admin") || User.IsInRole("Clerk"))
        {
            return true;
        }

        return listing.UserId != Guid.Empty && currentUserId != Guid.Empty && listing.UserId == currentUserId;
    }

    private string BuildListingUrl(Guid listingGuid, string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return returnUrl;
        }

        return $"/BusinessYellowPageDetail/Index?listingGuid={listingGuid}";
    }

    private IActionResult RedirectToLocalOrIndex(string? returnUrl, Guid listingGuid)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction(nameof(Index), new { listingGuid });
    }

    private async Task PopulateListingSelectListAsync(Guid currentUserId, Guid? selectedListingId = null)
    {
        var isPrivileged = User.IsInRole("Admin") || User.IsInRole("Clerk");

        var query = context.Listings
            .AsNoTracking()
            .Include(item => item.Category)
            .Where(listing => !listing.DeletedInd)
            .Where(listing => listing.Category != null && listing.Category.Name == BusinessYellowPageCategoryName)
            .Where(listing => !context.BusinessYellowPageDetails.Any(detail => detail.ListingGUID == listing.ListingGUID && !detail.DeletedInd)
                || listing.ListingGUID == selectedListingId);

        if (!isPrivileged)
        {
            query = query.Where(listing => listing.UserId == currentUserId && listing.UserId != Guid.Empty);
        }

        var listings = await query
            .OrderByDescending(listing => listing.CreatedDate)
            .ToListAsync();

        ViewData["ListingGUID"] = new SelectList(
            listings.Select(listing => new
            {
                ListingGUID = listing.ListingGUID,
                DisplayText = $"{listing.Subject} ({listing.ListingGUID})"
            }),
            "ListingGUID",
            "DisplayText",
            selectedListingId);
    }

    private async Task<string?> SaveListingImageFromDataUrlAsync(string dataUrl)
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
        var folderPath = MediaPathHelper.BuildPhysicalFolderPath(environment.WebRootPath, ListImageFolder);

        Directory.CreateDirectory(folderPath);
        var filePath = Path.Combine(folderPath, fileName);
        await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);

        return fileName;
    }

    private string ListImageFolder => MediaPathHelper.ResolveWebFolder(configuration, "ListImageFolder", "/ListingImages");

    private string? ResolveListingImageUrl(string? url)
    {
        return MediaPathHelper.BuildMediaUrl(url, ListImageFolder);
    }

    private static int ComputeDetailPageAdScore(Listing listing, Ad ad)
    {
        var listingWords = BuildWordSet(listing.Subject, listing.KeyWords, listing.Description);
        var adWords = BuildWordSet(ad.Subject, ad.KeyWords, ad.Description);

        var score = 0;
        if (listingWords.Count > 0 && adWords.Count > 0)
        {
            score += listingWords.Intersect(adWords, StringComparer.OrdinalIgnoreCase).Count() * 10;
        }

        score += ComputePairTextScore(listing.Subject, ad.Subject, exactWeight: 4, partialWeight: 2);
        score += ComputePairTextScore(listing.KeyWords, ad.KeyWords, exactWeight: 5, partialWeight: 2);
        score += ComputePairTextScore(listing.Description, ad.Description, exactWeight: 3, partialWeight: 1);

        return score;
    }

    private static int ComputePairTextScore(string? leftText, string? rightText, int exactWeight, int partialWeight)
    {
        if (string.IsNullOrWhiteSpace(leftText) || string.IsNullOrWhiteSpace(rightText))
        {
            return 0;
        }

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
}
