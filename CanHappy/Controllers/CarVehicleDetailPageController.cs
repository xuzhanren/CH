using CanHappy.Data;
using CanHappy.Common;
using CanHappy.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CanHappy.Controllers;

[Route("CarVehicleDetail")]
public class CarVehicleDetailPageController(ApplicationDbContext context, IWebHostEnvironment environment, IConfiguration configuration) : Controller
{
    private const string CarVehicleCategoryName = "Car & Vehicle";

    [HttpGet("")]
    public async Task<IActionResult> Index(Guid? listingGuid)
    {
        ViewData["CarVehicleSliderWaitSeconds"] = Math.Max(0,
            configuration.GetValue<int?>("CarVehicleDetailSliderWaitSeconds")
            ?? configuration.GetValue<int?>("CarVehicleSliderWaitSeconds")
            ?? 2);
        ViewData["CarVehicleAdSlidingInterval"] = Math.Max(1,
            configuration.GetValue<int?>("CarVehicleDetailAdSlidingInterval")
            ?? configuration.GetValue<int?>("CarVehicleAdSlidingInterval")
            ?? 5);
        ViewData["CarVehicleAdSliderFadingMode"] = configuration.GetValue<string>("CarVehicleDetailAdSliderFadingMode")
            ?? configuration.GetValue<string>("CarVehicleAdSliderFadingMode")
            ?? "homeAdFade";
        ViewData["CarVehicleAdPixelResolveTransitionPeriodMiliSeconds"] = Math.Max(100,
            configuration.GetValue<int?>("carVehicleDetailAdPixelResolveTransitionPeriodMiliSeconds")
            ?? configuration.GetValue<int?>("carVehicleAdPixelResolveTransitionPeriodMiliSeconds")
            ?? 1000);
        ViewData["CarVehicleAdFadeTransitionMilliSeconds"] = Math.Max(100,
            configuration.GetValue<int?>("carVehicleDetailAdFadeTransitionMilliSeconds")
            ?? configuration.GetValue<int?>("carVehicleAdFadeTransitionMilliSeconds")
            ?? 450);
        ViewData["CarVehicleSlideTransitionMilliSeconds"] = Math.Max(100,
            configuration.GetValue<int?>("carVehicleDetailSlideTransitionMilliSeconds")
            ?? configuration.GetValue<int?>("carVehicleSlideTransitionMilliSeconds")
            ?? 650);
        ViewData["CarVehicleAdSlideDistancePercent"] = Math.Clamp(
            configuration.GetValue<int?>("carVehicleDetailAdSlideDistancePercent")
            ?? configuration.GetValue<int?>("carVehicleAdSlideDistancePercent")
            ?? 8, 1, 30);
        ViewData["CarVehicleAdTransitionEasing"] = configuration.GetValue<string>("carVehicleDetailAdTransitionEasing")
            ?? configuration.GetValue<string>("carVehicleAdTransitionEasing")
            ?? "ease-in-out";

        var hasUserGuid = TryGetCurrentUserGuid(out var currentUserId);
        var canManageByRole = User.IsInRole("Admin") || User.IsInRole("Clerk");

        var model = new CarVehicleDetailIndexPageViewModel
        {
            ListingGUIDFilter = listingGuid
        };

        var query = context.CarVehicleDetails
            .AsNoTracking()
            .Include(item => item.Listing)
            .ThenInclude(listing => listing!.Category)
            .Where(item => !item.DeletedInd
                && item.Listing != null
                && !item.Listing.DeletedInd
                && item.Listing.Category != null
                && item.Listing.Category.Name == CarVehicleCategoryName)
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

            if (!string.Equals(listing.Category?.Name, CarVehicleCategoryName, StringComparison.OrdinalIgnoreCase))
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

            var listingImages = await context.ListingImages
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

            foreach (var image in listingImages)
            {
                image.ThumbnailURL = ResolveListingImageUrl(image.ThumbnailURL);
                image.ImageURL = ResolveListingImageUrl(image.ImageURL);
            }

            model.ListingImages = listingImages;
        }

        model.Items = await query
            .OrderByDescending(item => item.CreatedDate)
            .Select(item => new CarVehicleDetailIndexItemViewModel
            {
                CarVehicleDetailGUID = item.CarVehicleDetailGUID,
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
                Kilometers = item.Kilometers,
                Doors = item.Doors,
                Seats = item.Seats,
                BodyStyle = item.BodyStyle,
                Engine = item.Engine,
                ExteriorColor = item.ExteriorColor,
                InteriorColor = item.InteriorColor,
                Transmission = item.Transmission,
                Drivetrain = item.Drivetrain,
                FuelType = item.FuelType,
                SellerType = item.SellerType,
                LeatherSeatsInd = item.LeatherSeatsInd,
                BackupCameraInd = item.BackupCameraInd,
                AlloyWheelsInd = item.AlloyWheelsInd,
                BluetoothInd = item.BluetoothInd,
                HeatedSeatsInd = item.HeatedSeatsInd,
                CarPlayInd = item.CarPlayInd,
                AndroidAutoInd = item.AndroidAutoInd,
                NavigationMapInd = item.NavigationMapInd,
                RemoteStartInd = item.RemoteStartInd,
                SunroofInd = item.SunroofInd,
                MoonroofInd = item.MoonroofInd,
                BlindSpotMonitoringInd = item.BlindSpotMonitoringInd,
                LaneTrackingInd = item.LaneTrackingInd,
                AdaptiveCruiseInd = item.AdaptiveCruiseInd,
                AssistedParkingCameraInd = item.AssistedParkingCameraInd,
                CanManage = canManageByRole || (hasUserGuid && item.Listing.UserId != Guid.Empty && item.Listing.UserId == currentUserId)
            })
            .ToListAsync();

        if (listingGuid.HasValue && model.ListingCard is not null)
        {
            var focusDetail = model.Items.FirstOrDefault(item => item.ListingGUID == listingGuid.Value);
            if (focusDetail is not null)
            {
                model.HasExistingFocusDetail = true;
                model.FocusDetail = new CarVehicleDetailEditViewModel
                {
                    CarVehicleDetailGUID = focusDetail.CarVehicleDetailGUID,
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
                    Kilometers = focusDetail.Kilometers,
                    Doors = focusDetail.Doors,
                    Seats = focusDetail.Seats,
                    BodyStyle = focusDetail.BodyStyle,
                    Engine = focusDetail.Engine,
                    ExteriorColor = focusDetail.ExteriorColor,
                    InteriorColor = focusDetail.InteriorColor,
                    Transmission = focusDetail.Transmission,
                    Drivetrain = focusDetail.Drivetrain,
                    FuelType = focusDetail.FuelType,
                    SellerType = focusDetail.SellerType,
                    LeatherSeatsInd = focusDetail.LeatherSeatsInd,
                    BackupCameraInd = focusDetail.BackupCameraInd,
                    AlloyWheelsInd = focusDetail.AlloyWheelsInd,
                    BluetoothInd = focusDetail.BluetoothInd,
                    HeatedSeatsInd = focusDetail.HeatedSeatsInd,
                    CarPlayInd = focusDetail.CarPlayInd,
                    AndroidAutoInd = focusDetail.AndroidAutoInd,
                    NavigationMapInd = focusDetail.NavigationMapInd,
                    RemoteStartInd = focusDetail.RemoteStartInd,
                    SunroofInd = focusDetail.SunroofInd,
                    MoonroofInd = focusDetail.MoonroofInd,
                    BlindSpotMonitoringInd = focusDetail.BlindSpotMonitoringInd,
                    LaneTrackingInd = focusDetail.LaneTrackingInd,
                    AdaptiveCruiseInd = focusDetail.AdaptiveCruiseInd,
                    AssistedParkingCameraInd = focusDetail.AssistedParkingCameraInd
                };
            }
            else
            {
                model.FocusDetail = new CarVehicleDetailEditViewModel
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
                    Quantity = model.ListingCard.Quantity
                };
            }
        }

        return View("~/Views/CarVehicleDetail/Index.cshtml", model);
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
                && item.PublishDate.Date <= today
                && (!item.ExpiryDate.HasValue || item.ExpiryDate.Value.Date >= today)
                && item.AdSizeOption != null
                && (item.AdSizeOption.Name == "1200x300DetailPageSlider2Ads" || item.AdSizeOption.Name.Contains("1200x300"))
                && !string.IsNullOrWhiteSpace(item.ImageURL)
                && !string.IsNullOrWhiteSpace(item.TargetURL)
                && (!item.ProvinceId.HasValue || item.ProvinceId.Value == listing.ProvinceId)
                && (!item.CityId.HasValue || item.CityId.Value == listing.CityId)
                && (!item.CategoryId.HasValue || item.CategoryId.Value == listing.CategoryId)
                && (!item.SubcategoryId.HasValue || item.SubcategoryId.Value == listing.SubcategoryId))
            .ToListAsync();

        if (candidateAds.Count == 0)
        {
            candidateAds = await context.Ads
                .AsNoTracking()
                .Include(item => item.AdSizeOption)
                .Where(item => !item.DeletedInd
                    && item.ActiveInd
                    && item.AdStatusId == 3
                    && item.PublishDate.Date <= today
                    && (!item.ExpiryDate.HasValue || item.ExpiryDate.Value.Date >= today)
                    && !string.IsNullOrWhiteSpace(item.ImageURL)
                    && !string.IsNullOrWhiteSpace(item.TargetURL))
                .ToListAsync();
        }

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
                imageURL = ResolveAdImageUrl(item.Ad.ImageURL),
                targetURL = item.Ad.TargetURL,
                score = item.Score
            })
            .ToList();

        return Json(scoredAds);
    }

    [HttpGet("SimilarItems/{listingGuid:guid}")]
    public async Task<IActionResult> SimilarItems(Guid listingGuid)
    {
        var listing = await context.Listings
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.ListingGUID == listingGuid && !item.DeletedInd);

        if (listing is null)
        {
            return Json(Array.Empty<object>());
        }

        var similarCandidates = await context.Listings
            .AsNoTracking()
            .Where(item => !item.DeletedInd && item.ListingGUID != listing.ListingGUID)
            .Where(item => listing.ProvinceId <= 0 || item.ProvinceId == listing.ProvinceId)
            .Where(item => listing.CityId <= 0 || item.CityId == listing.CityId)
            .Where(item => listing.CategoryId <= 0 || item.CategoryId == listing.CategoryId)
            .Where(item => !listing.SubcategoryId.HasValue || item.SubcategoryId == listing.SubcategoryId)
            .ToListAsync();

        if (similarCandidates.Count == 0)
        {
            return Json(Array.Empty<object>());
        }

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

        if (selectedSimilarListings.Count == 0)
        {
            return Json(Array.Empty<object>());
        }

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

        var result = selectedSimilarListings
            .Select(item => new SimilarListingItemViewModel
            {
                ListingGUID = item.ListingGUID,
                Subject = item.Subject,
                ThumbnailURL = !string.IsNullOrWhiteSpace(item.ThumbnailURL)
                    ? ResolveListingImageUrl(item.ThumbnailURL)
                    : (thumbnailByListing.TryGetValue(item.ListingGUID, out var thumbnailUrl) ? thumbnailUrl : null)
            })
            .ToList();

        return Json(result);
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

            if (!string.Equals(listing.Category?.Name, CarVehicleCategoryName, StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            if (!CanManageListing(listing, currentUserId))
            {
                return Forbid();
            }

            var hasExisting = await context.CarVehicleDetails
                .AsNoTracking()
                .AnyAsync(item => item.ListingGUID == listingGuid.Value && !item.DeletedInd);

            if (hasExisting)
            {
                TempData["MessageError"] = "The listing details already exist! Edit them if changes needed.";
                return RedirectToAction(nameof(Index), new { listingGuid = listingGuid.Value });
            }
        }

        await PopulateListingSelectListAsync(currentUserId, listingGuid);
        return View("~/Views/CarVehicleDetail/Create.cshtml", new CarVehicleDetailEditViewModel
        {
            ListingGUID = listingGuid ?? Guid.Empty
        });
    }

    [HttpPost("Create")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ListingGUID,Kilometers,Doors,Seats,BodyStyle,Engine,ExteriorColor,InteriorColor,Transmission,Drivetrain,FuelType,SellerType,LeatherSeatsInd,BackupCameraInd,AlloyWheelsInd,BluetoothInd,HeatedSeatsInd,CarPlayInd,AndroidAutoInd,NavigationMapInd,RemoteStartInd,SunroofInd,MoonroofInd,BlindSpotMonitoringInd,LaneTrackingInd,AdaptiveCruiseInd,AssistedParkingCameraInd")] CarVehicleDetailEditViewModel model)
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
            if (!string.Equals(listing.Category?.Name, CarVehicleCategoryName, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(model.ListingGUID), "Listing category must be Car & Vehicle.");
            }
            else if (!CanManageListing(listing, currentUserId))
            {
                return Forbid();
            }
        }

        var hasExisting = await context.CarVehicleDetails.AnyAsync(item => item.ListingGUID == model.ListingGUID && !item.DeletedInd);
        if (hasExisting)
        {
            ModelState.AddModelError(nameof(model.ListingGUID), "This listing already has Car/Vehicle details.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateListingSelectListAsync(currentUserId, model.ListingGUID);
            return View("~/Views/CarVehicleDetail/Create.cshtml", model);
        }

        var entity = new CarVehicleDetail
        {
            CarVehicleDetailGUID = Guid.NewGuid(),
            ListingGUID = model.ListingGUID,
            Kilometers = model.Kilometers,
            Doors = model.Doors,
            Seats = model.Seats,
            BodyStyle = model.BodyStyle,
            Engine = model.Engine,
            ExteriorColor = model.ExteriorColor,
            InteriorColor = model.InteriorColor,
            Transmission = model.Transmission,
            Drivetrain = model.Drivetrain,
            FuelType = model.FuelType,
            SellerType = model.SellerType,
            LeatherSeatsInd = model.LeatherSeatsInd,
            BackupCameraInd = model.BackupCameraInd,
            AlloyWheelsInd = model.AlloyWheelsInd,
            BluetoothInd = model.BluetoothInd,
            HeatedSeatsInd = model.HeatedSeatsInd,
            CarPlayInd = model.CarPlayInd,
            AndroidAutoInd = model.AndroidAutoInd,
            NavigationMapInd = model.NavigationMapInd,
            RemoteStartInd = model.RemoteStartInd,
            SunroofInd = model.SunroofInd,
            MoonroofInd = model.MoonroofInd,
            BlindSpotMonitoringInd = model.BlindSpotMonitoringInd,
            LaneTrackingInd = model.LaneTrackingInd,
            AdaptiveCruiseInd = model.AdaptiveCruiseInd,
            AssistedParkingCameraInd = model.AssistedParkingCameraInd,
            CreatedBy = User.Identity?.Name,
            CreatedDate = CanHappy.Common.EasternTime.Now,
            DeletedInd = false
        };

        context.CarVehicleDetails.Add(entity);
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

        var entity = await context.CarVehicleDetails
            .Include(item => item.Listing)
            .ThenInclude(listing => listing!.Category)
            .FirstOrDefaultAsync(item => item.CarVehicleDetailGUID == id && !item.DeletedInd);

        if (entity is null || entity.Listing is null)
        {
            return NotFound();
        }

        if (!string.Equals(entity.Listing.Category?.Name, CarVehicleCategoryName, StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        if (!CanManageListing(entity.Listing, currentUserId))
        {
            return Forbid();
        }

        var model = ToEditViewModel(entity, entity.Listing);
        return View("~/Views/CarVehicleDetail/Edit.cshtml", model);
    }

    [HttpPost("Edit/{id:guid}")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, [Bind("CarVehicleDetailGUID,ListingGUID,Kilometers,Doors,Seats,BodyStyle,Engine,ExteriorColor,InteriorColor,Transmission,Drivetrain,FuelType,SellerType,LeatherSeatsInd,BackupCameraInd,AlloyWheelsInd,BluetoothInd,HeatedSeatsInd,CarPlayInd,AndroidAutoInd,NavigationMapInd,RemoteStartInd,SunroofInd,MoonroofInd,BlindSpotMonitoringInd,LaneTrackingInd,AdaptiveCruiseInd,AssistedParkingCameraInd")] CarVehicleDetailEditViewModel model)
    {
        if (model.CarVehicleDetailGUID != id)
        {
            return NotFound();
        }

        if (!TryGetCurrentUserGuid(out var currentUserId) && !User.IsInRole("Admin") && !User.IsInRole("Clerk"))
        {
            return Forbid();
        }

        var entity = await context.CarVehicleDetails
            .Include(item => item.Listing)
            .ThenInclude(listing => listing!.Category)
            .FirstOrDefaultAsync(item => item.CarVehicleDetailGUID == id && !item.DeletedInd);

        if (entity is null || entity.Listing is null)
        {
            return NotFound();
        }

        if (!string.Equals(entity.Listing.Category?.Name, CarVehicleCategoryName, StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        if (!CanManageListing(entity.Listing, currentUserId))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            var invalidModel = ToEditViewModel(entity, entity.Listing);
            invalidModel.Kilometers = model.Kilometers;
            invalidModel.Doors = model.Doors;
            invalidModel.Seats = model.Seats;
            invalidModel.BodyStyle = model.BodyStyle;
            invalidModel.Engine = model.Engine;
            invalidModel.ExteriorColor = model.ExteriorColor;
            invalidModel.InteriorColor = model.InteriorColor;
            invalidModel.Transmission = model.Transmission;
            invalidModel.Drivetrain = model.Drivetrain;
            invalidModel.FuelType = model.FuelType;
            invalidModel.SellerType = model.SellerType;
            invalidModel.LeatherSeatsInd = model.LeatherSeatsInd;
            invalidModel.BackupCameraInd = model.BackupCameraInd;
            invalidModel.AlloyWheelsInd = model.AlloyWheelsInd;
            invalidModel.BluetoothInd = model.BluetoothInd;
            invalidModel.HeatedSeatsInd = model.HeatedSeatsInd;
            invalidModel.CarPlayInd = model.CarPlayInd;
            invalidModel.AndroidAutoInd = model.AndroidAutoInd;
            invalidModel.NavigationMapInd = model.NavigationMapInd;
            invalidModel.RemoteStartInd = model.RemoteStartInd;
            invalidModel.SunroofInd = model.SunroofInd;
            invalidModel.MoonroofInd = model.MoonroofInd;
            invalidModel.BlindSpotMonitoringInd = model.BlindSpotMonitoringInd;
            invalidModel.LaneTrackingInd = model.LaneTrackingInd;
            invalidModel.AdaptiveCruiseInd = model.AdaptiveCruiseInd;
            invalidModel.AssistedParkingCameraInd = model.AssistedParkingCameraInd;

            return View("~/Views/CarVehicleDetail/Edit.cshtml", invalidModel);
        }

        entity.Kilometers = model.Kilometers;
        entity.Doors = model.Doors;
        entity.Seats = model.Seats;
        entity.BodyStyle = model.BodyStyle;
        entity.Engine = model.Engine;
        entity.ExteriorColor = model.ExteriorColor;
        entity.InteriorColor = model.InteriorColor;
        entity.Transmission = model.Transmission;
        entity.Drivetrain = model.Drivetrain;
        entity.FuelType = model.FuelType;
        entity.SellerType = model.SellerType;
        entity.LeatherSeatsInd = model.LeatherSeatsInd;
        entity.BackupCameraInd = model.BackupCameraInd;
        entity.AlloyWheelsInd = model.AlloyWheelsInd;
        entity.BluetoothInd = model.BluetoothInd;
        entity.HeatedSeatsInd = model.HeatedSeatsInd;
        entity.CarPlayInd = model.CarPlayInd;
        entity.AndroidAutoInd = model.AndroidAutoInd;
        entity.NavigationMapInd = model.NavigationMapInd;
        entity.RemoteStartInd = model.RemoteStartInd;
        entity.SunroofInd = model.SunroofInd;
        entity.MoonroofInd = model.MoonroofInd;
        entity.BlindSpotMonitoringInd = model.BlindSpotMonitoringInd;
        entity.LaneTrackingInd = model.LaneTrackingInd;
        entity.AdaptiveCruiseInd = model.AdaptiveCruiseInd;
        entity.AssistedParkingCameraInd = model.AssistedParkingCameraInd;
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

        var entity = await context.CarVehicleDetails
            .AsNoTracking()
            .Include(item => item.Listing)
            .ThenInclude(listing => listing!.Category)
            .FirstOrDefaultAsync(item => item.CarVehicleDetailGUID == id && !item.DeletedInd);

        if (entity is null || entity.Listing is null)
        {
            return NotFound();
        }

        if (!string.Equals(entity.Listing.Category?.Name, CarVehicleCategoryName, StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        if (!CanManageListing(entity.Listing, currentUserId))
        {
            return Forbid();
        }

        var model = ToEditViewModel(entity, entity.Listing);
        return View("~/Views/CarVehicleDetail/Delete.cshtml", model);
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

        var entity = await context.CarVehicleDetails
            .Include(item => item.Listing)
            .ThenInclude(listing => listing!.Category)
            .FirstOrDefaultAsync(item => item.CarVehicleDetailGUID == id && !item.DeletedInd);

        if (entity is null || entity.Listing is null)
        {
            return NotFound();
        }

        if (!string.Equals(entity.Listing.Category?.Name, CarVehicleCategoryName, StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        if (!CanManageListing(entity.Listing, currentUserId))
        {
            return Forbid();
        }

        context.CarVehicleDetails.Remove(entity);
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

        if (!string.Equals(listing.Category?.Name, CarVehicleCategoryName, StringComparison.OrdinalIgnoreCase))
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

        if (!string.Equals(listing.Category?.Name, CarVehicleCategoryName, StringComparison.OrdinalIgnoreCase))
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

    private CarVehicleDetailEditViewModel ToEditViewModel(CarVehicleDetail detail, Listing listing)
    {
        return new CarVehicleDetailEditViewModel
        {
            CarVehicleDetailGUID = detail.CarVehicleDetailGUID,
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
            Kilometers = detail.Kilometers,
            Doors = detail.Doors,
            Seats = detail.Seats,
            BodyStyle = detail.BodyStyle,
            Engine = detail.Engine,
            ExteriorColor = detail.ExteriorColor,
            InteriorColor = detail.InteriorColor,
            Transmission = detail.Transmission,
            Drivetrain = detail.Drivetrain,
            FuelType = detail.FuelType,
            SellerType = detail.SellerType,
            LeatherSeatsInd = detail.LeatherSeatsInd,
            BackupCameraInd = detail.BackupCameraInd,
            AlloyWheelsInd = detail.AlloyWheelsInd,
            BluetoothInd = detail.BluetoothInd,
            HeatedSeatsInd = detail.HeatedSeatsInd,
            CarPlayInd = detail.CarPlayInd,
            AndroidAutoInd = detail.AndroidAutoInd,
            NavigationMapInd = detail.NavigationMapInd,
            RemoteStartInd = detail.RemoteStartInd,
            SunroofInd = detail.SunroofInd,
            MoonroofInd = detail.MoonroofInd,
            BlindSpotMonitoringInd = detail.BlindSpotMonitoringInd,
            LaneTrackingInd = detail.LaneTrackingInd,
            AdaptiveCruiseInd = detail.AdaptiveCruiseInd,
            AssistedParkingCameraInd = detail.AssistedParkingCameraInd
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
            .Where(listing => listing.Category != null && listing.Category.Name == CarVehicleCategoryName)
            .Where(listing => !context.CarVehicleDetails.Any(detail => detail.ListingGUID == listing.ListingGUID && !detail.DeletedInd));

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

    private string AdImagesFolder => MediaPathHelper.ResolveWebFolder(configuration, "AdImagesFolder", "/AdImages");

    private string? ResolveListingImageUrl(string? url)
    {
        return MediaPathHelper.BuildMediaUrl(url, ListImageFolder);
    }

    private string? ResolveAdImageUrl(string? url)
    {
        return MediaPathHelper.BuildMediaUrl(url, AdImagesFolder);
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
