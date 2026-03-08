using System.Security.Claims;
using CanHappy.Data;
using CanHappy.Models;
using CanHappy.Services.Email;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CanHappy.Controllers;

[Authorize]
[Route("RideRequest")]
public class RideRequestController(
    ApplicationDbContext context,
    UserManager<IdentityUser> userManager,
    IMessageNotificationEmailSender messageNotificationEmailSender,
    ILogger<RideRequestController> logger) : Controller
{
    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index()
    {
        if (!TryGetCurrentUserGuid(out var currentUserId))
        {
            return Forbid();
        }

        var items = await context.RideRequests
            .AsNoTracking()
            .Include(item => item.RideRequestStatus)
            .Include(item => item.CarPoolDetail)
            .ThenInclude(detail => detail!.Listing)
            .Where(item => !item.DeletedInd && item.RiderUserID == currentUserId)
            .OrderByDescending(item => item.CreatedDate)
            .ToListAsync();

        return View("~/Views/RideRequest/Index.cshtml", items);
    }

    [HttpGet("Create")]
    public async Task<IActionResult> Create(Guid? carPoolDetailGuid)
    {
        var model = new RideRequest
        {
            CarPoolDetailGUID = carPoolDetailGuid ?? Guid.Empty,
            RideRequestStatusId = 1,
            RiderUserID = TryGetCurrentUserGuid(out var currentUserId) ? currentUserId : Guid.Empty
        };

        await PopulateSelectionsAsync(model.CarPoolDetailGUID, model.RideRequestStatusId, carPoolDetailGuid.HasValue);
        return View("~/Views/RideRequest/Create.cshtml", model);
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RideRequest model)
    {
        if (model.CarPoolDetailGUID == Guid.Empty)
        {
            ModelState.AddModelError(nameof(RideRequest.CarPoolDetailGUID), "Please select a carpool detail.");
        }

        if (!TryGetCurrentUserGuid(out var currentUserId))
        {
            ModelState.AddModelError(string.Empty, "Unable to determine current user identity.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateSelectionsAsync(model.CarPoolDetailGUID, 1, model.CarPoolDetailGUID != Guid.Empty);
            return View("~/Views/RideRequest/Create.cshtml", model);
        }

        var carPoolDetail = await context.CarPoolDetails
            .AsNoTracking()
            .Include(item => item.Listing)
            .FirstOrDefaultAsync(item => item.CarPoolDetailGUID == model.CarPoolDetailGUID && !item.DeletedInd);

        if (carPoolDetail is null || carPoolDetail.Listing is null || carPoolDetail.Listing.DeletedInd)
        {
            ModelState.AddModelError(nameof(RideRequest.CarPoolDetailGUID), "Selected car pool detail is not available.");
            await PopulateSelectionsAsync(model.CarPoolDetailGUID, 1, model.CarPoolDetailGUID != Guid.Empty);
            return View("~/Views/RideRequest/Create.cshtml", model);
        }

        model.RideRequestGUID = Guid.NewGuid();
        model.RiderUserID = currentUserId;
        model.RideRequestStatusId = 1;
        model.DeletedInd = false;
        model.CreatedBy = User.Identity?.Name ?? "web-user";
        model.CreatedDate = CanHappy.Common.EasternTime.Now;
        model.ModifiedBy = null;
        model.ModifiedDate = null;

        context.RideRequests.Add(model);

        await AddRideRequestMessageNotificationAsync(model, carPoolDetail.Listing, currentUserId, isUpdate: false);
        await context.SaveChangesAsync();

        await SendRideRequestEmailNotificationAsync(model, carPoolDetail.Listing, currentUserId);

        TempData["MessageSuccess"] = "Ride request created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Edit/{id:guid}")]
    public async Task<IActionResult> Edit(Guid id)
    {
        if (!TryGetCurrentUserGuid(out var currentUserId))
        {
            return Forbid();
        }

        var model = await context.RideRequests
            .FirstOrDefaultAsync(item => item.RideRequestGUID == id && !item.DeletedInd);

        if (model is null)
        {
            return NotFound();
        }

        if (model.RiderUserID != currentUserId)
        {
            return Forbid();
        }

        await PopulateSelectionsAsync(model.CarPoolDetailGUID, model.RideRequestStatusId, false);
        return View("~/Views/RideRequest/Edit.cshtml", model);
    }

    [HttpPost("Edit/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, RideRequest input)
    {
        if (id != input.RideRequestGUID)
        {
            return NotFound();
        }

        var model = await context.RideRequests
            .FirstOrDefaultAsync(item => item.RideRequestGUID == id && !item.DeletedInd);

        if (model is null)
        {
            return NotFound();
        }

        if (input.CarPoolDetailGUID == Guid.Empty)
        {
            ModelState.AddModelError(nameof(RideRequest.CarPoolDetailGUID), "Please select a carpool detail.");
        }

        if (!TryGetCurrentUserGuid(out var currentUserId))
        {
            return Forbid();
        }

        if (model.RiderUserID != currentUserId)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            await PopulateSelectionsAsync(input.CarPoolDetailGUID, input.RideRequestStatusId, false);
            return View("~/Views/RideRequest/Edit.cshtml", input);
        }

        var carPoolDetail = await context.CarPoolDetails
            .AsNoTracking()
            .Include(item => item.Listing)
            .FirstOrDefaultAsync(item => item.CarPoolDetailGUID == input.CarPoolDetailGUID && !item.DeletedInd);

        if (carPoolDetail is null || carPoolDetail.Listing is null || carPoolDetail.Listing.DeletedInd)
        {
            ModelState.AddModelError(nameof(RideRequest.CarPoolDetailGUID), "Selected car pool detail is not available.");
            await PopulateSelectionsAsync(input.CarPoolDetailGUID, input.RideRequestStatusId, false);
            return View("~/Views/RideRequest/Edit.cshtml", input);
        }

        model.CarPoolDetailGUID = input.CarPoolDetailGUID;
        model.RideRequestStatusId = input.RideRequestStatusId;
        model.RequestMessage = input.RequestMessage;
        model.ModifiedBy = User.Identity?.Name ?? "web-user";
        model.ModifiedDate = CanHappy.Common.EasternTime.Now;

        await AddRideRequestMessageNotificationAsync(model, carPoolDetail.Listing, currentUserId, isUpdate: true);

        await context.SaveChangesAsync();

        await SendRideRequestEmailNotificationAsync(model, carPoolDetail.Listing, currentUserId);

        TempData["MessageSuccess"] = "Ride request updated.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateSelectionsAsync(Guid selectedCarPoolDetailGuid, int selectedStatusId, bool lockCarPoolDetail)
    {
        var carPoolDetails = await context.CarPoolDetails
            .AsNoTracking()
            .Include(item => item.Listing)
            .Where(item => !item.DeletedInd)
            .OrderByDescending(item => item.CreatedDate)
            .Select(item => new
            {
                item.CarPoolDetailGUID,
                Label = item.Listing != null
                    ? $"{item.Listing.Subject} ({item.CarPoolDetailGUID})"
                    : item.CarPoolDetailGUID.ToString()
            })
            .ToListAsync();

        var statuses = await context.RideRequestStatuses
            .AsNoTracking()
            .Where(item => !item.DeletedInd)
            .OrderBy(item => item.RideRequestStatusId)
            .ToListAsync();

        ViewData["CarPoolDetailGUID"] = new SelectList(carPoolDetails, "CarPoolDetailGUID", "Label", selectedCarPoolDetailGuid);
        ViewData["RideRequestStatusId"] = new SelectList(statuses, "RideRequestStatusId", "Name", selectedStatusId);
        ViewData["LockCarPoolDetail"] = lockCarPoolDetail;
    }

    private async Task AddRideRequestMessageNotificationAsync(RideRequest rideRequest, Listing listing, Guid senderUserId, bool isUpdate)
    {
        if (listing.UserId == Guid.Empty || listing.UserId == senderUserId)
        {
            return;
        }

        var actionText = isUpdate ? "updated" : "created";
        var statusName = await context.RideRequestStatuses
            .AsNoTracking()
            .Where(item => item.RideRequestStatusId == rideRequest.RideRequestStatusId)
            .Select(item => item.Name)
            .FirstOrDefaultAsync() ?? $"Status #{rideRequest.RideRequestStatusId}";

        var messageBody = string.IsNullOrWhiteSpace(rideRequest.RequestMessage)
            ? $"A ride request was {actionText} for your listing '{listing.Subject}'. Current status: {statusName}."
            : rideRequest.RequestMessage.Trim();

        var inboxMessage = new UserMessage
        {
            UserMessageGUID = Guid.NewGuid(),
            ListingGUID = listing.ListingGUID,
            SenderUserId = senderUserId,
            RecipientUserId = listing.UserId,
            Subject = $"Ride request {actionText}: {listing.Subject}",
            Body = messageBody,
            IsRead = false,
            CreatedDate = CanHappy.Common.EasternTime.Now
        };

        context.UserMessages.Add(inboxMessage);
    }

    private async Task SendRideRequestEmailNotificationAsync(RideRequest rideRequest, Listing listing, Guid senderUserId)
    {
        if (listing.UserId == Guid.Empty || listing.UserId == senderUserId)
        {
            return;
        }

        try
        {
            var senderUser = await userManager.FindByIdAsync(senderUserId.ToString());
            var ownerUser = await userManager.FindByIdAsync(listing.UserId.ToString());

            if (string.IsNullOrWhiteSpace(ownerUser?.Email))
            {
                return;
            }

            var listingUrl = Url.Action(
                "Index",
                "CarPoolDetailPage",
                new { listingGuid = listing.ListingGUID },
                Request.Scheme) ?? $"{Request.Scheme}://{Request.Host}/CarPoolDetail?listingGuid={listing.ListingGUID}";

            var ownerInboxUrl = Url.Action(
                "Inbox",
                "MessagePage",
                null,
                Request.Scheme) ?? $"{Request.Scheme}://{Request.Host}/Messages/Inbox";

            var senderEmail = senderUser?.Email;
            if (string.IsNullOrWhiteSpace(senderEmail))
            {
                senderEmail = User.Identity?.Name ?? "(sender email unavailable)";
            }

            await messageNotificationEmailSender.SendSellerMessageNotificationAsync(
                ownerUser.Email,
                ownerUser.UserName ?? ownerUser.Email,
                senderEmail,
                senderUser?.UserName ?? senderEmail,
                listing.Subject,
                "Ride request message",
                rideRequest.RequestMessage ?? string.Empty,
                listingUrl,
                ownerInboxUrl);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send ride request email notification for listing {ListingGuid}.", listing.ListingGUID);
        }
    }

    private bool TryGetCurrentUserGuid(out Guid userId)
    {
        userId = Guid.Empty;
        var userIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return !string.IsNullOrWhiteSpace(userIdText) && Guid.TryParse(userIdText, out userId);
    }
}
