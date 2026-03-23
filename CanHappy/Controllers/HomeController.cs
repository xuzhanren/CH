using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using CanHappy.Models;
using CanHappy.Models.Home;
using CanHappy.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace CanHappy.Controllers;

public class HomeController(
    ApplicationDbContext context,
    IConfiguration configuration) : Controller
{
    public IActionResult Index()
    {
        ViewData["HomePageSliderAdWaitSeconds"] = Math.Max(0, configuration.GetValue<int?>("HomePageSliderAdWaitSeconds") ?? 2);
        ViewData["AdSlidingInterval"] = Math.Max(1, configuration.GetValue<int?>("AdSlidingInterval") ?? 5);
        ViewData["AdSliderFadingMode"] = configuration.GetValue<string>("AdSliderFadingMode") ?? "homeAdFade";
        ViewData["HomeAdPixelResolveTransitionPeriodMiliSeconds"] = Math.Max(100, configuration.GetValue<int?>("homeAdPixelResolveTransitionPeriodMiliSeconds") ?? 1000);
        ViewData["HomeAdFadeTransitionMilliSeconds"] = Math.Max(100, configuration.GetValue<int?>("homeAdFadeTransitionMilliSeconds") ?? 450);
        ViewData["SlideTransitionMilliSeconds"] = Math.Max(100, configuration.GetValue<int?>("slideTransitionMilliSeconds") ?? 650);
        ViewData["HomeAdSlideDistancePercent"] = Math.Clamp(configuration.GetValue<int?>("homeAdSlideDistancePercent") ?? 8, 1, 30);
        ViewData["HomeAdTransitionEasing"] = configuration.GetValue<string>("homeAdTransitionEasing") ?? "ease-in-out";
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> HomePageSliderAds(int? provinceId, int? cityId)
    {
        var today = CanHappy.Common.EasternTime.Now.Date;
        var query = context.Ads
            .AsNoTracking()
            .Include(item => item.AdSizeOption)
            .Where(item => !item.DeletedInd
                && item.ActiveInd
                && item.AdStatusId == 3
                && item.PublishDate < today
                && (!item.ExpiryDate.HasValue || item.ExpiryDate.Value > today)
                && item.AdSizeOption != null
                && item.AdSizeOption.Name == "2400x300HomePageSliderAd"
                && !string.IsNullOrWhiteSpace(item.ImageURL)
                && !string.IsNullOrWhiteSpace(item.TargetURL));

        if (provinceId.HasValue) // && cityId.HasValue)
        {
            query = query.Where(item =>
                item.ProvinceId == null
                ||
                (
                    item.ProvinceId == provinceId.Value
                    && (
                        !item.CityId.HasValue
                        || (item.CityId.HasValue && cityId.HasValue && item.CityId == cityId.Value)
                        )
                )
                );
        }

        var ads = await query
            .OrderBy(item => EF.Functions.Random())
            .Take(10)
            .Select(item => new
            {
                adGuid = item.AdGUID,
                subject = item.Subject,
                imageURL = item.ImageURL,
                targetURL = item.TargetURL
            })
            .ToListAsync();

        return Json(ads);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [HttpGet]
    public IActionResult About()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Contact()
    {
        return View(new ContactViewModel());
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Contact(ContactViewModel model)
    {
        if (!TryGetCurrentUserGuid(out var currentUserId))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var recipientIdStrings = await (
            from userRole in context.UserRoles
            join role in context.Roles on userRole.RoleId equals role.Id
            where role.Name == "Admin" || role.Name == "Clerk"
            select userRole.UserId)
            .Distinct()
            .ToListAsync();

        var recipientIds = recipientIdStrings
            .Select(id => Guid.TryParse(id, out var parsedId) ? parsedId : Guid.Empty)
            .Where(id => id != Guid.Empty && id != currentUserId)
            .Distinct()
            .ToList();

        if (recipientIds.Count == 0)
        {
            TempData["ContactError"] = "No Clerk or Admin users are available to receive this message.";
            return RedirectToAction(nameof(Contact));
        }

        var createdDate = CanHappy.Common.EasternTime.Now;
        var senderName = User.Identity?.Name ?? "User";
        var effectiveSubject = $"Contact: {model.Subject.Trim()}";
        var effectiveBody = $"From: {senderName}\n\n{model.Body.Trim()}";

        foreach (var recipientId in recipientIds)
        {
            context.UserMessages.Add(new UserMessage
            {
                UserMessageGUID = Guid.NewGuid(),
                ListingGUID = null,
                SenderUserId = currentUserId,
                RecipientUserId = recipientId,
                Subject = effectiveSubject,
                Body = effectiveBody,
                IsRead = false,
                CreatedDate = createdDate
            });
        }

        await context.SaveChangesAsync();

        TempData["ContactSuccess"] = "Your message has been sent to all Clerk and Admin users.";
        return RedirectToAction(nameof(Contact));
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private bool TryGetCurrentUserGuid(out Guid userId)
    {
        userId = Guid.Empty;
        var userIdText = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return !string.IsNullOrWhiteSpace(userIdText) && Guid.TryParse(userIdText, out userId);
    }
}
