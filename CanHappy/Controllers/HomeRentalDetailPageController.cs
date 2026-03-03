using CanHappy.Data;
using CanHappy.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CanHappy.Controllers;

[Route("HomeRentalDetail")]
public class HomeRentalDetailPageController(ApplicationDbContext context, IWebHostEnvironment environment, IConfiguration configuration) : Controller
{
    private const string HomeRentalCategoryName = "Home Rental";

    [HttpGet("")]
    public async Task<IActionResult> Index(Guid? listingGuid)
    {
        ViewData["HomeRentalDetailSliderWaitSeconds"] = Math.Max(0, configuration.GetValue<int?>("HomeRentalDetailSliderWaitSeconds") ?? 2);
        ViewData["HomeRentalDetailAdSlidingInterval"] = Math.Max(1, configuration.GetValue<int?>("HomeRentalDetailAdSlidingInterval") ?? 5);
        ViewData["HomeRentalDetailAdSliderFadingMode"] = configuration.GetValue<string>("HomeRentalDetailAdSliderFadingMode") ?? "homeAdFade";
        ViewData["HomeRentalDetailAdPixelResolveTransitionPeriodMiliSeconds"] = Math.Max(100, configuration.GetValue<int?>("homeRentalDetailAdPixelResolveTransitionPeriodMiliSeconds") ?? 1000);
        ViewData["HomeRentalDetailAdFadeTransitionMilliSeconds"] = Math.Max(100, configuration.GetValue<int?>("homeRentalDetailAdFadeTransitionMilliSeconds") ?? 450);
        ViewData["HomeRentalDetailSlideTransitionMilliSeconds"] = Math.Max(100, configuration.GetValue<int?>("homeRentalDetailSlideTransitionMilliSeconds") ?? 650);
        ViewData["HomeRentalDetailAdSlideDistancePercent"] = Math.Clamp(configuration.GetValue<int?>("homeRentalDetailAdSlideDistancePercent") ?? 8, 1, 30);
        ViewData["HomeRentalDetailAdTransitionEasing"] = configuration.GetValue<string>("homeRentalDetailAdTransitionEasing") ?? "ease-in-out";

        var hasUserGuid = TryGetCurrentUserGuid(out var currentUserId);
        var canManageByRole = User.IsInRole("Admin") || User.IsInRole("Clerk");

        var model = new HomeRentalDetailIndexPageViewModel
        {
            ListingGUIDFilter = listingGuid
        };

        var query = context.HomeRentalDetails
            .AsNoTracking()
            .Include(item => item.Listing)
            .ThenInclude(listing => listing!.Category)
            .Include(item => item.PropertyType)
            .Include(item => item.RentalPropertyType)
            .Where(item => !item.DeletedInd
                && item.Listing != null
                && !item.Listing.DeletedInd
                && item.Listing.Category != null
                && item.Listing.Category.Name == HomeRentalCategoryName)
            .AsQueryable();

        if (listingGuid.HasValue)
        {
            query = query.Where(item => item.ListingGUID == listingGuid.Value);
            ViewData["ListingGUID"] = listingGuid.Value;

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

            if (!string.Equals(listing.Category?.Name, HomeRentalCategoryName, StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Index", "ListingPage", new { focusListingId = listing.ListingGUID });
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
                Price = listing.Price,
                DiscountPercent = listing.DiscountPercent,
                DiscountBeginDate = listing.DiscountBeginDate,
                DiscountEndDate = listing.DiscountEndDate,
                Brand = listing.Brand,
                Condition = listing.Condition,
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

            model.ListingImages = await context.ListingImages
                .AsNoTracking()
                .Where(image => image.ListingGUID == listing.ListingGUID && !image.DeletedInd)
                .OrderBy(image => image.SorOrder)
                .ThenBy(image => image.CreatedDate)
                .Take(30)
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
        }

        model.Items = await query
            .OrderByDescending(item => item.CreatedDate)
            .Select(item => new HomeRentalDetailIndexItemViewModel
            {
                HomeRentalDetailGUID = item.HomeRentalDetailGUID,
                ListingGUID = item.ListingGUID,
                Subject = item.Listing!.Subject,
                Description = item.Listing.Description,
                CategoryId = item.Listing.CategoryId,
                SubcategoryId = item.Listing.SubcategoryId,
                ProvinceId = item.Listing.ProvinceId,
                CityId = item.Listing.CityId,
                Price = item.Listing.Price,
                Brand = item.Listing.Brand,
                Condition = item.Listing.Condition,
                ManufactureYear = item.Listing.ManufactureYear,
                ListingModel = item.Listing.Model,
                Quantity = item.Listing.Quantity,
                PropertyTypeId = item.PropertyTypeId,
                PropertyTypeName = item.PropertyType != null ? item.PropertyType.Name : null,
                NumberOfStoreys = item.NumberOfStoreys,
                PriceNegotiableInd = item.PriceNegotiableInd,
                RentalPropertyTypeId = item.RentalPropertyTypeId,
                RentalPropertyTypeName = item.RentalPropertyType != null ? item.RentalPropertyType.Name : null,
                RentalSquareFeet = item.RentalSquareFeet,
                Washrooms = item.Washrooms,
                SharedWashroomInd = item.SharedWashroomInd,
                Baths = item.Baths,
                YearBuilt = item.YearBuilt,
                ParkingSpots = item.ParkingSpots,
                RentalParkingSpots = item.RentalParkingSpots,
                ParkingIncludedInd = item.ParkingIncludedInd,
                PreferredRentalStartDate = item.PreferredRentalStartDate,
                PreferredRentalTerm = item.PreferredRentalTerm,
                RentalTermNegotiableInd = item.RentalTermNegotiableInd,
                FurnishedInd = item.FurnishedInd,
                WaterIncludedInd = item.WaterIncludedInd,
                HeatingIncludedInd = item.HeatingIncludedInd,
                HydroElectricityIncludedInd = item.HydroElectricityIncludedInd,
                InternetWiFiIncludedInd = item.InternetWiFiIncludedInd,
                AdditionalInfo = item.AdditionalInfo,
                CanManage = canManageByRole || (hasUserGuid && item.Listing.UserId != Guid.Empty && item.Listing.UserId == currentUserId)
            })
            .ToListAsync();

        if (listingGuid.HasValue && model.ListingCard is not null)
        {
            var focusDetail = model.Items.FirstOrDefault(item => item.ListingGUID == listingGuid.Value);
            if (focusDetail is not null)
            {
                model.HasExistingFocusDetail = true;
                model.FocusDetail = new HomeRentalDetailEditViewModel
                {
                    HomeRentalDetailGUID = focusDetail.HomeRentalDetailGUID,
                    ListingGUID = focusDetail.ListingGUID,
                    Subject = focusDetail.Subject,
                    Description = focusDetail.Description,
                    CategoryId = focusDetail.CategoryId,
                    SubcategoryId = focusDetail.SubcategoryId,
                    ProvinceId = focusDetail.ProvinceId,
                    CityId = focusDetail.CityId,
                    Price = focusDetail.Price,
                    Brand = focusDetail.Brand,
                    Condition = focusDetail.Condition,
                    ManufactureYear = focusDetail.ManufactureYear,
                    ListingModel = focusDetail.ListingModel,
                    Quantity = focusDetail.Quantity,
                    PropertyTypeId = focusDetail.PropertyTypeId,
                    PropertyTypeName = focusDetail.PropertyTypeName,
                    NumberOfStoreys = focusDetail.NumberOfStoreys,
                    PriceNegotiableInd = focusDetail.PriceNegotiableInd,
                    RentalPropertyTypeId = focusDetail.RentalPropertyTypeId,
                    RentalPropertyTypeName = focusDetail.RentalPropertyTypeName,
                    RentalSquareFeet = focusDetail.RentalSquareFeet,
                    Washrooms = focusDetail.Washrooms,
                    SharedWashroomInd = focusDetail.SharedWashroomInd,
                    Baths = focusDetail.Baths,
                    YearBuilt = focusDetail.YearBuilt,
                    ParkingSpots = focusDetail.ParkingSpots,
                    RentalParkingSpots = focusDetail.RentalParkingSpots,
                    ParkingIncludedInd = focusDetail.ParkingIncludedInd,
                    PreferredRentalStartDate = focusDetail.PreferredRentalStartDate,
                    PreferredRentalTerm = focusDetail.PreferredRentalTerm,
                    RentalTermNegotiableInd = focusDetail.RentalTermNegotiableInd,
                    FurnishedInd = focusDetail.FurnishedInd,
                    WaterIncludedInd = focusDetail.WaterIncludedInd,
                    HeatingIncludedInd = focusDetail.HeatingIncludedInd,
                    HydroElectricityIncludedInd = focusDetail.HydroElectricityIncludedInd,
                    InternetWiFiIncludedInd = focusDetail.InternetWiFiIncludedInd,
                    AdditionalInfo = focusDetail.AdditionalInfo
                };
            }
            else
            {
                model.FocusDetail = new HomeRentalDetailEditViewModel
                {
                    ListingGUID = model.ListingCard.ListingGUID,
                    Subject = model.ListingCard.Subject,
                    Description = model.ListingCard.Description,
                    CategoryId = model.ListingCard.CategoryId,
                    SubcategoryId = model.ListingCard.SubcategoryId,
                    ProvinceId = model.ListingCard.ProvinceId,
                    CityId = model.ListingCard.CityId,
                    Price = model.ListingCard.Price,
                    Brand = model.ListingCard.Brand,
                    Condition = model.ListingCard.Condition,
                    ManufactureYear = model.ListingCard.ManufactureYear,
                    ListingModel = model.ListingCard.Model,
                    Quantity = model.ListingCard.Quantity,
                    SharedWashroomInd = true
                };
            }
        }

        return View("~/Views/HomeRentalDetail/Index.cshtml", model);
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

            if (!string.Equals(listing.Category?.Name, HomeRentalCategoryName, StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            if (!CanManageListing(listing, currentUserId))
            {
                return Forbid();
            }

            var hasExisting = await context.HomeRentalDetails
                .AsNoTracking()
                .AnyAsync(item => item.ListingGUID == listingGuid.Value && !item.DeletedInd);

            if (hasExisting)
            {
                TempData["MessageError"] = "The listing details already exist! Edit them if changes needed.";
                return RedirectToAction(nameof(Index), new { listingGuid = listingGuid.Value });
            }
        }

        await PopulateListingSelectListAsync(currentUserId, listingGuid);
        await PopulatePropertyTypeSelectListAsync();

        return View("~/Views/HomeRentalDetail/Create.cshtml", new HomeRentalDetailEditViewModel
        {
            ListingGUID = listingGuid ?? Guid.Empty,
            SharedWashroomInd = true
        });
    }

    [HttpPost("Create")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ListingGUID,PropertyTypeId,NumberOfStoreys,PriceNegotiableInd,RentalPropertyTypeId,RentalSquareFeet,Washrooms,SharedWashroomInd,Baths,YearBuilt,ParkingSpots,RentalParkingSpots,ParkingIncludedInd,PreferredRentalStartDate,PreferredRentalTerm,RentalTermNegotiableInd,FurnishedInd,WaterIncludedInd,HeatingIncludedInd,HydroElectricityIncludedInd,InternetWiFiIncludedInd,AdditionalInfo")] HomeRentalDetailEditViewModel model)
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
            if (!string.Equals(listing.Category?.Name, HomeRentalCategoryName, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(model.ListingGUID), "Listing category must be Home Rental.");
            }
            else if (!CanManageListing(listing, currentUserId))
            {
                return Forbid();
            }
        }

        var propertyTypeExists = await context.PropertyTypes.AsNoTracking().AnyAsync(item => item.PropertyTypeId == model.PropertyTypeId && !item.DeletedInd);
        if (!propertyTypeExists)
        {
            ModelState.AddModelError(nameof(model.PropertyTypeId), "Property type is invalid.");
        }

        var rentalPropertyTypeExists = await context.PropertyTypes.AsNoTracking().AnyAsync(item => item.PropertyTypeId == model.RentalPropertyTypeId && !item.DeletedInd);
        if (!rentalPropertyTypeExists)
        {
            ModelState.AddModelError(nameof(model.RentalPropertyTypeId), "Rental property type is invalid.");
        }

        var hasExisting = await context.HomeRentalDetails.AnyAsync(item => item.ListingGUID == model.ListingGUID && !item.DeletedInd);
        if (hasExisting)
        {
            ModelState.AddModelError(nameof(model.ListingGUID), "This listing already has Home Rental details.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateListingSelectListAsync(currentUserId, model.ListingGUID);
            await PopulatePropertyTypeSelectListAsync(model.PropertyTypeId, model.RentalPropertyTypeId);
            return View("~/Views/HomeRentalDetail/Create.cshtml", model);
        }

        var entity = new HomeRentalDetail
        {
            HomeRentalDetailGUID = Guid.NewGuid(),
            ListingGUID = model.ListingGUID,
            PropertyTypeId = model.PropertyTypeId,
            NumberOfStoreys = model.NumberOfStoreys,
            PriceNegotiableInd = model.PriceNegotiableInd,
            RentalPropertyTypeId = model.RentalPropertyTypeId,
            RentalSquareFeet = model.RentalSquareFeet,
            Washrooms = model.Washrooms,
            SharedWashroomInd = model.SharedWashroomInd,
            Baths = model.Baths,
            YearBuilt = model.YearBuilt,
            ParkingSpots = model.ParkingSpots,
            RentalParkingSpots = model.RentalParkingSpots,
            ParkingIncludedInd = model.ParkingIncludedInd,
            PreferredRentalStartDate = model.PreferredRentalStartDate,
            PreferredRentalTerm = model.PreferredRentalTerm,
            RentalTermNegotiableInd = model.RentalTermNegotiableInd,
            FurnishedInd = model.FurnishedInd,
            WaterIncludedInd = model.WaterIncludedInd,
            HeatingIncludedInd = model.HeatingIncludedInd,
            HydroElectricityIncludedInd = model.HydroElectricityIncludedInd,
            InternetWiFiIncludedInd = model.InternetWiFiIncludedInd,
            AdditionalInfo = model.AdditionalInfo,
            CreatedBy = User.Identity?.Name,
            CreatedDate = CanHappy.Common.EasternTime.Now,
            DeletedInd = false
        };

        context.HomeRentalDetails.Add(entity);
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

        var entity = await context.HomeRentalDetails
            .Include(item => item.Listing)
            .ThenInclude(listing => listing!.Category)
            .Include(item => item.PropertyType)
            .Include(item => item.RentalPropertyType)
            .FirstOrDefaultAsync(item => item.HomeRentalDetailGUID == id && !item.DeletedInd);

        if (entity is null || entity.Listing is null)
        {
            return NotFound();
        }

        if (!string.Equals(entity.Listing.Category?.Name, HomeRentalCategoryName, StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        if (!CanManageListing(entity.Listing, currentUserId))
        {
            return Forbid();
        }

        await PopulatePropertyTypeSelectListAsync(entity.PropertyTypeId, entity.RentalPropertyTypeId);

        var model = ToEditViewModel(entity, entity.Listing);
        return View("~/Views/HomeRentalDetail/Edit.cshtml", model);
    }

    [HttpPost("Edit/{id:guid}")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, [Bind("HomeRentalDetailGUID,ListingGUID,PropertyTypeId,NumberOfStoreys,PriceNegotiableInd,RentalPropertyTypeId,RentalSquareFeet,Washrooms,SharedWashroomInd,Baths,YearBuilt,ParkingSpots,RentalParkingSpots,ParkingIncludedInd,PreferredRentalStartDate,PreferredRentalTerm,RentalTermNegotiableInd,FurnishedInd,WaterIncludedInd,HeatingIncludedInd,HydroElectricityIncludedInd,InternetWiFiIncludedInd,AdditionalInfo")] HomeRentalDetailEditViewModel model)
    {
        if (model.HomeRentalDetailGUID != id)
        {
            return NotFound();
        }

        if (!TryGetCurrentUserGuid(out var currentUserId) && !User.IsInRole("Admin") && !User.IsInRole("Clerk"))
        {
            return Forbid();
        }

        var entity = await context.HomeRentalDetails
            .Include(item => item.Listing)
            .ThenInclude(listing => listing!.Category)
            .Include(item => item.PropertyType)
            .Include(item => item.RentalPropertyType)
            .FirstOrDefaultAsync(item => item.HomeRentalDetailGUID == id && !item.DeletedInd);

        if (entity is null || entity.Listing is null)
        {
            return NotFound();
        }

        if (!string.Equals(entity.Listing.Category?.Name, HomeRentalCategoryName, StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        if (!CanManageListing(entity.Listing, currentUserId))
        {
            return Forbid();
        }

        var propertyTypeExists = await context.PropertyTypes.AsNoTracking().AnyAsync(item => item.PropertyTypeId == model.PropertyTypeId && !item.DeletedInd);
        if (!propertyTypeExists)
        {
            ModelState.AddModelError(nameof(model.PropertyTypeId), "Property type is invalid.");
        }

        var rentalPropertyTypeExists = await context.PropertyTypes.AsNoTracking().AnyAsync(item => item.PropertyTypeId == model.RentalPropertyTypeId && !item.DeletedInd);
        if (!rentalPropertyTypeExists)
        {
            ModelState.AddModelError(nameof(model.RentalPropertyTypeId), "Rental property type is invalid.");
        }

        if (!ModelState.IsValid)
        {
            await PopulatePropertyTypeSelectListAsync(model.PropertyTypeId, model.RentalPropertyTypeId);
            var invalidModel = ToEditViewModel(entity, entity.Listing);
            invalidModel.PropertyTypeId = model.PropertyTypeId;
            invalidModel.NumberOfStoreys = model.NumberOfStoreys;
            invalidModel.PriceNegotiableInd = model.PriceNegotiableInd;
            invalidModel.RentalPropertyTypeId = model.RentalPropertyTypeId;
            invalidModel.RentalSquareFeet = model.RentalSquareFeet;
            invalidModel.Washrooms = model.Washrooms;
            invalidModel.SharedWashroomInd = model.SharedWashroomInd;
            invalidModel.Baths = model.Baths;
            invalidModel.YearBuilt = model.YearBuilt;
            invalidModel.ParkingSpots = model.ParkingSpots;
            invalidModel.RentalParkingSpots = model.RentalParkingSpots;
            invalidModel.ParkingIncludedInd = model.ParkingIncludedInd;
            invalidModel.PreferredRentalStartDate = model.PreferredRentalStartDate;
            invalidModel.PreferredRentalTerm = model.PreferredRentalTerm;
            invalidModel.RentalTermNegotiableInd = model.RentalTermNegotiableInd;
            invalidModel.FurnishedInd = model.FurnishedInd;
            invalidModel.WaterIncludedInd = model.WaterIncludedInd;
            invalidModel.HeatingIncludedInd = model.HeatingIncludedInd;
            invalidModel.HydroElectricityIncludedInd = model.HydroElectricityIncludedInd;
            invalidModel.InternetWiFiIncludedInd = model.InternetWiFiIncludedInd;
            invalidModel.AdditionalInfo = model.AdditionalInfo;

            return View("~/Views/HomeRentalDetail/Edit.cshtml", invalidModel);
        }

        entity.PropertyTypeId = model.PropertyTypeId;
        entity.NumberOfStoreys = model.NumberOfStoreys;
        entity.PriceNegotiableInd = model.PriceNegotiableInd;
        entity.RentalPropertyTypeId = model.RentalPropertyTypeId;
        entity.RentalSquareFeet = model.RentalSquareFeet;
        entity.Washrooms = model.Washrooms;
        entity.SharedWashroomInd = model.SharedWashroomInd;
        entity.Baths = model.Baths;
        entity.YearBuilt = model.YearBuilt;
        entity.ParkingSpots = model.ParkingSpots;
        entity.RentalParkingSpots = model.RentalParkingSpots;
        entity.ParkingIncludedInd = model.ParkingIncludedInd;
        entity.PreferredRentalStartDate = model.PreferredRentalStartDate;
        entity.PreferredRentalTerm = model.PreferredRentalTerm;
        entity.RentalTermNegotiableInd = model.RentalTermNegotiableInd;
        entity.FurnishedInd = model.FurnishedInd;
        entity.WaterIncludedInd = model.WaterIncludedInd;
        entity.HeatingIncludedInd = model.HeatingIncludedInd;
        entity.HydroElectricityIncludedInd = model.HydroElectricityIncludedInd;
        entity.InternetWiFiIncludedInd = model.InternetWiFiIncludedInd;
        entity.AdditionalInfo = model.AdditionalInfo;
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

        var entity = await context.HomeRentalDetails
            .AsNoTracking()
            .Include(item => item.Listing)
            .ThenInclude(listing => listing!.Category)
            .Include(item => item.PropertyType)
            .Include(item => item.RentalPropertyType)
            .FirstOrDefaultAsync(item => item.HomeRentalDetailGUID == id && !item.DeletedInd);

        if (entity is null || entity.Listing is null)
        {
            return NotFound();
        }

        if (!string.Equals(entity.Listing.Category?.Name, HomeRentalCategoryName, StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        if (!CanManageListing(entity.Listing, currentUserId))
        {
            return Forbid();
        }

        var model = ToEditViewModel(entity, entity.Listing);
        return View("~/Views/HomeRentalDetail/Delete.cshtml", model);
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

        var entity = await context.HomeRentalDetails
            .Include(item => item.Listing)
            .ThenInclude(listing => listing!.Category)
            .FirstOrDefaultAsync(item => item.HomeRentalDetailGUID == id && !item.DeletedInd);

        if (entity is null || entity.Listing is null)
        {
            return NotFound();
        }

        if (!string.Equals(entity.Listing.Category?.Name, HomeRentalCategoryName, StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        if (!CanManageListing(entity.Listing, currentUserId))
        {
            return Forbid();
        }

        context.HomeRentalDetails.Remove(entity);
        await context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
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

        var listing = await context.Listings
            .Include(item => item.Category)
            .FirstOrDefaultAsync(item => item.ListingGUID == listingGuid && !item.DeletedInd);
        if (listing is null)
        {
            return NotFound();
        }

        if (!string.Equals(listing.Category?.Name, HomeRentalCategoryName, StringComparison.OrdinalIgnoreCase))
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

        ListingImage? listingImage = null;
        if (listingImageGuid.HasValue)
        {
            listingImage = await context.ListingImages.FirstOrDefaultAsync(item => item.ListingImageGUID == listingImageGuid.Value && item.ListingGUID == listingGuid && !item.DeletedInd);
        }

        if (listingImage is null)
        {
            var imageCount = await context.ListingImages.CountAsync(item => item.ListingGUID == listingGuid && !item.DeletedInd);
            if (imageCount >= 30)
            {
                TempData["MessageError"] = "You can upload up to 30 photos for a listing.";
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

        var listing = await context.Listings
            .Include(item => item.Category)
            .FirstOrDefaultAsync(item => item.ListingGUID == listingGuid && !item.DeletedInd);
        if (listing is null)
        {
            return NotFound();
        }

        if (!string.Equals(listing.Category?.Name, HomeRentalCategoryName, StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
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

    private HomeRentalDetailEditViewModel ToEditViewModel(HomeRentalDetail detail, Listing listing)
    {
        return new HomeRentalDetailEditViewModel
        {
            HomeRentalDetailGUID = detail.HomeRentalDetailGUID,
            ListingGUID = listing.ListingGUID,
            Subject = listing.Subject,
            Description = listing.Description,
            CategoryId = listing.CategoryId,
            SubcategoryId = listing.SubcategoryId,
            ProvinceId = listing.ProvinceId,
            CityId = listing.CityId,
            Price = listing.Price,
            Brand = listing.Brand,
            Condition = listing.Condition,
            ManufactureYear = listing.ManufactureYear,
            ListingModel = listing.Model,
            Quantity = listing.Quantity,
            PropertyTypeId = detail.PropertyTypeId,
            PropertyTypeName = detail.PropertyType?.Name,
            NumberOfStoreys = detail.NumberOfStoreys,
            PriceNegotiableInd = detail.PriceNegotiableInd,
            RentalPropertyTypeId = detail.RentalPropertyTypeId,
            RentalPropertyTypeName = detail.RentalPropertyType?.Name,
            RentalSquareFeet = detail.RentalSquareFeet,
            Washrooms = detail.Washrooms,
            SharedWashroomInd = detail.SharedWashroomInd,
            Baths = detail.Baths,
            YearBuilt = detail.YearBuilt,
            ParkingSpots = detail.ParkingSpots,
            RentalParkingSpots = detail.RentalParkingSpots,
            ParkingIncludedInd = detail.ParkingIncludedInd,
            PreferredRentalStartDate = detail.PreferredRentalStartDate,
            PreferredRentalTerm = detail.PreferredRentalTerm,
            RentalTermNegotiableInd = detail.RentalTermNegotiableInd,
            FurnishedInd = detail.FurnishedInd,
            WaterIncludedInd = detail.WaterIncludedInd,
            HeatingIncludedInd = detail.HeatingIncludedInd,
            HydroElectricityIncludedInd = detail.HydroElectricityIncludedInd,
            InternetWiFiIncludedInd = detail.InternetWiFiIncludedInd,
            AdditionalInfo = detail.AdditionalInfo
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

    private async Task PopulateListingSelectListAsync(Guid currentUserId, Guid? selectedListingId = null)
    {
        var isPrivileged = User.IsInRole("Admin") || User.IsInRole("Clerk");

        var query = context.Listings
            .AsNoTracking()
            .Include(item => item.Category)
            .Where(listing => !listing.DeletedInd)
            .Where(listing => listing.Category != null && listing.Category.Name == HomeRentalCategoryName)
            .Where(listing => !context.HomeRentalDetails.Any(detail => detail.ListingGUID == listing.ListingGUID && !detail.DeletedInd));

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

    private async Task PopulatePropertyTypeSelectListAsync(int? selectedPropertyTypeId = null, int? selectedRentalPropertyTypeId = null)
    {
        var propertyTypes = await context.PropertyTypes
            .AsNoTracking()
            .Where(item => !item.DeletedInd)
            .OrderBy(item => item.Name)
            .ToListAsync();

        ViewData["PropertyTypeId"] = new SelectList(propertyTypes, nameof(PropertyType.PropertyTypeId), nameof(PropertyType.Name), selectedPropertyTypeId);
        ViewData["RentalPropertyTypeId"] = new SelectList(propertyTypes, nameof(PropertyType.PropertyTypeId), nameof(PropertyType.Name), selectedRentalPropertyTypeId);
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
        var relativePath = $"/ListingImages/{fileName}";
        var folderPath = Path.Combine(environment.WebRootPath, "ListingImages");

        Directory.CreateDirectory(folderPath);
        var filePath = Path.Combine(folderPath, fileName);
        await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);

        return relativePath;
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
