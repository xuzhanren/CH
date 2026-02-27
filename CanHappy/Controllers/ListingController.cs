using CanHappy.Data;
using CanHappy.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CanHappy.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ListingController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Listing>>> GetAll()
    {
        return Ok(await context.Listings
            .AsNoTracking()
            .Where(l => !l.DeletedInd)
            .OrderByDescending(l => l.CreatedDate)
            .ToListAsync());
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<Listing>> GetById(Guid id)
    {
        var listing = await context.Listings.FindAsync(id);
        if (listing is null || listing.DeletedInd)
        {
            return NotFound();
        }

        return Ok(listing);
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<Listing>> Create(Listing listing)
    {
        listing.ListingGUID = Guid.NewGuid();
        listing.CreatedBy = User.Identity?.Name ?? "api-user";
        listing.CreatedDate = CanHappy.Common.EasternTime.Now;
        listing.ModifiedBY = null;
        listing.ModifiedDate = null;
        listing.DeletedInd = false;

        context.Listings.Add(listing);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = listing.ListingGUID }, listing);
    }

    [HttpPut("{id:guid}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> Update(Guid id, Listing updatedListing)
    {
        var listing = await context.Listings.FindAsync(id);
        if (listing is null || listing.DeletedInd)
        {
            return NotFound();
        }

        listing.CategoryId = updatedListing.CategoryId;
        listing.SubcategoryId = updatedListing.SubcategoryId;
        listing.Subject = updatedListing.Subject;
        listing.Description = updatedListing.Description;
        listing.KeyWords = updatedListing.KeyWords;
        listing.ProvinceId = updatedListing.ProvinceId;
        listing.CityId = updatedListing.CityId;
        listing.PostalCode = updatedListing.PostalCode;
        listing.ContactPhone = updatedListing.ContactPhone;
        listing.ContactName = updatedListing.ContactName;
        listing.ShowContactInd = updatedListing.ShowContactInd;
        listing.ViewCount = updatedListing.ViewCount;
        listing.ClickCount = updatedListing.ClickCount;
        listing.SampleInd = updatedListing.SampleInd;
        listing.UserId = updatedListing.UserId;
        listing.ModifiedBY = User.Identity?.Name ?? "api-user";
        listing.ModifiedDate = CanHappy.Common.EasternTime.Now;

        await context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var listing = await context.Listings.FindAsync(id);
        if (listing is null)
        {
            return NotFound();
        }

        listing.DeletedInd = true;
        listing.ModifiedBY = User.Identity?.Name ?? "api-user";
        listing.ModifiedDate = CanHappy.Common.EasternTime.Now;

        await context.SaveChangesAsync();
        return NoContent();
    }
}

