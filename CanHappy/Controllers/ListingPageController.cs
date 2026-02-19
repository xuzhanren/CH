using CanHappy.Data;
using CanHappy.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CanHappy.Controllers;

[Route("Listing")]
public class ListingPageController(ApplicationDbContext context, IWebHostEnvironment environment) : Controller
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
    public async Task<IActionResult> Create([Bind("CategoryId,SubcategoryId,Subject,Description,KeyWords,ProvinceId,CityId,PostalCode,Price,DiscountPercent,ThumbnailURL")] Listing listing, string? croppedThumbnailData)
    {
        if (!ModelState.IsValid)
        {
            PopulateSelectLists(listing.CategoryId, listing.SubcategoryId, listing.ProvinceId, listing.CityId);
            return View("~/Views/Listing/Create.cshtml", listing);
        }

        listing.ListingGUID = Guid.NewGuid();
        listing.CreatedDate = DateTime.UtcNow;
        listing.ModifiedDate = null;

        if (!string.IsNullOrWhiteSpace(croppedThumbnailData))
        {
            listing.ThumbnailURL = await SaveThumbnailFromDataUrlAsync(croppedThumbnailData);
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
    public async Task<IActionResult> Edit(Guid id, [Bind("ListingGUID,CategoryId,SubcategoryId,Subject,Description,KeyWords,ProvinceId,CityId,PostalCode,Price,DiscountPercent,ThumbnailURL")] Listing listing, string? croppedThumbnailData)
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

        var existingListing = await context.Listings.FirstOrDefaultAsync(item => item.ListingGUID == id);
        if (existingListing is null)
        {
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(croppedThumbnailData))
        {
            listing.ThumbnailURL = await SaveThumbnailFromDataUrlAsync(croppedThumbnailData);
        }

        try
        {
            existingListing.CategoryId = listing.CategoryId;
            existingListing.SubcategoryId = listing.SubcategoryId;
            existingListing.Subject = listing.Subject;
            existingListing.Description = listing.Description;
            existingListing.KeyWords = listing.KeyWords;
            existingListing.ProvinceId = listing.ProvinceId;
            existingListing.CityId = listing.CityId;
            existingListing.PostalCode = listing.PostalCode;
            existingListing.Price = listing.Price;
            existingListing.DiscountPercent = listing.DiscountPercent;
            existingListing.ThumbnailURL = listing.ThumbnailURL;
            existingListing.ModifiedDate = DateTime.UtcNow;

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
        ViewData["SubcategoryLookup"] = context.Subcategories
            .AsNoTracking()
            .OrderBy(subcategory => subcategory.Name)
            .Select(subcategory => new
            {
                id = subcategory.SubcategoryId,
                categoryId = subcategory.CategoryId,
                name = subcategory.Name
            })
            .ToList();
        ViewData["CityLookup"] = context.Cities
            .AsNoTracking()
            .OrderBy(city => city.Name)
            .Select(city => new
            {
                id = city.CityId,
                provinceId = city.ProvinceId,
                name = city.Name
            })
            .ToList();
    }

    private bool ListingExists(Guid id)
    {
        return context.Listings.Any(listing => listing.ListingGUID == id);
    }

    private async Task<string?> SaveThumbnailFromDataUrlAsync(string dataUrl)
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

        var extension = metadata.Contains("image/png", StringComparison.OrdinalIgnoreCase) ? ".png" : ".jpg";
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var relativePath = $"/ListingImages/{fileName}";
        var folderPath = Path.Combine(environment.WebRootPath, "ListingImages");

        Directory.CreateDirectory(folderPath);

        var filePath = Path.Combine(folderPath, fileName);
        await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);

        return relativePath;
    }
}
