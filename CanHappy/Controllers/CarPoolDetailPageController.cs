using CanHappy.Data;
using CanHappy.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CanHappy.Controllers;

[Route("CarPoolDetail")]
public class CarPoolDetailPageController(ApplicationDbContext context, IWebHostEnvironment environment) : Controller
{
    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(Guid? listingGuid, bool mine = false)
    {
        var hasUserGuid = TryGetCurrentUserGuid(out var currentUserId);
        var canManageByRole = User.IsInRole("Admin") || User.IsInRole("Clerk");

        if (mine && !hasUserGuid)
        {
            return Forbid();
        }

        var model = new CarPoolDetailIndexPageViewModel
        {
            ListingGUIDFilter = listingGuid,
            MineOnly = mine
        };

        var query = context.CarPoolDetails
            .AsNoTracking()
            .Include(item => item.Listing)
            .Include(item => item.CarPoolType)
            .Include(item => item.CarPoolStatus)
            .Where(item => !item.DeletedInd && item.Listing != null && !item.Listing.DeletedInd)
            .AsQueryable();

        if (mine && hasUserGuid)
        {
            query = query.Where(item => item.Listing!.UserId == currentUserId);
        }

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

            model.ListingCard = new CarPoolListingCardViewModel
            {
                ListingGUID = listing.ListingGUID,
                UserId = listing.UserId,
                Subject = listing.Subject,
                Description = listing.Description,
                KeyWords = listing.KeyWords,
                CategoryName = listing.Category?.Name,
                SubcategoryName = listing.Subcategory?.Name,
                ProvinceName = listing.Province?.Name,
                CityName = listing.City?.Name,
                Address = listing.Address,
                PostalCode = listing.PostalCode,
                Price = listing.Price,
                ContactPhone = listing.ContactPhone,
                ContactName = listing.ContactName,
                ShowContactInd = listing.ShowContactInd
            };

            model.CanManageFocusedListing = canManageByRole || (hasUserGuid && listing.UserId != Guid.Empty && listing.UserId == currentUserId);

            model.ListingImages = await context.ListingImages
                .AsNoTracking()
                .Where(image => image.ListingGUID == listing.ListingGUID && !image.DeletedInd)
                .OrderBy(image => image.SorOrder)
                .ThenBy(image => image.CreatedDate)
                .Take(6)
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
            .Select(item => new CarPoolDetailIndexItemViewModel
            {
                CarPoolDetailGUID = item.CarPoolDetailGUID,
                ListingGUID = item.ListingGUID,
                ListingUserId = item.Listing!.UserId,
                Subject = item.Listing.Subject,
                Description = item.Listing.Description,
                Price = item.Listing.Price,
                FromCity = item.FromCity,
                DestinationCity = item.DestinationCity,
                LeavingDate = item.LeavingDate,
                LeavingTime = item.LeavingTime,
                CarPoolTypeName = item.CarPoolType != null ? item.CarPoolType.Name : null,
                CarPoolStatusName = item.CarPoolStatus != null ? item.CarPoolStatus.Name : null,
                CanManage = canManageByRole || (hasUserGuid && item.Listing.UserId != Guid.Empty && item.Listing.UserId == currentUserId)
            })
            .ToListAsync();

        if (listingGuid.HasValue && model.ListingCard is not null)
        {
            var focusDetail = model.Items.FirstOrDefault(item => item.ListingGUID == listingGuid.Value);
            if (focusDetail is not null)
            {
                var detailEntity = await context.CarPoolDetails
                    .AsNoTracking()
                    .Include(item => item.CarPoolType)
                    .Include(item => item.CarPoolStatus)
                    .FirstOrDefaultAsync(item => item.CarPoolDetailGUID == focusDetail.CarPoolDetailGUID);

                if (detailEntity is not null)
                {
                    model.HasExistingFocusDetail = true;
                    model.FocusDetail = ToEditViewModel(detailEntity);
                    model.FocusCarPoolTypeName = detailEntity.CarPoolType?.Name;
                    model.FocusCarPoolStatusName = detailEntity.CarPoolStatus?.Name;
                }
            }
            else
            {
                model.FocusDetail = new CarPoolDetailEditViewModel
                {
                    ListingGUID = model.ListingCard.ListingGUID,
                    CarPoolTypeId = 1,
                    CarPoolStatusId = 1
                };
            }
        }

        return View("~/Views/CarPoolDetail/Index.cshtml", model);
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

            if (!CanManageListing(listing, currentUserId))
            {
                return Forbid();
            }

            var hasExisting = await context.CarPoolDetails
                .AsNoTracking()
                .AnyAsync(item => item.ListingGUID == listingGuid.Value && !item.DeletedInd);

            if (hasExisting)
            {
                TempData["MessageError"] = "The listing details already exist! Edit them if changes needed.";
                return RedirectToAction(nameof(Index), new { listingGuid = listingGuid.Value });
            }
        }

        await PopulateSelectListsAsync(currentUserId, listingGuid);
        return View("~/Views/CarPoolDetail/Create.cshtml", new CarPoolDetailEditViewModel
        {
            ListingGUID = listingGuid ?? Guid.Empty,
            CarPoolTypeId = 1,
            CarPoolStatusId = 1
        });
    }

    [HttpPost("Create")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CarPoolDetailEditViewModel model)
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
        else if (!CanManageListing(listing, currentUserId))
        {
            return Forbid();
        }

        var hasExisting = await context.CarPoolDetails.AnyAsync(item => item.ListingGUID == model.ListingGUID && !item.DeletedInd);
        if (hasExisting)
        {
            ModelState.AddModelError(nameof(model.ListingGUID), "This listing already has Car Pool details.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateSelectListsAsync(currentUserId, model.ListingGUID, model.CarPoolTypeId, model.CarPoolStatusId);
            return View("~/Views/CarPoolDetail/Create.cshtml", model);
        }

        var entity = new CarPoolDetail
        {
            CarPoolDetailGUID = Guid.NewGuid(),
            ListingGUID = model.ListingGUID,
            CarPoolTypeId = model.CarPoolTypeId,
            CarPoolStatusId = model.CarPoolStatusId,
            FromCity = model.FromCity,
            LeavingDate = model.LeavingDate,
            WeekDays = model.WeekDays,
            LeavingTime = model.LeavingTime,
            PickupLocation = model.PickupLocation,
            DestinationCity = model.DestinationCity,
            DropoffLocation = model.DropoffLocation,
            TripStops = model.TripStops,
            VehicleModelYear = model.VehicleModelYear,
            VehicleLicensePlateNumber = model.VehicleLicensePlateNumber,
            VehicleColor = model.VehicleColor,
            WeeklyScheduleInd = model.WeeklyScheduleInd,
            SmallBagAllowedInd = model.SmallBagAllowedInd,
            MediumBagAllowedInd = model.MediumBagAllowedInd,
            OneLargeBagAllowedInd = model.OneLargeBagAllowedInd,
            AdditionalInfo = model.AdditionalInfo,
            CreatedBy = User.Identity?.Name,
            CreatedDate = CanHappy.Common.EasternTime.Now,
            DeletedInd = false
        };

        context.CarPoolDetails.Add(entity);
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

        var entity = await context.CarPoolDetails
            .Include(item => item.Listing)
            .FirstOrDefaultAsync(item => item.CarPoolDetailGUID == id && !item.DeletedInd);

        if (entity is null || entity.Listing is null)
        {
            return NotFound();
        }

        if (!CanManageListing(entity.Listing, currentUserId))
        {
            return Forbid();
        }

        var model = ToEditViewModel(entity);
        await PopulateSelectListsAsync(currentUserId, entity.ListingGUID, model.CarPoolTypeId, model.CarPoolStatusId);
        return View("~/Views/CarPoolDetail/Edit.cshtml", model);
    }

    [HttpPost("Edit/{id:guid}")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, CarPoolDetailEditViewModel model)
    {
        if (model.CarPoolDetailGUID != id)
        {
            return NotFound();
        }

        if (!TryGetCurrentUserGuid(out var currentUserId) && !User.IsInRole("Admin") && !User.IsInRole("Clerk"))
        {
            return Forbid();
        }

        var entity = await context.CarPoolDetails
            .Include(item => item.Listing)
            .FirstOrDefaultAsync(item => item.CarPoolDetailGUID == id && !item.DeletedInd);

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
            await PopulateSelectListsAsync(currentUserId, entity.ListingGUID, model.CarPoolTypeId, model.CarPoolStatusId);
            return View("~/Views/CarPoolDetail/Edit.cshtml", model);
        }

        entity.CarPoolTypeId = model.CarPoolTypeId;
        entity.CarPoolStatusId = model.CarPoolStatusId;
        entity.FromCity = model.FromCity;
        entity.LeavingDate = model.LeavingDate;
        entity.WeekDays = model.WeekDays;
        entity.LeavingTime = model.LeavingTime;
        entity.PickupLocation = model.PickupLocation;
        entity.DestinationCity = model.DestinationCity;
        entity.DropoffLocation = model.DropoffLocation;
        entity.TripStops = model.TripStops;
        entity.VehicleModelYear = model.VehicleModelYear;
        entity.VehicleLicensePlateNumber = model.VehicleLicensePlateNumber;
        entity.VehicleColor = model.VehicleColor;
        entity.WeeklyScheduleInd = model.WeeklyScheduleInd;
        entity.SmallBagAllowedInd = model.SmallBagAllowedInd;
        entity.MediumBagAllowedInd = model.MediumBagAllowedInd;
        entity.OneLargeBagAllowedInd = model.OneLargeBagAllowedInd;
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

        var entity = await context.CarPoolDetails
            .AsNoTracking()
            .Include(item => item.Listing)
            .FirstOrDefaultAsync(item => item.CarPoolDetailGUID == id && !item.DeletedInd);

        if (entity is null || entity.Listing is null)
        {
            return NotFound();
        }

        if (!CanManageListing(entity.Listing, currentUserId))
        {
            return Forbid();
        }

        var model = ToEditViewModel(entity);
        return View("~/Views/CarPoolDetail/Delete.cshtml", model);
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

        var entity = await context.CarPoolDetails
            .Include(item => item.Listing)
            .FirstOrDefaultAsync(item => item.CarPoolDetailGUID == id && !item.DeletedInd);

        if (entity is null || entity.Listing is null)
        {
            return NotFound();
        }

        if (!CanManageListing(entity.Listing, currentUserId))
        {
            return Forbid();
        }

        var listingGuid = entity.ListingGUID;
        entity.DeletedInd = true;
        entity.ModifiedBy = User.Identity?.Name;
        entity.ModifiedDate = CanHappy.Common.EasternTime.Now;

        await context.SaveChangesAsync();

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

    private CarPoolDetailEditViewModel ToEditViewModel(CarPoolDetail detail)
    {
        return new CarPoolDetailEditViewModel
        {
            CarPoolDetailGUID = detail.CarPoolDetailGUID,
            ListingGUID = detail.ListingGUID,
            CarPoolTypeId = detail.CarPoolTypeId,
            CarPoolStatusId = detail.CarPoolStatusId,
            FromCity = detail.FromCity,
            LeavingDate = detail.LeavingDate,
            WeekDays = detail.WeekDays,
            LeavingTime = detail.LeavingTime,
            PickupLocation = detail.PickupLocation,
            DestinationCity = detail.DestinationCity,
            DropoffLocation = detail.DropoffLocation,
            TripStops = detail.TripStops,
            VehicleModelYear = detail.VehicleModelYear,
            VehicleLicensePlateNumber = detail.VehicleLicensePlateNumber,
            VehicleColor = detail.VehicleColor,
            WeeklyScheduleInd = detail.WeeklyScheduleInd,
            SmallBagAllowedInd = detail.SmallBagAllowedInd,
            MediumBagAllowedInd = detail.MediumBagAllowedInd,
            OneLargeBagAllowedInd = detail.OneLargeBagAllowedInd,
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

    private async Task PopulateSelectListsAsync(Guid currentUserId, Guid? selectedListingId = null, int selectedTypeId = 1, int selectedStatusId = 1)
    {
        var isPrivileged = User.IsInRole("Admin") || User.IsInRole("Clerk");

        var query = context.Listings
            .AsNoTracking()
            .Include(listing => listing.Category)
            .Where(listing => !listing.DeletedInd)
            .Where(listing => listing.Category != null && listing.Category.Name == "Car Pool")
            .Where(listing => !context.CarPoolDetails.Any(detail => detail.ListingGUID == listing.ListingGUID && !detail.DeletedInd)
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

        var carPoolTypes = await context.CarPoolTypes
            .AsNoTracking()
            .Where(item => !item.DeletedInd)
            .OrderBy(item => item.CarPoolTypeId)
            .ToListAsync();

        var carPoolStatuses = await context.CarPoolStatuses
            .AsNoTracking()
            .Where(item => !item.DeletedInd)
            .OrderBy(item => item.CarPoolStatusId)
            .ToListAsync();

        ViewData["CarPoolTypeId"] = new SelectList(carPoolTypes, "CarPoolTypeId", "Name", selectedTypeId);
        ViewData["CarPoolStatusId"] = new SelectList(carPoolStatuses, "CarPoolStatusId", "Name", selectedStatusId);
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
}
