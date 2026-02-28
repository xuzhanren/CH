using CanHappy.Data;
using CanHappy.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CanHappy.Controllers;

[Authorize]
[Route("FavoriteListing")]
public class FavoriteListingController(ApplicationDbContext context) : Controller
{
    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index()
    {
        if (!TryGetCurrentUserGuid(out var currentUserId))
        {
            return Forbid();
        }

        var model = new FavoriteListingIndexPageViewModel
        {
            Items = await context.FavoriteListings
                .AsNoTracking()
                .Where(item => item.UserId == currentUserId && !item.DeletedInd)
                .OrderByDescending(item => item.CreatedDate)
                .ThenBy(item => item.SortOrder)
                .Select(item => new FavoriteListingListItemViewModel
                {
                    FavoriteListingGUID = item.FavoriteListingGUID,
                    ListingGUID = item.ListingGUID,
                    ListingURL = item.ListingURL,
                    ListingSubject = item.ListingSubject,
                    SortOrder = item.SortOrder,
                    CreatedDate = item.CreatedDate
                })
                .ToListAsync()
        };

        return View("~/Views/FavoriteListing/Index.cshtml", model);
    }

    [HttpPost("UpdateSortOrder/{favoriteListingGuid:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateSortOrder(Guid favoriteListingGuid, int sortOrder)
    {
        if (!TryGetCurrentUserGuid(out var currentUserId))
        {
            return Forbid();
        }

        var favorite = await context.FavoriteListings
            .FirstOrDefaultAsync(item => item.FavoriteListingGUID == favoriteListingGuid && item.UserId == currentUserId && !item.DeletedInd);
        if (favorite is null)
        {
            TempData["MessageError"] = "Favorite listing not found.";
            return RedirectToAction(nameof(Index));
        }

        favorite.SortOrder = sortOrder;
        favorite.ModifiedBy = User.Identity?.Name ?? string.Empty;
        favorite.ModifiedDate = CanHappy.Common.EasternTime.Now;

        await context.SaveChangesAsync();
        TempData["MessageSuccess"] = "Favorite sort order updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("Delete/{favoriteListingGuid:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid favoriteListingGuid)
    {
        if (!TryGetCurrentUserGuid(out var currentUserId))
        {
            return Forbid();
        }

        var favorite = await context.FavoriteListings
            .FirstOrDefaultAsync(item => item.FavoriteListingGUID == favoriteListingGuid && item.UserId == currentUserId && !item.DeletedInd);
        if (favorite is null)
        {
            TempData["MessageError"] = "Favorite listing not found.";
            return RedirectToAction(nameof(Index));
        }

        favorite.DeletedInd = true;
        favorite.ModifiedBy = User.Identity?.Name ?? string.Empty;
        favorite.ModifiedDate = CanHappy.Common.EasternTime.Now;

        await context.SaveChangesAsync();
        TempData["MessageSuccess"] = "Favorite removed.";
        return RedirectToAction(nameof(Index));
    }

    private bool TryGetCurrentUserGuid(out Guid userId)
    {
        userId = Guid.Empty;
        var userIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return !string.IsNullOrWhiteSpace(userIdText) && Guid.TryParse(userIdText, out userId);
    }
}
