using CanHappy.Data;
using CanHappy.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CanHappy.Controllers;

[Route("BuySellDetail")]
public class BuySellDetailPageController(ApplicationDbContext context, IWebHostEnvironment environment) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(Guid? listingGuid)
    {
        var hasUserGuid = TryGetCurrentUserGuid(out var currentUserId);
        var canManageByRole = User.IsInRole("Admin") || User.IsInRole("Clerk");

        var model = new BuySellDetailIndexPageViewModel
        {
            ListingGUIDFilter = listingGuid
        };

        var query = context.BuySellDetails
            .AsNoTracking()
            .Include(item => item.Listing)
            .Where(item => !item.DeletedInd && item.Listing != null && !item.Listing.DeletedInd)
            .AsQueryable();

        if (listingGuid.HasValue)
        {
            query = query.Where(item => item.ListingGUID == listingGuid.Value);
            ViewData["ListingGUID"] = listingGuid.Value;

            var listing = await context.Listings
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.ListingGUID == listingGuid.Value && !item.DeletedInd);

            if (listing is null)
            {
                return NotFound();
            }

            model.ListingCard = new BuySellDetailListingCardViewModel
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
                Model = listing.Model,
                Quantity = listing.Quantity
            };

            model.CanManageFocusedListing = canManageByRole || (hasUserGuid && listing.UserId != Guid.Empty && listing.UserId == currentUserId);

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
            .Select(item => new BuySellDetailIndexItemViewModel
            {
                BuySellDetailGUID = item.BuySellDetailGUID,
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
                BuySellModel = item.Model,
                Material = item.Material,
                NegotiablePriceInd = item.NegotiablePriceInd,
                DeliveryAvailableInd = item.DeliveryAvailableInd,
                PickupAvailableInd = item.PickupAvailableInd,
                WarrantyInfo = item.WarrantyInfo,
                AdditionalDetails = item.AdditionalDetails,
                CanManage = canManageByRole || (hasUserGuid && item.Listing.UserId != Guid.Empty && item.Listing.UserId == currentUserId)
            })
            .ToListAsync();

        if (listingGuid.HasValue && model.ListingCard is not null)
        {
            var focusDetail = model.Items.FirstOrDefault(item => item.ListingGUID == listingGuid.Value);
            if (focusDetail is not null)
            {
                model.HasExistingFocusDetail = true;
                model.FocusDetail = new BuySellDetailEditViewModel
                {
                    BuySellDetailGUID = focusDetail.BuySellDetailGUID,
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
                    BuySellModel = focusDetail.BuySellModel,
                    Material = focusDetail.Material,
                    NegotiablePriceInd = focusDetail.NegotiablePriceInd,
                    DeliveryAvailableInd = focusDetail.DeliveryAvailableInd,
                    PickupAvailableInd = focusDetail.PickupAvailableInd,
                    WarrantyInfo = focusDetail.WarrantyInfo,
                    AdditionalDetails = focusDetail.AdditionalDetails
                };
            }
            else
            {
                model.FocusDetail = new BuySellDetailEditViewModel
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

        return View("~/Views/BuySellDetail/Index.cshtml", model);
    }

    [HttpGet("Create")]
    [Authorize]
    public async Task<IActionResult> Create(Guid? listingGuid)
    {
        if (!TryGetCurrentUserGuid(out var currentUserId) && !User.IsInRole("Admin") && !User.IsInRole("Clerk"))
        {
            return Forbid();
        }

        await PopulateListingSelectListAsync(currentUserId, listingGuid);
        return View("~/Views/BuySellDetail/Create.cshtml", new BuySellDetailEditViewModel
        {
            ListingGUID = listingGuid ?? Guid.Empty
        });
    }

    [HttpPost("Create")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ListingGUID,BuySellModel,Material,NegotiablePriceInd,DeliveryAvailableInd,PickupAvailableInd,WarrantyInfo,AdditionalDetails")] BuySellDetailEditViewModel model)
    {
        if (!TryGetCurrentUserGuid(out var currentUserId) && !User.IsInRole("Admin") && !User.IsInRole("Clerk"))
        {
            return Forbid();
        }

        var listing = await context.Listings
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.ListingGUID == model.ListingGUID && !item.DeletedInd);

        if (listing is null)
        {
            ModelState.AddModelError(nameof(model.ListingGUID), "Listing not found.");
        }
        else if (!CanManageListing(listing, currentUserId))
        {
            return Forbid();
        }

        var hasExisting = await context.BuySellDetails.AnyAsync(item => item.ListingGUID == model.ListingGUID && !item.DeletedInd);
        if (hasExisting)
        {
            ModelState.AddModelError(nameof(model.ListingGUID), "This listing already has Buy/Sell details.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateListingSelectListAsync(currentUserId, model.ListingGUID);
            return View("~/Views/BuySellDetail/Create.cshtml", model);
        }

        var entity = new BuySellDetail
        {
            BuySellDetailGUID = Guid.NewGuid(),
            ListingGUID = model.ListingGUID,
            Model = model.BuySellModel,
            Material = model.Material,
            NegotiablePriceInd = model.NegotiablePriceInd,
            DeliveryAvailableInd = model.DeliveryAvailableInd,
            PickupAvailableInd = model.PickupAvailableInd,
            WarrantyInfo = model.WarrantyInfo,
            AdditionalDetails = model.AdditionalDetails,
            CreatedBy = User.Identity?.Name,
            CreatedDate = CanHappy.Common.EasternTime.Now,
            DeletedInd = false
        };

        context.BuySellDetails.Add(entity);
        await context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Edit/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Edit(Guid id)
    {
        if (!TryGetCurrentUserGuid(out var currentUserId) && !User.IsInRole("Admin") && !User.IsInRole("Clerk"))
        {
            return Forbid();
        }

        var entity = await context.BuySellDetails
            .Include(item => item.Listing)
            .FirstOrDefaultAsync(item => item.BuySellDetailGUID == id && !item.DeletedInd);

        if (entity is null || entity.Listing is null)
        {
            return NotFound();
        }

        if (!CanManageListing(entity.Listing, currentUserId))
        {
            return Forbid();
        }

        var model = ToEditViewModel(entity, entity.Listing);
        return View("~/Views/BuySellDetail/Edit.cshtml", model);
    }

    [HttpPost("Edit/{id:guid}")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, [Bind("BuySellDetailGUID,ListingGUID,BuySellModel,Material,NegotiablePriceInd,DeliveryAvailableInd,PickupAvailableInd,WarrantyInfo,AdditionalDetails")] BuySellDetailEditViewModel model)
    {
        if (model.BuySellDetailGUID != id)
        {
            return NotFound();
        }

        if (!TryGetCurrentUserGuid(out var currentUserId) && !User.IsInRole("Admin") && !User.IsInRole("Clerk"))
        {
            return Forbid();
        }

        var entity = await context.BuySellDetails
            .Include(item => item.Listing)
            .FirstOrDefaultAsync(item => item.BuySellDetailGUID == id && !item.DeletedInd);

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
            var invalidModel = ToEditViewModel(entity, entity.Listing);
            invalidModel.BuySellModel = model.BuySellModel;
            invalidModel.Material = model.Material;
            invalidModel.NegotiablePriceInd = model.NegotiablePriceInd;
            invalidModel.DeliveryAvailableInd = model.DeliveryAvailableInd;
            invalidModel.PickupAvailableInd = model.PickupAvailableInd;
            invalidModel.WarrantyInfo = model.WarrantyInfo;
            invalidModel.AdditionalDetails = model.AdditionalDetails;

            return View("~/Views/BuySellDetail/Edit.cshtml", invalidModel);
        }

        entity.Model = model.BuySellModel;
        entity.Material = model.Material;
        entity.NegotiablePriceInd = model.NegotiablePriceInd;
        entity.DeliveryAvailableInd = model.DeliveryAvailableInd;
        entity.PickupAvailableInd = model.PickupAvailableInd;
        entity.WarrantyInfo = model.WarrantyInfo;
        entity.AdditionalDetails = model.AdditionalDetails;
        entity.ModifiedBY = User.Identity?.Name;
        entity.ModifiedDate = CanHappy.Common.EasternTime.Now;

        await context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Delete/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (!TryGetCurrentUserGuid(out var currentUserId) && !User.IsInRole("Admin") && !User.IsInRole("Clerk"))
        {
            return Forbid();
        }

        var entity = await context.BuySellDetails
            .AsNoTracking()
            .Include(item => item.Listing)
            .FirstOrDefaultAsync(item => item.BuySellDetailGUID == id && !item.DeletedInd);

        if (entity is null || entity.Listing is null)
        {
            return NotFound();
        }

        if (!CanManageListing(entity.Listing, currentUserId))
        {
            return Forbid();
        }

        var model = ToEditViewModel(entity, entity.Listing);
        return View("~/Views/BuySellDetail/Delete.cshtml", model);
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

        var entity = await context.BuySellDetails
            .Include(item => item.Listing)
            .FirstOrDefaultAsync(item => item.BuySellDetailGUID == id && !item.DeletedInd);

        if (entity is null || entity.Listing is null)
        {
            return NotFound();
        }

        if (!CanManageListing(entity.Listing, currentUserId))
        {
            return Forbid();
        }

        context.BuySellDetails.Remove(entity);
        await context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("UpsertPhoto/{listingGuid:guid}")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpsertPhoto(Guid listingGuid, Guid? listingImageGuid, string? title, int? sorOrder, string? croppedImageData)
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
        listingImage.ThumbnailURL = imagePath;
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

    private BuySellDetailEditViewModel ToEditViewModel(BuySellDetail detail, Listing listing)
    {
        return new BuySellDetailEditViewModel
        {
            BuySellDetailGUID = detail.BuySellDetailGUID,
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
            BuySellModel = detail.Model,
            Material = detail.Material,
            NegotiablePriceInd = detail.NegotiablePriceInd,
            DeliveryAvailableInd = detail.DeliveryAvailableInd,
            PickupAvailableInd = detail.PickupAvailableInd,
            WarrantyInfo = detail.WarrantyInfo,
            AdditionalDetails = detail.AdditionalDetails
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
            .Where(listing => !listing.DeletedInd)
            .Where(listing => !context.BuySellDetails.Any(detail => detail.ListingGUID == listing.ListingGUID && !detail.DeletedInd));

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
        var relativePath = $"/ListingImages/{fileName}";
        var folderPath = Path.Combine(environment.WebRootPath, "ListingImages");

        Directory.CreateDirectory(folderPath);
        var filePath = Path.Combine(folderPath, fileName);
        await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);

        return relativePath;
    }
}

