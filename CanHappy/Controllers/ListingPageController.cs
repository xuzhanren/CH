using CanHappy.Data;
using CanHappy.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CanHappy.Controllers;

[Route("Listing")]
public class ListingPageController(ApplicationDbContext context) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(string? categoryName, string? subcategoryName)
    {
        var query = context.Listings
            .Include(listing => listing.Category)
            .Include(listing => listing.Subcategory)
            .Include(listing => listing.Province)
            .Include(listing => listing.City)
            .AsNoTracking()
            .Where(listing => !listing.DeletedInd)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(categoryName))
        {
            query = query.Where(listing => listing.Category != null && listing.Category.Name == categoryName);
            ViewData["CategoryName"] = categoryName;
        }

        if (!string.IsNullOrWhiteSpace(subcategoryName))
        {
            query = query.Where(listing => listing.Subcategory != null && listing.Subcategory.Name == subcategoryName);
            ViewData["SubcategoryName"] = subcategoryName;
        }

        var listings = await query
            .OrderByDescending(listing => listing.CreatedDate)
            .ToListAsync();

        return View("~/Views/Listing/Index.cshtml", listings);
    }

    [HttpGet("Details/{id:guid}")]
    public async Task<IActionResult> Details(Guid? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var listing = await context.Listings
            .Include(listing => listing.Category)
            .Include(listing => listing.Subcategory)
            .Include(listing => listing.Province)
            .Include(listing => listing.City)
            .FirstOrDefaultAsync(listing => listing.ListingGUID == id);

        if (listing is null)
        {
            return NotFound();
        }

        return View("~/Views/Listing/Details.cshtml", listing);
    }

    [HttpGet("Create")]
    public IActionResult Create()
    {
        PopulateSelectLists();
        return View("~/Views/Listing/Create.cshtml");
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ListingGUID,CategoryId,SubcategoryId,Subject,Description,KeyWords,ProvinceId,CityId,PostalCode,ViewCount,ClickCount,DeletedInd,SampleInd,UserId,CreatedBy,ModifiedBY,CreatedDate,ModifiedDate")] Listing listing)
    {
        if (!ModelState.IsValid)
        {
            PopulateSelectLists(listing.CategoryId, listing.SubcategoryId, listing.ProvinceId, listing.CityId);
            return View("~/Views/Listing/Create.cshtml", listing);
        }

        context.Add(listing);
        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Edit/{id:guid}")]
    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var listing = await context.Listings.FindAsync(id);
        if (listing is null)
        {
            return NotFound();
        }

        PopulateSelectLists(listing.CategoryId, listing.SubcategoryId, listing.ProvinceId, listing.CityId);
        return View("~/Views/Listing/Edit.cshtml", listing);
    }

    [HttpPost("Edit/{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, [Bind("ListingGUID,CategoryId,SubcategoryId,Subject,Description,KeyWords,ProvinceId,CityId,PostalCode,ViewCount,ClickCount,DeletedInd,SampleInd,UserId,CreatedBy,ModifiedBY,CreatedDate,ModifiedDate")] Listing listing)
    {
        if (id != listing.ListingGUID)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            PopulateSelectLists(listing.CategoryId, listing.SubcategoryId, listing.ProvinceId, listing.CityId);
            return View("~/Views/Listing/Edit.cshtml", listing);
        }

        try
        {
            context.Update(listing);
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ListingExists(listing.ListingGUID))
            {
                return NotFound();
            }

            throw;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet("Delete/{id:guid}")]
    public async Task<IActionResult> Delete(Guid? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var listing = await context.Listings
            .Include(listing => listing.Category)
            .Include(listing => listing.Subcategory)
            .Include(listing => listing.Province)
            .Include(listing => listing.City)
            .FirstOrDefaultAsync(listing => listing.ListingGUID == id);

        if (listing is null)
        {
            return NotFound();
        }

        return View("~/Views/Listing/Delete.cshtml", listing);
    }

    [HttpPost("Delete/{id:guid}"), ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var listing = await context.Listings.FindAsync(id);
        if (listing is not null)
        {
            context.Listings.Remove(listing);
            await context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private void PopulateSelectLists(int? categoryId = null, int? subcategoryId = null, int? provinceId = null, int? cityId = null)
    {
        ViewData["CategoryId"] = new SelectList(context.Categories.OrderBy(category => category.Name), "CategoryId", "Name", categoryId);
        ViewData["SubcategoryId"] = new SelectList(context.Subcategories.OrderBy(subcategory => subcategory.Name), "SubcategoryId", "Name", subcategoryId);
        ViewData["ProvinceId"] = new SelectList(context.Provinces.OrderBy(province => province.Name), "ProvinceId", "Name", provinceId);
        ViewData["CityId"] = new SelectList(context.Cities.OrderBy(city => city.Name), "CityId", "Name", cityId);
    }

    private bool ListingExists(Guid id)
    {
        return context.Listings.Any(listing => listing.ListingGUID == id);
    }
}
