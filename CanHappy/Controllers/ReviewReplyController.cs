using System.Security.Claims;
using CanHappy.Data;
using CanHappy.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CanHappy.Controllers;

[Route("ReviewReply")]
public class ReviewReplyController(ApplicationDbContext context) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(Guid? listingReviewGuid)
    {
        var query = context.ReviewReplies
            .AsNoTracking()
            .Include(item => item.ListingReview)
            .ThenInclude(review => review!.Listing)
            .Where(item => !item.DeletedInd)
            .AsQueryable();

        if (listingReviewGuid.HasValue)
        {
            query = query.Where(item => item.ListingReviewGUID == listingReviewGuid.Value);
            ViewData["ListingReviewGUID"] = listingReviewGuid.Value;
        }

        var replies = await query
            .OrderByDescending(item => item.CreatedDate)
            .ToListAsync();

        ViewData["CanManageRole"] = User.IsInRole("Admin") || User.IsInRole("Clerk");
        ViewData["CurrentUserId"] = GetCurrentUserIdText();

        return View("~/Views/ReviewReply/Index.cshtml", replies);
    }

    [HttpGet("Create")]
    [Authorize]
    public async Task<IActionResult> Create(Guid? listingReviewGuid)
    {
        await PopulateReviewSelectListAsync(listingReviewGuid);
        ViewData["CanManageStatus"] = User.IsInRole("Admin") || User.IsInRole("Clerk");
        return View("~/Views/ReviewReply/Create.cshtml", new ReviewReply
        {
            ListingReviewGUID = listingReviewGuid ?? Guid.Empty,
            Status = ReviewWorkflowStatus.Submitted
        });
    }

    [HttpPost("Create")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ListingReviewGUID,ReplyMessage,Status,OfficialReplyInd")] ReviewReply model)
    {
        var currentUserIdText = GetCurrentUserIdText();
        if (string.IsNullOrWhiteSpace(currentUserIdText))
        {
            return Forbid();
        }

        if (!await ListingReviewExistsAsync(model.ListingReviewGUID))
        {
            ModelState.AddModelError(nameof(model.ListingReviewGUID), "Listing review not found.");
        }

        model.Status = ReviewWorkflowStatus.Submitted;

        if (!ModelState.IsValid)
        {
            await PopulateReviewSelectListAsync(model.ListingReviewGUID);
            ViewData["CanManageStatus"] = User.IsInRole("Admin") || User.IsInRole("Clerk");
            return View("~/Views/ReviewReply/Create.cshtml", model);
        }

        model.ReviewReplyGUID = Guid.NewGuid();
        model.CreatedBy = currentUserIdText;
        model.ModifiedBy = null;
        model.CreatedDate = CanHappy.Common.EasternTime.Now;
        model.ModifiedDate = null;
        model.DeletedInd = false;

        context.ReviewReplies.Add(model);
        await context.SaveChangesAsync();

        var listingGuid = await context.ListingReviews
            .Where(item => item.ListingReviewGUID == model.ListingReviewGUID)
            .Select(item => item.ListingGUID)
            .FirstOrDefaultAsync();

        return RedirectToAction("Index", "ListingReview", new { listingGuid });
    }

    [HttpGet("Edit/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Edit(Guid id)
    {
        var reply = await context.ReviewReplies
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.ReviewReplyGUID == id && !item.DeletedInd);

        if (reply is null)
        {
            return NotFound();
        }

        if (!CanModifyReply(reply))
        {
            return Forbid();
        }

        await PopulateReviewSelectListAsync(reply.ListingReviewGUID);
        ViewData["CanManageStatus"] = User.IsInRole("Admin") || User.IsInRole("Clerk");
        ViewData["CanManageDeletedInd"] = User.IsInRole("Admin") || User.IsInRole("Clerk");
        return View("~/Views/ReviewReply/Edit.cshtml", reply);
    }

    [HttpPost("Edit/{id:guid}")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, [Bind("ReviewReplyGUID,ListingReviewGUID,ReplyMessage,Status,OfficialReplyInd,DeletedInd")] ReviewReply model)
    {
        if (id != model.ReviewReplyGUID)
        {
            return NotFound();
        }

        var reply = await context.ReviewReplies
            .FirstOrDefaultAsync(item => item.ReviewReplyGUID == id && !item.DeletedInd);

        if (reply is null)
        {
            return NotFound();
        }

        if (!CanModifyReply(reply))
        {
            return Forbid();
        }

        var canManageStatus = User.IsInRole("Admin") || User.IsInRole("Clerk");
        model.Status = canManageStatus ? NormalizeStatus(model.Status) : reply.Status;
        model.DeletedInd = canManageStatus ? model.DeletedInd : reply.DeletedInd;

        if (!ModelState.IsValid)
        {
            await PopulateReviewSelectListAsync(model.ListingReviewGUID);
            ViewData["CanManageStatus"] = canManageStatus;
            ViewData["CanManageDeletedInd"] = canManageStatus;
            return View("~/Views/ReviewReply/Edit.cshtml", model);
        }

        reply.ReplyMessage = model.ReplyMessage;
        reply.Status = model.Status;
        reply.OfficialReplyInd = model.OfficialReplyInd;
        reply.DeletedInd = model.DeletedInd;
        reply.ModifiedBy = GetCurrentUserIdText();
        reply.ModifiedDate = CanHappy.Common.EasternTime.Now;

        await context.SaveChangesAsync();

        var listingGuid = await context.ListingReviews
            .Where(item => item.ListingReviewGUID == model.ListingReviewGUID)
            .Select(item => item.ListingGUID)
            .FirstOrDefaultAsync();

        return RedirectToAction("Index", "ListingReview", new { listingGuid });
    }

    [HttpGet("Delete/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id)
    {
        var reply = await context.ReviewReplies
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.ReviewReplyGUID == id && !item.DeletedInd);

        if (reply is null)
        {
            return NotFound();
        }

        if (!CanModifyReply(reply))
        {
            return Forbid();
        }

        return View("~/Views/ReviewReply/Delete.cshtml", reply);
    }

    [HttpPost("Delete/{id:guid}"), ActionName("Delete")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var reply = await context.ReviewReplies
            .FirstOrDefaultAsync(item => item.ReviewReplyGUID == id && !item.DeletedInd);

        if (reply is null)
        {
            return NotFound();
        }

        if (!CanModifyReply(reply))
        {
            return Forbid();
        }

        var listingReviewGuid = reply.ListingReviewGUID;
        reply.DeletedInd = true;
        reply.ModifiedBy = GetCurrentUserIdText();
        reply.ModifiedDate = CanHappy.Common.EasternTime.Now;

        await context.SaveChangesAsync();

        var listingGuid = await context.ListingReviews
            .Where(item => item.ListingReviewGUID == listingReviewGuid)
            .Select(item => item.ListingGUID)
            .FirstOrDefaultAsync();

        return RedirectToAction("Index", "ListingReview", new { listingGuid });
    }

    private async Task PopulateReviewSelectListAsync(Guid? selectedListingReviewGuid)
    {
        var reviews = await context.ListingReviews
            .AsNoTracking()
            .Include(item => item.Listing)
            .Where(item => !item.DeletedInd)
            .OrderByDescending(item => item.CreatedDate)
            .Select(item => new
            {
                item.ListingReviewGUID,
                DisplayText = $"{item.Listing!.Subject} - {item.ReviewTitle ?? "Review"}"
            })
            .ToListAsync();

        ViewData["ListingReviewGUID"] = new SelectList(reviews, "ListingReviewGUID", "DisplayText", selectedListingReviewGuid);
        ViewData["Statuses"] = new SelectList(ReviewWorkflowStatus.All);
    }

    private async Task<bool> ListingReviewExistsAsync(Guid listingReviewGuid)
    {
        return await context.ListingReviews.AnyAsync(item => item.ListingReviewGUID == listingReviewGuid && !item.DeletedInd);
    }

    private bool CanModifyReply(ReviewReply reply)
    {
        if (User.IsInRole("Admin") || User.IsInRole("Clerk"))
        {
            return true;
        }

        var currentUserIdText = GetCurrentUserIdText();
        return !string.IsNullOrWhiteSpace(currentUserIdText)
            && !string.IsNullOrWhiteSpace(reply.CreatedBy)
            && string.Equals(currentUserIdText, reply.CreatedBy, StringComparison.OrdinalIgnoreCase);
    }

    private string? GetCurrentUserIdText()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    private static string NormalizeStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return ReviewWorkflowStatus.Submitted;
        }

        var matched = ReviewWorkflowStatus.All.FirstOrDefault(item => string.Equals(item, status, StringComparison.OrdinalIgnoreCase));
        return matched ?? ReviewWorkflowStatus.Submitted;
    }
}
