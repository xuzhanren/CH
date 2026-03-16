using CanHappy.Data;
using CanHappy.Models;
using CanHappy.Models.Messaging;
using CanHappy.Services.Email;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CanHappy.Controllers;

[Authorize]
[Route("Messages")]
public class MessagePageController(
    ApplicationDbContext context,
    UserManager<IdentityUser> userManager,
    IMessageNotificationEmailSender messageNotificationEmailSender,
    ILogger<MessagePageController> logger) : Controller
{
    [HttpGet("")]
    [HttpGet("Inbox")]
    public async Task<IActionResult> Inbox()
    {
        if (!TryGetCurrentUserGuid(out var currentUserId))
        {
            return Forbid();
        }

        var messages = await context.UserMessages
            .AsNoTracking()
            .Include(message => message.Listing)
            .Where(message => message.RecipientUserId == currentUserId)
            .OrderByDescending(message => message.CreatedDate)
            .ToListAsync();

        var senderIds = messages.Select(message => message.SenderUserId.ToString()).Distinct().ToList();
        var senders = await userManager.Users
            .AsNoTracking()
            .Where(user => senderIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, user => user.UserName ?? user.Email ?? user.Id);

        var model = messages.Select(message => new MessageListItemViewModel
        {
            UserMessageGUID = message.UserMessageGUID,
            ListingGUID = message.ListingGUID,
            ListingSubject = message.Listing?.Subject ?? "Support",
            Subject = message.Subject,
            BodyPreview = BuildPreview(message.Body),
            CreatedDate = message.CreatedDate,
            IsRead = message.IsRead,
            CounterpartyName = senders.TryGetValue(message.SenderUserId.ToString(), out var senderName)
                ? senderName
                : message.SenderUserId.ToString()
        }).ToList();

        return View("~/Views/Message/Inbox.cshtml", model);
    }

    [HttpGet("Sent")]
    public async Task<IActionResult> Sent()
    {
        if (!TryGetCurrentUserGuid(out var currentUserId))
        {
            return Forbid();
        }

        var messages = await context.UserMessages
            .AsNoTracking()
            .Include(message => message.Listing)
            .Where(message => message.SenderUserId == currentUserId)
            .OrderByDescending(message => message.CreatedDate)
            .ToListAsync();

        var recipientIds = messages.Select(message => message.RecipientUserId.ToString()).Distinct().ToList();
        var recipients = await userManager.Users
            .AsNoTracking()
            .Where(user => recipientIds.Contains(user.Id))
            .ToDictionaryAsync(user => user.Id, user => user.UserName ?? user.Email ?? user.Id);

        var model = messages.Select(message => new MessageListItemViewModel
        {
            UserMessageGUID = message.UserMessageGUID,
            ListingGUID = message.ListingGUID,
            ListingSubject = message.Listing?.Subject ?? "Support",
            Subject = message.Subject,
            BodyPreview = BuildPreview(message.Body),
            CreatedDate = message.CreatedDate,
            IsRead = message.IsRead,
            CounterpartyName = recipients.TryGetValue(message.RecipientUserId.ToString(), out var recipientName)
                ? recipientName
                : message.RecipientUserId.ToString()
        }).ToList();

        return View("~/Views/Message/Sent.cshtml", model);
    }

    [HttpGet("Details/{id:guid}")]
    public async Task<IActionResult> Details(Guid id)
    {
        if (!TryGetCurrentUserGuid(out var currentUserId))
        {
            return Forbid();
        }

        var message = await context.UserMessages
            .Include(item => item.Listing)
            .FirstOrDefaultAsync(item => item.UserMessageGUID == id);

        if (message is null)
        {
            return NotFound();
        }

        var isRecipient = message.RecipientUserId == currentUserId;
        var isSender = message.SenderUserId == currentUserId;
        if (!isRecipient && !isSender)
        {
            return Forbid();
        }

        if (isRecipient && !message.IsRead)
        {
            message.IsRead = true;
            await context.SaveChangesAsync();
        }

        var sender = await userManager.FindByIdAsync(message.SenderUserId.ToString());
        var recipient = await userManager.FindByIdAsync(message.RecipientUserId.ToString());

        var model = new MessageDetailsViewModel
        {
            UserMessageGUID = message.UserMessageGUID,
            ListingGUID = message.ListingGUID,
            ListingSubject = message.Listing?.Subject ?? "Support",
            Subject = message.Subject,
            Body = message.Body,
            CreatedDate = message.CreatedDate,
            IsRead = message.IsRead,
            SenderName = sender?.UserName ?? sender?.Email ?? message.SenderUserId.ToString(),
            RecipientName = recipient?.UserName ?? recipient?.Email ?? message.RecipientUserId.ToString(),
            IsInboxMessage = isRecipient
        };

        return View("~/Views/Message/Details.cshtml", model);
    }

    [HttpPost("SendToSeller/{listingId:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendToSeller(Guid listingId, string? subject, string? body, string? returnUrl)
    {
        if (!TryGetCurrentUserGuid(out var currentUserId))
        {
            return Forbid();
        }

        var listing = await context.Listings
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.ListingGUID == listingId && !item.DeletedInd);

        if (listing is null)
        {
            return NotFound();
        }

        if (listing.UserId == Guid.Empty)
        {
            TempData["MessageError"] = "This seller cannot receive messages yet.";
            return RedirectAfterSend(returnUrl, listingId);
        }

        if (listing.UserId == currentUserId)
        {
            TempData["MessageError"] = "You cannot send a message to yourself.";
            return RedirectAfterSend(returnUrl, listingId);
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            TempData["MessageError"] = "Message body is required.";
            return RedirectAfterSend(returnUrl, listingId);
        }

        var effectiveSubject = string.IsNullOrWhiteSpace(subject)
            ? $"Message about: {listing.Subject}"
            : subject.Trim();

        var message = new UserMessage
        {
            UserMessageGUID = Guid.NewGuid(),
            ListingGUID = listing.ListingGUID,
            SenderUserId = currentUserId,
            RecipientUserId = listing.UserId,
            Subject = effectiveSubject,
            Body = body.Trim(),
            IsRead = false,
            CreatedDate = CanHappy.Common.EasternTime.Now
        };

        context.UserMessages.Add(message);
        await context.SaveChangesAsync();

        try
        {
            var senderUser = await userManager.FindByIdAsync(currentUserId.ToString());
            var sellerUser = await userManager.FindByIdAsync(listing.UserId.ToString());

            if (!string.IsNullOrWhiteSpace(sellerUser?.Email))
            {
                var listingUrl = Url.Action(
                    "Details",
                    "ListingPage",
                    new { id = listing.ListingGUID },
                    Request.Scheme) ?? $"{Request.Scheme}://{Request.Host}/Listing/Details/{listing.ListingGUID}";

                var sellerInboxUrl = Url.Action(
                    nameof(Inbox),
                    "MessagePage",
                    null,
                    Request.Scheme) ?? $"{Request.Scheme}://{Request.Host}/Messages/Inbox";

                var senderEmail = senderUser?.Email;
                if (string.IsNullOrWhiteSpace(senderEmail))
                {
                    senderEmail = User.Identity?.Name ?? "(sender email unavailable)";
                }

                await messageNotificationEmailSender.SendSellerMessageNotificationAsync(
                    sellerUser.Email,
                    sellerUser.UserName ?? sellerUser.Email,
                    senderEmail,
                    senderUser?.UserName ?? senderEmail,
                    listing.Subject,
                    effectiveSubject,
                    message.Body,
                    listingUrl,
                    sellerInboxUrl);
            }
            else
            {
                logger.LogInformation("Seller email is not available for user {SellerUserId}; skipped message notification email.", listing.UserId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send seller notification email for listing {ListingId}.", listing.ListingGUID);
        }

        TempData["MessageSuccess"] = "Message sent to seller.";
        return RedirectAfterSend(returnUrl, listingId);
    }

    [HttpPost("Reply/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(Guid id, string? subject, string? body)
    {
        if (!TryGetCurrentUserGuid(out var currentUserId))
        {
            return Forbid();
        }

        var originalMessage = await context.UserMessages
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.UserMessageGUID == id);

        if (originalMessage is null)
        {
            return NotFound();
        }

        if (originalMessage.RecipientUserId != currentUserId)
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            TempData["MessageError"] = "Reply body is required.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var effectiveSubject = string.IsNullOrWhiteSpace(subject)
            ? (originalMessage.Subject.StartsWith("Re:", StringComparison.OrdinalIgnoreCase)
                ? originalMessage.Subject
                : $"Re: {originalMessage.Subject}")
            : subject.Trim();

        var reply = new UserMessage
        {
            UserMessageGUID = Guid.NewGuid(),
            ListingGUID = originalMessage.ListingGUID,
            SenderUserId = currentUserId,
            RecipientUserId = originalMessage.SenderUserId,
            Subject = effectiveSubject,
            Body = body.Trim(),
            IsRead = false,
            CreatedDate = CanHappy.Common.EasternTime.Now
        };

        context.UserMessages.Add(reply);
        await context.SaveChangesAsync();

        TempData["MessageSuccess"] = "Reply sent.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private bool TryGetCurrentUserGuid(out Guid userId)
    {
        userId = Guid.Empty;
        var userIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return !string.IsNullOrWhiteSpace(userIdText) && Guid.TryParse(userIdText, out userId);
    }

    private static string BuildPreview(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return string.Empty;
        }

        var trimmed = body.Trim();
        return trimmed.Length <= 120 ? trimmed : $"{trimmed[..120]}...";
    }

    private IActionResult RedirectAfterSend(string? returnUrl, Guid listingId)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Details", "ListingPage", new { id = listingId });
    }
}

