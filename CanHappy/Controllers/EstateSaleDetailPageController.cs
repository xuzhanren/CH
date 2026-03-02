using CanHappy.Data;
using CanHappy.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CanHappy.Controllers;

[Route("EstateSaleDetail")]
public class EstateSaleDetailPageController(ApplicationDbContext context, IWebHostEnvironment environment) : Controller
{
    private const string EstateSaleCategoryName = "Estate Sale";
    private const long MaxVideoBytes = 50L * 1024L * 1024L;

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(Guid? listingGuid)
    {
        var hasUserGuid = TryGetCurrentUserGuid(out var currentUserId);
        var canManageByRole = User.IsInRole("Admin") || User.IsInRole("Clerk");

        var model = new EstateSaleDetailIndexPageViewModel
        {
            ListingGUIDFilter = listingGuid
        };

        if (listingGuid.HasValue)
        {
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

            if (!string.Equals(listing.Category?.Name, EstateSaleCategoryName, StringComparison.OrdinalIgnoreCase))
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

            model.ListingVideos = await context.ListingVideos
                .AsNoTracking()
                .Where(video => video.ListingGUID == listing.ListingGUID && !video.DeletedInd)
                .OrderBy(video => video.SortOrder)
                .ThenBy(video => video.CreatedDate)
                .Select(video => new ListingVideoCardViewModel
                {
                    ListingVideoGUID = video.ListingVideoGUID,
                    Name = video.Name,
                    SortOrder = video.SortOrder,
                    VideoSize = video.VideoSize,
                    ThumbnailURL = video.ThumbnailURL,
                    VideoURL = video.VideoURL
                })
                .ToListAsync();

            var detail = await context.EstateSaleDetails
                .AsNoTracking()
                .Include(item => item.EstateType)
                .FirstOrDefaultAsync(item => item.ListingGUID == listing.ListingGUID && !item.DeletedInd);

            if (detail is not null)
            {
                model.HasExistingFocusDetail = true;
                model.FocusDetail = ToEditViewModel(detail, listing);

                model.Rooms = await context.EstateRooms
                    .AsNoTracking()
                    .Where(room => room.EstateSaleDetailGUID == detail.EstateSaleDetailGUID && !room.DeletedInd)
                    .OrderBy(room => room.SortOrder)
                    .ThenBy(room => room.OnFloorNumber)
                    .ThenBy(room => room.RoomName)
                    .Select(room => new EstateRoomEditViewModel
                    {
                        EstateRoomGUID = room.EstateRoomGUID,
                        EstateSaleDetailGUID = room.EstateSaleDetailGUID,
                        RoomName = room.RoomName,
                        RoomSizeFtxFt = room.RoomSizeFtxFt,
                        SortOrder = room.SortOrder,
                        OnFloorNumber = room.OnFloorNumber,
                        ThumbnailURL = room.ThumbnailURL,
                        ImageURL = room.ImageURL
                    })
                    .ToListAsync();
            }
            else
            {
                model.FocusDetail = new EstateSaleDetailEditViewModel
                {
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
                    Quantity = listing.Quantity
                };
            }
        }

        return View("~/Views/EstateSaleDetail/Index.cshtml", model);
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

            if (!string.Equals(listing.Category?.Name, EstateSaleCategoryName, StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            if (!CanManageListing(listing, currentUserId))
            {
                return Forbid();
            }

            var hasExisting = await context.EstateSaleDetails
                .AsNoTracking()
                .AnyAsync(item => item.ListingGUID == listingGuid.Value && !item.DeletedInd);

            if (hasExisting)
            {
                TempData["MessageError"] = "The listing details already exist! Edit them if changes needed.";
                return RedirectToAction(nameof(Index), new { listingGuid = listingGuid.Value });
            }
        }

        await PopulateListingSelectListAsync(currentUserId, listingGuid);
        await PopulateEstateTypeSelectListAsync();

        return View("~/Views/EstateSaleDetail/Create.cshtml", new EstateSaleDetailEditViewModel
        {
            ListingGUID = listingGuid ?? Guid.Empty
        });
    }

    [HttpPost("Create")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ListingGUID,AnnualManagementFee,EstitateTypeId,YearBuilt,SquareFeet,LandSizeSqFt,LandDimensionWxD,NumberOfStoreys,PriceNegotiableInd,AnuualPropertyTax,Bedrooms,Washrooms,Baths,ParkingSpots,ParkingType,FoundationType,HydroType,WaterType,SewerType,ExternalStructures,CoolingType,HeatingType,WaterFrontInd,SoldByOwnerInd,AppliancesIncluded,HasBasementInd,BasementFinishedInd,RentalEquipment,CommunityName,CloseToSchoolInd,CloseToDaycareInd,CloseToBusInd,CloseToShoppingCenterInd,FurnishedInd,HasFireplaceInd,AdditionalInfo")] EstateSaleDetailEditViewModel model)
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
            if (!string.Equals(listing.Category?.Name, EstateSaleCategoryName, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(model.ListingGUID), "Listing category must be Estate Sale.");
            }
            else if (!CanManageListing(listing, currentUserId))
            {
                return Forbid();
            }
        }

        var estateTypeExists = await context.EstateTypes.AsNoTracking().AnyAsync(item => item.EstitateTypeId == model.EstitateTypeId && !item.DeletedInd);
        if (!estateTypeExists)
        {
            ModelState.AddModelError(nameof(model.EstitateTypeId), "Estate type is invalid.");
        }

        var hasExisting = await context.EstateSaleDetails.AnyAsync(item => item.ListingGUID == model.ListingGUID && !item.DeletedInd);
        if (hasExisting)
        {
            ModelState.AddModelError(nameof(model.ListingGUID), "This listing already has Estate Sale details.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateListingSelectListAsync(currentUserId, model.ListingGUID);
            await PopulateEstateTypeSelectListAsync(model.EstitateTypeId);
            return View("~/Views/EstateSaleDetail/Create.cshtml", model);
        }

        var entity = new EstateSaleDetail
        {
            EstateSaleDetailGUID = Guid.NewGuid(),
            ListingGUID = model.ListingGUID,
            AnnualManagementFee = model.AnnualManagementFee,
            EstitateTypeId = model.EstitateTypeId,
            YearBuilt = model.YearBuilt,
            SquareFeet = model.SquareFeet,
            LandSizeSqFt = model.LandSizeSqFt,
            LandDimensionWxD = model.LandDimensionWxD,
            NumberOfStoreys = model.NumberOfStoreys,
            PriceNegotiableInd = model.PriceNegotiableInd,
            AnuualPropertyTax = model.AnuualPropertyTax,
            Bedrooms = model.Bedrooms,
            Washrooms = model.Washrooms,
            Baths = model.Baths,
            ParkingSpots = model.ParkingSpots,
            ParkingType = model.ParkingType,
            FoundationType = model.FoundationType,
            HydroType = model.HydroType,
            WaterType = model.WaterType,
            SewerType = model.SewerType,
            ExternalStructures = model.ExternalStructures,
            CoolingType = model.CoolingType,
            HeatingType = model.HeatingType,
            WaterFrontInd = model.WaterFrontInd,
            SoldByOwnerInd = model.SoldByOwnerInd,
            AppliancesIncluded = model.AppliancesIncluded,
            HasBasementInd = model.HasBasementInd,
            BasementFinishedInd = model.BasementFinishedInd,
            RentalEquipment = model.RentalEquipment,
            CommunityName = model.CommunityName,
            CloseToSchoolInd = model.CloseToSchoolInd,
            CloseToDaycareInd = model.CloseToDaycareInd,
            CloseToBusInd = model.CloseToBusInd,
            CloseToShoppingCenterInd = model.CloseToShoppingCenterInd,
            FurnishedInd = model.FurnishedInd,
            HasFireplaceInd = model.HasFireplaceInd,
            AdditionalInfo = model.AdditionalInfo,
            CreatedBy = User.Identity?.Name,
            CreatedDate = CanHappy.Common.EasternTime.Now,
            DeletedInd = false
        };

        context.EstateSaleDetails.Add(entity);
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

        var entity = await context.EstateSaleDetails
            .Include(item => item.Listing)
            .ThenInclude(listing => listing!.Category)
            .Include(item => item.EstateType)
            .FirstOrDefaultAsync(item => item.EstateSaleDetailGUID == id && !item.DeletedInd);

        if (entity is null || entity.Listing is null)
        {
            return NotFound();
        }

        if (!string.Equals(entity.Listing.Category?.Name, EstateSaleCategoryName, StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        if (!CanManageListing(entity.Listing, currentUserId))
        {
            return Forbid();
        }

        await PopulateEstateTypeSelectListAsync(entity.EstitateTypeId);
        var model = ToEditViewModel(entity, entity.Listing);
        return View("~/Views/EstateSaleDetail/Edit.cshtml", model);
    }

    [HttpPost("Edit/{id:guid}")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, [Bind("EstateSaleDetailGUID,ListingGUID,AnnualManagementFee,EstitateTypeId,YearBuilt,SquareFeet,LandSizeSqFt,LandDimensionWxD,NumberOfStoreys,PriceNegotiableInd,AnuualPropertyTax,Bedrooms,Washrooms,Baths,ParkingSpots,ParkingType,FoundationType,HydroType,WaterType,SewerType,ExternalStructures,CoolingType,HeatingType,WaterFrontInd,SoldByOwnerInd,AppliancesIncluded,HasBasementInd,BasementFinishedInd,RentalEquipment,CommunityName,CloseToSchoolInd,CloseToDaycareInd,CloseToBusInd,CloseToShoppingCenterInd,FurnishedInd,HasFireplaceInd,AdditionalInfo")] EstateSaleDetailEditViewModel model)
    {
        if (model.EstateSaleDetailGUID != id)
        {
            return NotFound();
        }

        if (!TryGetCurrentUserGuid(out var currentUserId) && !User.IsInRole("Admin") && !User.IsInRole("Clerk"))
        {
            return Forbid();
        }

        var entity = await context.EstateSaleDetails
            .Include(item => item.Listing)
            .ThenInclude(listing => listing!.Category)
            .Include(item => item.EstateType)
            .FirstOrDefaultAsync(item => item.EstateSaleDetailGUID == id && !item.DeletedInd);

        if (entity is null || entity.Listing is null)
        {
            return NotFound();
        }

        if (!string.Equals(entity.Listing.Category?.Name, EstateSaleCategoryName, StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        if (!CanManageListing(entity.Listing, currentUserId))
        {
            return Forbid();
        }

        var estateTypeExists = await context.EstateTypes.AsNoTracking().AnyAsync(item => item.EstitateTypeId == model.EstitateTypeId && !item.DeletedInd);
        if (!estateTypeExists)
        {
            ModelState.AddModelError(nameof(model.EstitateTypeId), "Estate type is invalid.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateEstateTypeSelectListAsync(model.EstitateTypeId);
            var invalidModel = ToEditViewModel(entity, entity.Listing);

            invalidModel.AnnualManagementFee = model.AnnualManagementFee;
            invalidModel.EstitateTypeId = model.EstitateTypeId;
            invalidModel.YearBuilt = model.YearBuilt;
            invalidModel.SquareFeet = model.SquareFeet;
            invalidModel.LandSizeSqFt = model.LandSizeSqFt;
            invalidModel.LandDimensionWxD = model.LandDimensionWxD;
            invalidModel.NumberOfStoreys = model.NumberOfStoreys;
            invalidModel.PriceNegotiableInd = model.PriceNegotiableInd;
            invalidModel.AnuualPropertyTax = model.AnuualPropertyTax;
            invalidModel.Bedrooms = model.Bedrooms;
            invalidModel.Washrooms = model.Washrooms;
            invalidModel.Baths = model.Baths;
            invalidModel.ParkingSpots = model.ParkingSpots;
            invalidModel.ParkingType = model.ParkingType;
            invalidModel.FoundationType = model.FoundationType;
            invalidModel.HydroType = model.HydroType;
            invalidModel.WaterType = model.WaterType;
            invalidModel.SewerType = model.SewerType;
            invalidModel.ExternalStructures = model.ExternalStructures;
            invalidModel.CoolingType = model.CoolingType;
            invalidModel.HeatingType = model.HeatingType;
            invalidModel.WaterFrontInd = model.WaterFrontInd;
            invalidModel.SoldByOwnerInd = model.SoldByOwnerInd;
            invalidModel.AppliancesIncluded = model.AppliancesIncluded;
            invalidModel.HasBasementInd = model.HasBasementInd;
            invalidModel.BasementFinishedInd = model.BasementFinishedInd;
            invalidModel.RentalEquipment = model.RentalEquipment;
            invalidModel.CommunityName = model.CommunityName;
            invalidModel.CloseToSchoolInd = model.CloseToSchoolInd;
            invalidModel.CloseToDaycareInd = model.CloseToDaycareInd;
            invalidModel.CloseToBusInd = model.CloseToBusInd;
            invalidModel.CloseToShoppingCenterInd = model.CloseToShoppingCenterInd;
            invalidModel.FurnishedInd = model.FurnishedInd;
            invalidModel.HasFireplaceInd = model.HasFireplaceInd;
            invalidModel.AdditionalInfo = model.AdditionalInfo;

            return View("~/Views/EstateSaleDetail/Edit.cshtml", invalidModel);
        }

        entity.AnnualManagementFee = model.AnnualManagementFee;
        entity.EstitateTypeId = model.EstitateTypeId;
        entity.YearBuilt = model.YearBuilt;
        entity.SquareFeet = model.SquareFeet;
        entity.LandSizeSqFt = model.LandSizeSqFt;
        entity.LandDimensionWxD = model.LandDimensionWxD;
        entity.NumberOfStoreys = model.NumberOfStoreys;
        entity.PriceNegotiableInd = model.PriceNegotiableInd;
        entity.AnuualPropertyTax = model.AnuualPropertyTax;
        entity.Bedrooms = model.Bedrooms;
        entity.Washrooms = model.Washrooms;
        entity.Baths = model.Baths;
        entity.ParkingSpots = model.ParkingSpots;
        entity.ParkingType = model.ParkingType;
        entity.FoundationType = model.FoundationType;
        entity.HydroType = model.HydroType;
        entity.WaterType = model.WaterType;
        entity.SewerType = model.SewerType;
        entity.ExternalStructures = model.ExternalStructures;
        entity.CoolingType = model.CoolingType;
        entity.HeatingType = model.HeatingType;
        entity.WaterFrontInd = model.WaterFrontInd;
        entity.SoldByOwnerInd = model.SoldByOwnerInd;
        entity.AppliancesIncluded = model.AppliancesIncluded;
        entity.HasBasementInd = model.HasBasementInd;
        entity.BasementFinishedInd = model.BasementFinishedInd;
        entity.RentalEquipment = model.RentalEquipment;
        entity.CommunityName = model.CommunityName;
        entity.CloseToSchoolInd = model.CloseToSchoolInd;
        entity.CloseToDaycareInd = model.CloseToDaycareInd;
        entity.CloseToBusInd = model.CloseToBusInd;
        entity.CloseToShoppingCenterInd = model.CloseToShoppingCenterInd;
        entity.FurnishedInd = model.FurnishedInd;
        entity.HasFireplaceInd = model.HasFireplaceInd;
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

        var entity = await context.EstateSaleDetails
            .AsNoTracking()
            .Include(item => item.Listing)
            .ThenInclude(listing => listing!.Category)
            .Include(item => item.EstateType)
            .FirstOrDefaultAsync(item => item.EstateSaleDetailGUID == id && !item.DeletedInd);

        if (entity is null || entity.Listing is null)
        {
            return NotFound();
        }

        if (!string.Equals(entity.Listing.Category?.Name, EstateSaleCategoryName, StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        if (!CanManageListing(entity.Listing, currentUserId))
        {
            return Forbid();
        }

        return View("~/Views/EstateSaleDetail/Delete.cshtml", ToEditViewModel(entity, entity.Listing));
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

        var entity = await context.EstateSaleDetails
            .Include(item => item.Listing)
            .ThenInclude(listing => listing!.Category)
            .FirstOrDefaultAsync(item => item.EstateSaleDetailGUID == id && !item.DeletedInd);

        if (entity is null || entity.Listing is null)
        {
            return NotFound();
        }

        if (!string.Equals(entity.Listing.Category?.Name, EstateSaleCategoryName, StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        if (!CanManageListing(entity.Listing, currentUserId))
        {
            return Forbid();
        }

        context.EstateSaleDetails.Remove(entity);
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

        if (!string.Equals(listing.Category?.Name, EstateSaleCategoryName, StringComparison.OrdinalIgnoreCase))
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

        var imagePath = await SaveImageFromDataUrlAsync(croppedImageData, "ListingImages");
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            TempData["MessageError"] = "Invalid image data.";
            return RedirectToAction(nameof(Index), new { listingGuid });
        }

        var thumbnailPath = string.IsNullOrWhiteSpace(thumbnailImageData)
            ? null
            : await SaveImageFromDataUrlAsync(thumbnailImageData, "ListingImages");

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

        if (!string.Equals(listing.Category?.Name, EstateSaleCategoryName, StringComparison.OrdinalIgnoreCase))
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

    [HttpPost("UpsertVideo/{listingGuid:guid}")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpsertVideo(Guid listingGuid, Guid? listingVideoGuid, string? name, int? sortOrder, IFormFile? editedVideoFile, string? thumbnailImageData)
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

        if (!string.Equals(listing.Category?.Name, EstateSaleCategoryName, StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        if (!CanManageListing(listing, currentUserId))
        {
            return Forbid();
        }

        ListingVideo? listingVideo = null;
        if (listingVideoGuid.HasValue)
        {
            listingVideo = await context.ListingVideos.FirstOrDefaultAsync(item => item.ListingVideoGUID == listingVideoGuid.Value && item.ListingGUID == listingGuid && !item.DeletedInd);
        }

        if (listingVideo is null)
        {
            listingVideo = await context.ListingVideos
                .Where(item => item.ListingGUID == listingGuid && !item.DeletedInd)
                .OrderBy(item => item.SortOrder)
                .ThenBy(item => item.CreatedDate)
                .FirstOrDefaultAsync();
        }

        if (listingVideo is null)
        {
            listingVideo = new ListingVideo
            {
                ListingVideoGUID = Guid.NewGuid(),
                ListingGUID = listingGuid,
                CreatedBy = User.Identity?.Name ?? string.Empty,
                CreatedDate = CanHappy.Common.EasternTime.Now,
                DeletedInd = false
            };
            context.ListingVideos.Add(listingVideo);
        }

        if (editedVideoFile is not null)
        {
            if (editedVideoFile.Length <= 0)
            {
                TempData["MessageError"] = "Video file is empty.";
                return RedirectToAction(nameof(Index), new { listingGuid });
            }

            if (editedVideoFile.Length > MaxVideoBytes)
            {
                TempData["MessageError"] = "Edited video must be under 50MB.";
                return RedirectToAction(nameof(Index), new { listingGuid });
            }

            var savedVideoPath = await SaveUploadedVideoAsync(editedVideoFile, "ListingVideos");
            if (string.IsNullOrWhiteSpace(savedVideoPath))
            {
                TempData["MessageError"] = "Could not save video file.";
                return RedirectToAction(nameof(Index), new { listingGuid });
            }

            listingVideo.VideoURL = savedVideoPath;
            listingVideo.VideoSize = ToMegaByteSizeText(editedVideoFile.Length);
        }

        if (!string.IsNullOrWhiteSpace(thumbnailImageData))
        {
            var thumbnailPath = await SaveImageFromDataUrlAsync(thumbnailImageData, "ListingVideoThumbnails");
            if (!string.IsNullOrWhiteSpace(thumbnailPath))
            {
                listingVideo.ThumbnailURL = thumbnailPath;
            }
        }

        if (string.IsNullOrWhiteSpace(listingVideo.VideoURL))
        {
            TempData["MessageError"] = "Video is required.";
            return RedirectToAction(nameof(Index), new { listingGuid });
        }

        if (string.IsNullOrWhiteSpace(listingVideo.ThumbnailURL))
        {
            TempData["MessageError"] = "Video thumbnail is required.";
            return RedirectToAction(nameof(Index), new { listingGuid });
        }

        listingVideo.Name = string.IsNullOrWhiteSpace(name) ? string.Empty : name.Trim();
        listingVideo.SortOrder = sortOrder.GetValueOrDefault(0);
        listingVideo.ModifiedBy = User.Identity?.Name ?? string.Empty;
        listingVideo.ModifiedDate = CanHappy.Common.EasternTime.Now;

        var extraVideos = await context.ListingVideos
            .Where(item => item.ListingGUID == listingGuid && !item.DeletedInd && item.ListingVideoGUID != listingVideo.ListingVideoGUID)
            .ToListAsync();
        foreach (var extraVideo in extraVideos)
        {
            extraVideo.DeletedInd = true;
            extraVideo.ModifiedBy = User.Identity?.Name ?? string.Empty;
            extraVideo.ModifiedDate = CanHappy.Common.EasternTime.Now;
        }

        await context.SaveChangesAsync();
        TempData["MessageSuccess"] = "Video saved.";
        return RedirectToAction(nameof(Index), new { listingGuid });
    }

    [HttpPost("DeleteVideo/{listingGuid:guid}/{listingVideoGuid:guid}")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteVideo(Guid listingGuid, Guid listingVideoGuid)
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

        if (!string.Equals(listing.Category?.Name, EstateSaleCategoryName, StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        if (!CanManageListing(listing, currentUserId))
        {
            return Forbid();
        }

        var listingVideo = await context.ListingVideos.FirstOrDefaultAsync(item => item.ListingVideoGUID == listingVideoGuid && item.ListingGUID == listingGuid && !item.DeletedInd);
        if (listingVideo is null)
        {
            TempData["MessageError"] = "Video not found.";
            return RedirectToAction(nameof(Index), new { listingGuid });
        }

        var videoUrlToDelete = listingVideo.VideoURL;
        var thumbnailUrlToDelete = listingVideo.ThumbnailURL;

        listingVideo.DeletedInd = true;
        listingVideo.ModifiedBy = User.Identity?.Name ?? string.Empty;
        listingVideo.ModifiedDate = CanHappy.Common.EasternTime.Now;
        await context.SaveChangesAsync();

        TryDeleteMediaFile(videoUrlToDelete);
        if (!string.Equals(videoUrlToDelete, thumbnailUrlToDelete, StringComparison.OrdinalIgnoreCase))
        {
            TryDeleteMediaFile(thumbnailUrlToDelete);
        }

        TempData["MessageSuccess"] = "Video deleted.";
        return RedirectToAction(nameof(Index), new { listingGuid });
    }

    [HttpPost("UpsertRoom/{listingGuid:guid}")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpsertRoom(Guid listingGuid, Guid? estateRoomGuid, string? roomName, string? roomSizeFtxFt, int? sortOrder, int? onFloorNumber, string? croppedImageData, string? thumbnailImageData)
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

        if (!string.Equals(listing.Category?.Name, EstateSaleCategoryName, StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        if (!CanManageListing(listing, currentUserId))
        {
            return Forbid();
        }

        var detail = await context.EstateSaleDetails.FirstOrDefaultAsync(item => item.ListingGUID == listingGuid && !item.DeletedInd);
        if (detail is null)
        {
            TempData["MessageError"] = "Please create Estate Sale detail first.";
            return RedirectToAction(nameof(Index), new { listingGuid });
        }

        EstateRoom? room = null;
        if (estateRoomGuid.HasValue)
        {
            room = await context.EstateRooms.FirstOrDefaultAsync(item => item.EstateRoomGUID == estateRoomGuid.Value && item.EstateSaleDetailGUID == detail.EstateSaleDetailGUID && !item.DeletedInd);
        }

        if (room is null)
        {
            room = new EstateRoom
            {
                EstateRoomGUID = Guid.NewGuid(),
                EstateSaleDetailGUID = detail.EstateSaleDetailGUID,
                CreatedBy = User.Identity?.Name,
                CreatedDate = CanHappy.Common.EasternTime.Now,
                DeletedInd = false
            };
            context.EstateRooms.Add(room);
        }

        room.RoomName = string.IsNullOrWhiteSpace(roomName) ? null : roomName.Trim();
        room.RoomSizeFtxFt = string.IsNullOrWhiteSpace(roomSizeFtxFt) ? null : roomSizeFtxFt.Trim();
        room.SortOrder = sortOrder.GetValueOrDefault(0);
        room.OnFloorNumber = onFloorNumber.GetValueOrDefault(0);

        if (!string.IsNullOrWhiteSpace(croppedImageData))
        {
            var imagePath = await SaveImageFromDataUrlAsync(croppedImageData, "EstateRoomImages");
            if (string.IsNullOrWhiteSpace(imagePath))
            {
                TempData["MessageError"] = "Invalid room image data.";
                return RedirectToAction(nameof(Index), new { listingGuid });
            }

            var thumbnailPath = string.IsNullOrWhiteSpace(thumbnailImageData)
                ? null
                : await SaveImageFromDataUrlAsync(thumbnailImageData, "EstateRoomImages");

            if (string.IsNullOrWhiteSpace(thumbnailPath))
            {
                thumbnailPath = imagePath;
            }

            room.ImageURL = imagePath;
            room.ThumbnailURL = thumbnailPath;
        }

        room.ModifiedBy = User.Identity?.Name;
        room.ModifiedDate = CanHappy.Common.EasternTime.Now;

        await context.SaveChangesAsync();

        TempData["MessageSuccess"] = "Room saved.";
        return RedirectToAction(nameof(Index), new { listingGuid });
    }

    [HttpPost("DeleteRoom/{listingGuid:guid}/{estateRoomGuid:guid}")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRoom(Guid listingGuid, Guid estateRoomGuid)
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

        if (!string.Equals(listing.Category?.Name, EstateSaleCategoryName, StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        if (!CanManageListing(listing, currentUserId))
        {
            return Forbid();
        }

        var detail = await context.EstateSaleDetails.FirstOrDefaultAsync(item => item.ListingGUID == listingGuid && !item.DeletedInd);
        if (detail is null)
        {
            return RedirectToAction(nameof(Index), new { listingGuid });
        }

        var room = await context.EstateRooms.FirstOrDefaultAsync(item => item.EstateRoomGUID == estateRoomGuid && item.EstateSaleDetailGUID == detail.EstateSaleDetailGUID && !item.DeletedInd);
        if (room is null)
        {
            TempData["MessageError"] = "Room not found.";
            return RedirectToAction(nameof(Index), new { listingGuid });
        }

        room.DeletedInd = true;
        room.ModifiedBy = User.Identity?.Name;
        room.ModifiedDate = CanHappy.Common.EasternTime.Now;
        await context.SaveChangesAsync();

        TempData["MessageSuccess"] = "Room deleted.";
        return RedirectToAction(nameof(Index), new { listingGuid });
    }

    private EstateSaleDetailEditViewModel ToEditViewModel(EstateSaleDetail detail, Listing listing)
    {
        return new EstateSaleDetailEditViewModel
        {
            EstateSaleDetailGUID = detail.EstateSaleDetailGUID,
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
            AnnualManagementFee = detail.AnnualManagementFee,
            EstitateTypeId = detail.EstitateTypeId,
            EstitateTypeName = detail.EstateType?.Name,
            YearBuilt = detail.YearBuilt,
            SquareFeet = detail.SquareFeet,
            LandSizeSqFt = detail.LandSizeSqFt,
            LandDimensionWxD = detail.LandDimensionWxD,
            NumberOfStoreys = detail.NumberOfStoreys,
            PriceNegotiableInd = detail.PriceNegotiableInd,
            AnuualPropertyTax = detail.AnuualPropertyTax,
            Bedrooms = detail.Bedrooms,
            Washrooms = detail.Washrooms,
            Baths = detail.Baths,
            ParkingSpots = detail.ParkingSpots,
            ParkingType = detail.ParkingType,
            FoundationType = detail.FoundationType,
            HydroType = detail.HydroType,
            WaterType = detail.WaterType,
            SewerType = detail.SewerType,
            ExternalStructures = detail.ExternalStructures,
            CoolingType = detail.CoolingType,
            HeatingType = detail.HeatingType,
            WaterFrontInd = detail.WaterFrontInd,
            SoldByOwnerInd = detail.SoldByOwnerInd,
            AppliancesIncluded = detail.AppliancesIncluded,
            HasBasementInd = detail.HasBasementInd,
            BasementFinishedInd = detail.BasementFinishedInd,
            RentalEquipment = detail.RentalEquipment,
            CommunityName = detail.CommunityName,
            CloseToSchoolInd = detail.CloseToSchoolInd,
            CloseToDaycareInd = detail.CloseToDaycareInd,
            CloseToBusInd = detail.CloseToBusInd,
            CloseToShoppingCenterInd = detail.CloseToShoppingCenterInd,
            FurnishedInd = detail.FurnishedInd,
            HasFireplaceInd = detail.HasFireplaceInd,
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

    private string BuildListingUrl(Guid listingGuid, string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return returnUrl;
        }

        return $"/EstateSaleDetail/Index?listingGuid={listingGuid}";
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
            .Where(listing => listing.Category != null && listing.Category.Name == EstateSaleCategoryName)
            .Where(listing => !context.EstateSaleDetails.Any(detail => detail.ListingGUID == listing.ListingGUID && !detail.DeletedInd));

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

    private async Task PopulateEstateTypeSelectListAsync(int? selectedEstitateTypeId = null)
    {
        var estateTypes = await context.EstateTypes
            .AsNoTracking()
            .Where(item => !item.DeletedInd)
            .OrderBy(item => item.Name)
            .ToListAsync();

        ViewData["EstitateTypeId"] = new SelectList(estateTypes, nameof(EstateType.EstitateTypeId), nameof(EstateType.Name), selectedEstitateTypeId);
    }

    private async Task<string?> SaveImageFromDataUrlAsync(string dataUrl, string folderName)
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
        var relativePath = $"/{folderName}/{fileName}";
        var folderPath = Path.Combine(environment.WebRootPath, folderName);

        Directory.CreateDirectory(folderPath);
        var filePath = Path.Combine(folderPath, fileName);
        await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);

        return relativePath;
    }

    private async Task<string?> SaveUploadedVideoAsync(IFormFile file, string folderName)
    {
        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".webm";
        }

        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var relativePath = $"/{folderName}/{fileName}";
        var folderPath = Path.Combine(environment.WebRootPath, folderName);
        Directory.CreateDirectory(folderPath);

        var filePath = Path.Combine(folderPath, fileName);
        await using var output = System.IO.File.Create(filePath);
        await file.CopyToAsync(output);
        return relativePath;
    }

    private void TryDeleteMediaFile(string? mediaPath)
    {
        if (string.IsNullOrWhiteSpace(mediaPath))
        {
            return;
        }

        try
        {
            var normalizedPath = mediaPath.Trim();

            var queryIndex = normalizedPath.IndexOf('?');
            if (queryIndex >= 0)
            {
                normalizedPath = normalizedPath[..queryIndex];
            }

            var fragmentIndex = normalizedPath.IndexOf('#');
            if (fragmentIndex >= 0)
            {
                normalizedPath = normalizedPath[..fragmentIndex];
            }

            if (Uri.TryCreate(normalizedPath, UriKind.Absolute, out var absoluteUri))
            {
                normalizedPath = absoluteUri.AbsolutePath;
            }

            normalizedPath = normalizedPath.Replace('\\', '/');
            if (!normalizedPath.StartsWith('/'))
            {
                return;
            }

            var webRootFullPath = Path.GetFullPath(environment.WebRootPath);
            var relativeFsPath = normalizedPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var targetFullPath = Path.GetFullPath(Path.Combine(environment.WebRootPath, relativeFsPath));

            if (!targetFullPath.StartsWith(webRootFullPath, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (System.IO.File.Exists(targetFullPath))
            {
                System.IO.File.Delete(targetFullPath);
            }
        }
        catch
        {
        }
    }

    private static string ToMegaByteSizeText(long bytes)
    {
        var megaBytes = bytes / 1024d / 1024d;
        return $"{megaBytes:0.##} MB";
    }
}
