using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using CanHappy.Data;
using CanHappy.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace CanHappy.Controllers;

[Route("ListingReview")]
public class ListingReviewController(ApplicationDbContext context, IWebHostEnvironment environment) : Controller
{
    private const int MaxReviewFullImageBytes = 1 * 1024 * 1024;
    private const int MaxReviewThumbnailBytes = 50 * 1024;

    [HttpGet("")]
    public async Task<IActionResult> Index(Guid? listingGuid)
    {
        var query = context.ListingReviews
            .AsNoTracking()
            .Include(item => item.Listing)
            .Include(item => item.Images.Where(image => !image.DeletedInd))
            .Include(item => item.Replies.Where(reply => !reply.DeletedInd))
            .Where(item => !item.DeletedInd)
            .AsQueryable();

        if (listingGuid.HasValue)
        {
            query = query.Where(item => item.ListingGUID == listingGuid.Value);
            ViewData["ListingGUID"] = listingGuid.Value;
        }

        var reviews = await query
            .OrderByDescending(item => item.CreatedDate)
            .ToListAsync();

        ViewData["ListingGuidFilter"] = listingGuid;
        ViewData["CanManageRole"] = User.IsInRole("Admin") || User.IsInRole("Clerk");
        ViewData["CurrentUserId"] = GetCurrentUserIdText();

        return View("~/Views/ListingReview/Index.cshtml", reviews);
    }

    [HttpGet("Create")]
    [Authorize]
    public async Task<IActionResult> Create(Guid? listingGuid)
    {
        await PopulateListingSelectListAsync(listingGuid);
        ViewData["CanManageStatus"] = User.IsInRole("Admin") || User.IsInRole("Clerk");
        ViewData["CanManageDeletedInd"] = User.IsInRole("Admin") || User.IsInRole("Clerk");

        return View("~/Views/ListingReview/Create.cshtml", new ListingReview
        {
            ListingGUID = listingGuid ?? Guid.Empty,
            Rating = 3.5f,
            Status = ReviewWorkflowStatus.Submitted
        });
    }

    [HttpPost("Create")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ListingGUID,Rating,ReviewTitle,ReviewMessage,Status,VerifiedPurchaseInd")] ListingReview model, string? reviewPhotosJson)
    {
        var currentUserIdText = GetCurrentUserIdText();
        if (string.IsNullOrWhiteSpace(currentUserIdText))
        {
            return Forbid();
        }

        if (!await ListingExistsAsync(model.ListingGUID))
        {
            ModelState.AddModelError(nameof(model.ListingGUID), "Listing not found.");
        }

        model.Rating = NormalizeRating(model.Rating);
        model.Status = ReviewWorkflowStatus.Submitted;

        if (!ModelState.IsValid)
        {
            await PopulateListingSelectListAsync(model.ListingGUID);
            ViewData["CanManageStatus"] = User.IsInRole("Admin") || User.IsInRole("Clerk");
            ViewData["CanManageDeletedInd"] = User.IsInRole("Admin") || User.IsInRole("Clerk");
            return View("~/Views/ListingReview/Create.cshtml", model);
        }

        model.ListingReviewGUID = Guid.NewGuid();
        model.CreatedBy = currentUserIdText;
        model.ModifiedBy = null;
        model.CreatedDate = CanHappy.Common.EasternTime.Now;
        model.ModifiedDate = null;
        model.DeletedInd = false;

        context.ListingReviews.Add(model);
        await context.SaveChangesAsync();

        await SaveReviewImagesAsync(model.ListingReviewGUID, currentUserIdText, reviewPhotosJson);
        await UpdateListingAverageRatingAsync(model.ListingGUID);

        return RedirectToAction(nameof(Index), new { listingGuid = model.ListingGUID });
    }

    [HttpGet("Edit/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Edit(Guid id)
    {
        var review = await context.ListingReviews
            .Include(item => item.Images.Where(image => !image.DeletedInd))
            .FirstOrDefaultAsync(item => item.ListingReviewGUID == id && !item.DeletedInd);

        if (review is null)
        {
            return NotFound();
        }

        if (!CanModifyReview(review))
        {
            return Forbid();
        }

        await PopulateListingSelectListAsync(review.ListingGUID);
        ViewData["CanManageStatus"] = User.IsInRole("Admin") || User.IsInRole("Clerk");
        ViewData["CanManageDeletedInd"] = User.IsInRole("Admin") || User.IsInRole("Clerk");
        return View("~/Views/ListingReview/Edit.cshtml", review);
    }

    [HttpPost("Edit/{id:guid}")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, [Bind("ListingReviewGUID,ListingGUID,Rating,ReviewTitle,ReviewMessage,Status,VerifiedPurchaseInd,DeletedInd")] ListingReview model, string? reviewPhotosJson, List<Guid>? deleteImageGuids)
    {
        if (id != model.ListingReviewGUID)
        {
            return NotFound();
        }

        var review = await context.ListingReviews
            .Include(item => item.Images.Where(image => !image.DeletedInd))
            .FirstOrDefaultAsync(item => item.ListingReviewGUID == id && !item.DeletedInd);

        if (review is null)
        {
            return NotFound();
        }

        if (!CanModifyReview(review))
        {
            return Forbid();
        }

        model.Rating = NormalizeRating(model.Rating);
        var canManageStatus = User.IsInRole("Admin") || User.IsInRole("Clerk");
        model.Status = canManageStatus ? NormalizeStatus(model.Status) : review.Status;
        model.DeletedInd = canManageStatus ? model.DeletedInd : review.DeletedInd;

        if (!ModelState.IsValid)
        {
            await PopulateListingSelectListAsync(model.ListingGUID);
            ViewData["CanManageStatus"] = canManageStatus;
            ViewData["CanManageDeletedInd"] = canManageStatus;
            review.Rating = model.Rating;
            review.ReviewTitle = model.ReviewTitle;
            review.ReviewMessage = model.ReviewMessage;
            review.Status = model.Status;
            review.VerifiedPurchaseInd = model.VerifiedPurchaseInd;
            review.DeletedInd = model.DeletedInd;
            return View("~/Views/ListingReview/Edit.cshtml", review);
        }

        review.Rating = model.Rating;
        review.ReviewTitle = model.ReviewTitle;
        review.ReviewMessage = model.ReviewMessage;
        review.Status = model.Status;
        review.VerifiedPurchaseInd = model.VerifiedPurchaseInd;
        review.DeletedInd = model.DeletedInd;
        review.ModifiedBy = GetCurrentUserIdText();
        review.ModifiedDate = CanHappy.Common.EasternTime.Now;

        if (deleteImageGuids is not null && deleteImageGuids.Count > 0)
        {
            var targets = review.Images.Where(image => deleteImageGuids.Contains(image.ListingReviewImageGUID)).ToList();
            foreach (var image in targets)
            {
                image.DeletedInd = true;
            }
        }

        await SaveReviewImagesAsync(review.ListingReviewGUID, GetCurrentUserIdText(), reviewPhotosJson);
        await context.SaveChangesAsync();
        await UpdateListingAverageRatingAsync(review.ListingGUID);

        return RedirectToAction(nameof(Index), new { listingGuid = review.ListingGUID });
    }

    [HttpGet("Delete/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id)
    {
        var review = await context.ListingReviews
            .AsNoTracking()
            .Include(item => item.Listing)
            .FirstOrDefaultAsync(item => item.ListingReviewGUID == id && !item.DeletedInd);

        if (review is null)
        {
            return NotFound();
        }

        if (!CanModifyReview(review))
        {
            return Forbid();
        }

        return View("~/Views/ListingReview/Delete.cshtml", review);
    }

    [HttpPost("Delete/{id:guid}"), ActionName("Delete")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var review = await context.ListingReviews.FirstOrDefaultAsync(item => item.ListingReviewGUID == id && !item.DeletedInd);
        if (review is null)
        {
            return NotFound();
        }

        if (!CanModifyReview(review))
        {
            return Forbid();
        }

        review.DeletedInd = true;
        review.ModifiedBy = GetCurrentUserIdText();
        review.ModifiedDate = CanHappy.Common.EasternTime.Now;

        await context.SaveChangesAsync();
        await UpdateListingAverageRatingAsync(review.ListingGUID);

        return RedirectToAction(nameof(Index), new { listingGuid = review.ListingGUID });
    }

    private async Task<bool> ListingExistsAsync(Guid listingGuid)
    {
        return await context.Listings.AnyAsync(item => item.ListingGUID == listingGuid && !item.DeletedInd);
    }

    private async Task PopulateListingSelectListAsync(Guid? selectedListingGuid)
    {
        var listings = await context.Listings
            .AsNoTracking()
            .Where(item => !item.DeletedInd)
            .OrderByDescending(item => item.CreatedDate)
            .Select(item => new
            {
                item.ListingGUID,
                DisplayText = $"{item.Subject} ({item.ListingGUID})"
            })
            .ToListAsync();

        ViewData["ListingGUID"] = new SelectList(listings, "ListingGUID", "DisplayText", selectedListingGuid);
        ViewData["Statuses"] = new SelectList(ReviewWorkflowStatus.All);
    }

    private bool CanModifyReview(ListingReview review)
    {
        if (User.IsInRole("Admin") || User.IsInRole("Clerk"))
        {
            return true;
        }

        var currentUserIdText = GetCurrentUserIdText();
        return !string.IsNullOrWhiteSpace(currentUserIdText)
            && !string.IsNullOrWhiteSpace(review.CreatedBy)
            && string.Equals(currentUserIdText, review.CreatedBy, StringComparison.OrdinalIgnoreCase);
    }

    private string? GetCurrentUserIdText()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    private static float NormalizeRating(float value)
    {
        if (!float.IsFinite(value))
        {
            return 3.5f;
        }

        var clamped = Math.Clamp(value, 0.0f, 5.0f);
        return (float)Math.Round(clamped, 1, MidpointRounding.AwayFromZero);
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

    private async Task SaveReviewImagesAsync(Guid listingReviewGuid, string? currentUserIdText, string? reviewPhotosJson)
    {
        if (string.IsNullOrWhiteSpace(reviewPhotosJson))
        {
            return;
        }

        List<ReviewPhotoPayload>? payloads;
        try
        {
            payloads = JsonSerializer.Deserialize<List<ReviewPhotoPayload>>(reviewPhotosJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException)
        {
            payloads = null;
        }

        if (payloads is null || payloads.Count == 0)
        {
            return;
        }

        var activeCount = await context.ListingReviewImages
            .CountAsync(item => item.ListingReviewGUID == listingReviewGuid && !item.DeletedInd);

        foreach (var payload in payloads.Take(Math.Max(0, 4 - activeCount)))
        {
            if (string.IsNullOrWhiteSpace(payload.FullImageDataUrl))
            {
                continue;
            }

            var fullImage = await BuildSizedImageFromDataUrlAsync(payload.FullImageDataUrl, MaxReviewFullImageBytes, 640);
            if (fullImage is null)
            {
                continue;
            }

            var imageUrl = await SaveImageBytesAsync(fullImage.Value.ImageBytes, fullImage.Value.Extension);
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                continue;
            }

            string thumbnailUrl;
            if (fullImage.Value.ImageBytes.Length <= MaxReviewThumbnailBytes)
            {
                thumbnailUrl = imageUrl;
            }
            else
            {
                var thumbnail = await BuildSizedImageFromDataUrlAsync(payload.FullImageDataUrl, MaxReviewThumbnailBytes, 160);
                var thumbnailPath = thumbnail is null
                    ? null
                    : await SaveImageBytesAsync(thumbnail.Value.ImageBytes, thumbnail.Value.Extension);
                thumbnailUrl = string.IsNullOrWhiteSpace(thumbnailPath) ? imageUrl : thumbnailPath;
            }

            context.ListingReviewImages.Add(new ListingReviewImage
            {
                ListingReviewImageGUID = Guid.NewGuid(),
                ListingReviewGUID = listingReviewGuid,
                Title = payload.Title,
                SortOrder = payload.SortOrder,
                ThumbnailURL = thumbnailUrl,
                ImageURL = imageUrl,
                DeletedInd = false,
                CreatedBy = currentUserIdText,
                CreatedDate = CanHappy.Common.EasternTime.Now
            });

            activeCount += 1;
            if (activeCount >= 4)
            {
                break;
            }
        }
    }

    private async Task UpdateListingAverageRatingAsync(Guid listingGuid)
    {
        var listing = await context.Listings.FirstOrDefaultAsync(item => item.ListingGUID == listingGuid && !item.DeletedInd);
        if (listing is null)
        {
            return;
        }

        var ratings = await context.ListingReviews
            .AsNoTracking()
            .Where(item => item.ListingGUID == listingGuid && !item.DeletedInd)
            .Select(item => item.Rating)
            .ToListAsync();

        if (ratings.Count == 0)
        {
            listing.Rating = "3.5";
        }
        else
        {
            var validRatings = ratings.Where(item => float.IsFinite(item) && item >= 0f && item <= 5f).ToList();
            if (validRatings.Count == 0)
            {
                listing.Rating = "3.5";
            }
            else
            {
                var average = validRatings.Average();
                listing.Rating = Math.Round(average, 1, MidpointRounding.AwayFromZero).ToString("0.0");
            }
        }

        await context.SaveChangesAsync();
    }

    private async Task<(byte[] ImageBytes, string Extension)?> BuildSizedImageFromDataUrlAsync(string dataUrl, int maxBytes, int minDimension)
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

        if (imageBytes.Length <= maxBytes)
        {
            var passthroughExtension = metadata.Contains("image/png", StringComparison.OrdinalIgnoreCase) ? ".png" : ".jpg";
            return (imageBytes, passthroughExtension);
        }

        using var image = Image.Load(imageBytes);

        var width = image.Width;
        var height = image.Height;
        var quality = 85;
        var guard = 0;

        while (guard < 24)
        {
            guard += 1;

            using var stream = new MemoryStream();
            await image.SaveAsJpegAsync(stream, new JpegEncoder { Quality = quality });
            var output = stream.ToArray();
            if (output.Length <= maxBytes)
            {
                return (output, ".jpg");
            }

            if (quality > 45)
            {
                quality -= 5;
            }
            else
            {
                var nextWidth = Math.Max(minDimension, (int)Math.Round(width * 0.9));
                var nextHeight = Math.Max(minDimension, (int)Math.Round(height * 0.9));
                if (nextWidth == width && nextHeight == height)
                {
                    break;
                }

                width = nextWidth;
                height = nextHeight;
                image.Mutate(operation => operation.Resize(width, height));
            }
        }

        using var fallbackStream = new MemoryStream();
        await image.SaveAsJpegAsync(fallbackStream, new JpegEncoder { Quality = 40 });
        return (fallbackStream.ToArray(), ".jpg");
    }

    private async Task<string?> SaveImageBytesAsync(byte[] imageBytes, string extension)
    {
        var resolvedExtension = string.IsNullOrWhiteSpace(extension) ? ".jpg" : extension;
        var fileName = $"{Guid.NewGuid():N}{resolvedExtension}";
        var relativePath = $"/ListingReviewImages/{fileName}";
        var folderPath = Path.Combine(environment.WebRootPath, "ListingReviewImages");

        Directory.CreateDirectory(folderPath);

        var filePath = Path.Combine(folderPath, fileName);
        await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);

        return relativePath;
    }

    private sealed class ReviewPhotoPayload
    {
        [JsonPropertyName("fullImageDataUrl")]
        public string? FullImageDataUrl { get; set; }

        [JsonPropertyName("thumbnailDataUrl")]
        public string? ThumbnailDataUrl { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("sortOrder")]
        public int SortOrder { get; set; }
    }
}
