using CanHappy.Data;
using CanHappy.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CanHappy.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Ad>>> GetAll()
    {
        return Ok(await context.Ads
            .AsNoTracking()
            .Where(a => !a.DeletedInd)
            .OrderByDescending(a => a.PublishDate)
            .ToListAsync());
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<Ad>> GetById(Guid id)
    {
        var ad = await context.Ads.FindAsync(id);
        if (ad is null || ad.DeletedInd)
        {
            return NotFound();
        }

        return Ok(ad);
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<Ad>> Create(Ad ad)
    {
        ad.AdGUID = Guid.NewGuid();
        ad.CreatedBy = User.Identity?.Name ?? "api-user";
        ad.CreatedDate = DateTime.UtcNow;
        ad.ModifiedBY = null;
        ad.ModifiedDate = null;
        ad.DeletedInd = false;

        context.Ads.Add(ad);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = ad.AdGUID }, ad);
    }

    [HttpPut("{id:guid}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> Update(Guid id, Ad updatedAd)
    {
        var ad = await context.Ads.FindAsync(id);
        if (ad is null || ad.DeletedInd)
        {
            return NotFound();
        }

        ad.CityId = updatedAd.CityId;
        ad.PostalCode = updatedAd.PostalCode;
        ad.Subject = updatedAd.Subject;
        ad.Description = updatedAd.Description;
        ad.TargetURL = updatedAd.TargetURL;
        ad.ImageURL = updatedAd.ImageURL;
        ad.Price = updatedAd.Price;
        ad.CurrencyCode = updatedAd.CurrencyCode;
        ad.Status = updatedAd.Status;
        ad.IsFeatured = updatedAd.IsFeatured;
        ad.PublishDate = updatedAd.PublishDate;
        ad.ExpiryDate = updatedAd.ExpiryDate;
        ad.ViewCount = updatedAd.ViewCount;
        ad.ContactName = updatedAd.ContactName;
        ad.ContactEmail = updatedAd.ContactEmail;
        ad.ContactPhone = updatedAd.ContactPhone;
        ad.SampleInd = updatedAd.SampleInd;
        ad.ModifiedBY = User.Identity?.Name ?? "api-user";
        ad.ModifiedDate = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var ad = await context.Ads.FindAsync(id);
        if (ad is null)
        {
            return NotFound();
        }

        ad.DeletedInd = true;
        ad.ModifiedBY = User.Identity?.Name ?? "api-user";
        ad.ModifiedDate = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return NoContent();
    }
}
