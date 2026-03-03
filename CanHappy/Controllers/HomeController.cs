using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using CanHappy.Models;
using CanHappy.Data;
using Microsoft.EntityFrameworkCore;

namespace CanHappy.Controllers;

public class HomeController(ApplicationDbContext context, IConfiguration configuration) : Controller
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

        if (provinceId.HasValue && cityId.HasValue)
        {
            query = query.Where(item => item.ProvinceId == provinceId.Value && item.CityId == cityId.Value);
        }

        var ads = await query
            .OrderBy(item => EF.Functions.Random())
            .Take(3)
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

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
