using CanHappy.Data;
using CanHappy.Models;
using CanHappy.Models.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CanHappy.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController(
    UserManager<IdentityUser> userManager,
    RoleManager<IdentityRole> roleManager,
    ApplicationDbContext context) : Controller
{
    public async Task<IActionResult> Users()
    {
        var users = userManager.Users.OrderBy(user => user.UserName).ToList();
        var model = new List<AdminUserListItemViewModel>(users.Count);

        foreach (var user in users)
        {
            var userRoles = await userManager.GetRolesAsync(user);
            model.Add(new AdminUserListItemViewModel
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                EmailConfirmed = user.EmailConfirmed,
                Roles = userRoles.ToList()
            });
        }

        model = model.OrderBy(user => user.UserName).ToList();

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> CreateUser()
    {
        var model = new AdminUserEditViewModel();
        await PopulateRolesAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(AdminUserEditViewModel model)
    {
        await PopulateRolesAsync(model);
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new IdentityUser
        {
            UserName = model.UserName,
            Email = model.Email,
            EmailConfirmed = model.EmailConfirmed
        };

        var createResult = await userManager.CreateAsync(user, model.Password!);
        if (!createResult.Succeeded)
        {
            AddIdentityErrors(createResult.Errors);
            return View(model);
        }

        var selectedRoles = model.SelectedRoleNames.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (selectedRoles.Length > 0)
        {
            var roleResult = await userManager.AddToRolesAsync(user, selectedRoles);
            if (!roleResult.Succeeded)
            {
                AddIdentityErrors(roleResult.Errors);
                return View(model);
            }
        }

        return RedirectToAction(nameof(Users));
    }

    [HttpGet]
    public async Task<IActionResult> EditUser(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var roles = await userManager.GetRolesAsync(user);
        var model = new AdminUserEditViewModel
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            EmailConfirmed = user.EmailConfirmed,
            SelectedRoleNames = roles.ToList()
        };

        await PopulateRolesAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(AdminUserEditViewModel model)
    {
        await PopulateRolesAsync(model);
        if (string.IsNullOrWhiteSpace(model.Id))
        {
            return NotFound();
        }

        var user = await userManager.FindByIdAsync(model.Id);
        if (user is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        user.UserName = model.UserName;
        user.Email = model.Email;
        user.EmailConfirmed = model.EmailConfirmed;

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            AddIdentityErrors(updateResult.Errors);
            return View(model);
        }

        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            var removePasswordResult = await userManager.RemovePasswordAsync(user);
            if (!removePasswordResult.Succeeded)
            {
                AddIdentityErrors(removePasswordResult.Errors);
                return View(model);
            }

            var addPasswordResult = await userManager.AddPasswordAsync(user, model.Password);
            if (!addPasswordResult.Succeeded)
            {
                AddIdentityErrors(addPasswordResult.Errors);
                return View(model);
            }
        }

        var currentRoles = await userManager.GetRolesAsync(user);
        var desiredRoles = model.SelectedRoleNames.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        var removeRoles = currentRoles.Except(desiredRoles, StringComparer.OrdinalIgnoreCase).ToArray();
        if (removeRoles.Length > 0)
        {
            var removeRolesResult = await userManager.RemoveFromRolesAsync(user, removeRoles);
            if (!removeRolesResult.Succeeded)
            {
                AddIdentityErrors(removeRolesResult.Errors);
                return View(model);
            }
        }

        var addRoles = desiredRoles.Except(currentRoles, StringComparer.OrdinalIgnoreCase).ToArray();
        if (addRoles.Length > 0)
        {
            var addRolesResult = await userManager.AddToRolesAsync(user, addRoles);
            if (!addRolesResult.Succeeded)
            {
                AddIdentityErrors(addRolesResult.Errors);
                return View(model);
            }
        }

        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result.Errors);
            var users = userManager.Users.OrderBy(item => item.UserName).ToList();
            var model = new List<AdminUserListItemViewModel>(users.Count);

            foreach (var item in users)
            {
                var userRoles = await userManager.GetRolesAsync(item);
                model.Add(new AdminUserListItemViewModel
                {
                    Id = item.Id,
                    UserName = item.UserName,
                    Email = item.Email,
                    EmailConfirmed = item.EmailConfirmed,
                    Roles = userRoles.ToList()
                });
            }

            return View(nameof(Users), model);
        }

        return RedirectToAction(nameof(Users));
    }

    public IActionResult Roles()
    {
        var roles = roleManager.Roles
            .OrderBy(role => role.Name)
            .Select(role => new AdminRoleViewModel
            {
                Id = role.Id,
                Name = role.Name
            })
            .ToList();

        return View(roles);
    }

    [HttpGet]
    public IActionResult CreateRole()
    {
        return View(new AdminRoleEditViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRole(AdminRoleEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var role = new IdentityRole(model.Name!.Trim());
        var result = await roleManager.CreateAsync(role);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result.Errors);
            return View(model);
        }

        return RedirectToAction(nameof(Roles));
    }

    [HttpGet]
    public async Task<IActionResult> Ads()
    {
        var ads = await context.Ads
            .AsNoTracking()
            .Include(ad => ad.City)
            .Where(ad => !ad.DeletedInd)
            .OrderByDescending(ad => ad.PublishDate)
            .ToListAsync();

        return View(ads);
    }

    [HttpGet]
    public async Task<IActionResult> CreateAd()
    {
        await PopulateCitySelectListAsync();
        return View(new Ad { PublishDate = CanHappy.Common.EasternTime.Now, Status = "Draft" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAd(Ad model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateCitySelectListAsync(model.CityId);
            return View(model);
        }

        model.AdGUID = Guid.NewGuid();
        model.CreatedBy = User.Identity?.Name ?? "admin";
        model.CreatedDate = CanHappy.Common.EasternTime.Now;
        model.ModifiedBY = null;
        model.ModifiedDate = null;
        model.DeletedInd = false;

        context.Ads.Add(model);
        await context.SaveChangesAsync();

        return RedirectToAction(nameof(Ads));
    }

    [HttpGet]
    public async Task<IActionResult> EditAd(Guid id)
    {
        var ad = await context.Ads.FirstOrDefaultAsync(item => item.AdGUID == id && !item.DeletedInd);
        if (ad is null)
        {
            return NotFound();
        }

        await PopulateCitySelectListAsync(ad.CityId);
        return View(ad);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditAd(Guid id, Ad model)
    {
        var ad = await context.Ads.FirstOrDefaultAsync(item => item.AdGUID == id && !item.DeletedInd);
        if (ad is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await PopulateCitySelectListAsync(model.CityId);
            model.AdGUID = id;
            return View(model);
        }

        ad.CityId = model.CityId;
        ad.PostalCode = model.PostalCode;
        ad.Subject = model.Subject;
        ad.Description = model.Description;
        ad.TargetURL = model.TargetURL;
        ad.ImageURL = model.ImageURL;
        ad.Price = model.Price;
        ad.CurrencyCode = model.CurrencyCode;
        ad.Status = model.Status;
        ad.IsFeatured = model.IsFeatured;
        ad.PublishDate = model.PublishDate;
        ad.ExpiryDate = model.ExpiryDate;
        ad.ContactName = model.ContactName;
        ad.ContactEmail = model.ContactEmail;
        ad.ContactPhone = model.ContactPhone;
        ad.SampleInd = model.SampleInd;
        ad.ModifiedBY = User.Identity?.Name ?? "admin";
        ad.ModifiedDate = CanHappy.Common.EasternTime.Now;

        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Ads));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAd(Guid id)
    {
        var ad = await context.Ads.FirstOrDefaultAsync(item => item.AdGUID == id && !item.DeletedInd);
        if (ad is null)
        {
            return NotFound();
        }

        ad.DeletedInd = true;
        ad.ModifiedBY = User.Identity?.Name ?? "admin";
        ad.ModifiedDate = CanHappy.Common.EasternTime.Now;
        await context.SaveChangesAsync();

        return RedirectToAction(nameof(Ads));
    }

    [HttpGet]
    public async Task<IActionResult> Areas()
    {
        var areas = await context.Areas
            .AsNoTracking()
            .Include(area => area.City)
            .Where(area => !area.DeletedInd)
            .OrderBy(area => area.Name)
            .ToListAsync();

        return View(areas);
    }

    [HttpGet]
    public async Task<IActionResult> CreateArea()
    {
        await PopulateCitySelectListAsync();
        return View(new Area());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateArea(Area model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateCitySelectListAsync(model.CityId);
            return View(model);
        }

        model.AreaId = 0;
        model.CreatedBy = User.Identity?.Name ?? "admin";
        model.CreatedDate = CanHappy.Common.EasternTime.Now;
        model.ModifiedBY = null;
        model.ModifiedDate = null;
        model.DeletedInd = false;

        context.Areas.Add(model);
        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Areas));
    }

    [HttpGet]
    public async Task<IActionResult> EditArea(int id)
    {
        var area = await context.Areas.FirstOrDefaultAsync(item => item.AreaId == id && !item.DeletedInd);
        if (area is null)
        {
            return NotFound();
        }

        await PopulateCitySelectListAsync(area.CityId);
        return View(area);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditArea(int id, Area model)
    {
        var area = await context.Areas.FirstOrDefaultAsync(item => item.AreaId == id && !item.DeletedInd);
        if (area is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await PopulateCitySelectListAsync(model.CityId);
            model.AreaId = id;
            return View(model);
        }

        area.CityId = model.CityId;
        area.Name = model.Name;
        area.Code = model.Code;
        area.Description = model.Description;
        area.SampleInd = model.SampleInd;
        area.ModifiedBY = User.Identity?.Name ?? "admin";
        area.ModifiedDate = CanHappy.Common.EasternTime.Now;

        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Areas));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteArea(int id)
    {
        var area = await context.Areas.FirstOrDefaultAsync(item => item.AreaId == id && !item.DeletedInd);
        if (area is null)
        {
            return NotFound();
        }

        area.DeletedInd = true;
        area.ModifiedBY = User.Identity?.Name ?? "admin";
        area.ModifiedDate = CanHappy.Common.EasternTime.Now;
        await context.SaveChangesAsync();

        return RedirectToAction(nameof(Areas));
    }

    [HttpGet]
    public async Task<IActionResult> Cities()
    {
        var cities = await context.Cities
            .AsNoTracking()
            .Include(city => city.Province)
            .Where(city => !city.DeletedInd)
            .OrderBy(city => city.Name)
            .ToListAsync();

        return View(cities);
    }

    [HttpGet]
    public async Task<IActionResult> CreateCity()
    {
        await PopulateProvinceSelectListAsync();
        return View(new City());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateCity(City model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateProvinceSelectListAsync(model.ProvinceId);
            return View(model);
        }

        model.CityId = 0;
        model.CreatedBy = User.Identity?.Name ?? "admin";
        model.CreatedDate = CanHappy.Common.EasternTime.Now;
        model.ModifiedBY = null;
        model.ModifiedDate = null;
        model.DeletedInd = false;

        context.Cities.Add(model);
        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Cities));
    }

    [HttpGet]
    public async Task<IActionResult> EditCity(int id)
    {
        var city = await context.Cities.FirstOrDefaultAsync(item => item.CityId == id && !item.DeletedInd);
        if (city is null)
        {
            return NotFound();
        }

        await PopulateProvinceSelectListAsync(city.ProvinceId);
        return View(city);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCity(int id, City model)
    {
        var city = await context.Cities.FirstOrDefaultAsync(item => item.CityId == id && !item.DeletedInd);
        if (city is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await PopulateProvinceSelectListAsync(model.ProvinceId);
            model.CityId = id;
            return View(model);
        }

        city.ProvinceId = model.ProvinceId;
        city.Name = model.Name;
        city.Code = model.Code;
        city.Description = model.Description;
        city.SampleInd = model.SampleInd;
        city.ModifiedBY = User.Identity?.Name ?? "admin";
        city.ModifiedDate = CanHappy.Common.EasternTime.Now;

        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Cities));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCity(int id)
    {
        var city = await context.Cities.FirstOrDefaultAsync(item => item.CityId == id && !item.DeletedInd);
        if (city is null)
        {
            return NotFound();
        }

        city.DeletedInd = true;
        city.ModifiedBY = User.Identity?.Name ?? "admin";
        city.ModifiedDate = CanHappy.Common.EasternTime.Now;
        await context.SaveChangesAsync();

        return RedirectToAction(nameof(Cities));
    }

    [HttpGet]
    public async Task<IActionResult> Provinces()
    {
        var provinces = await context.Provinces
            .AsNoTracking()
            .Include(province => province.Country)
            .Where(province => !province.DeletedInd)
            .OrderBy(province => province.Name)
            .ToListAsync();

        return View(provinces);
    }

    [HttpGet]
    public async Task<IActionResult> CreateProvince()
    {
        await PopulateCountrySelectListAsync();
        return View(new Province());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateProvince(Province model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateCountrySelectListAsync(model.CountryId);
            return View(model);
        }

        model.ProvinceId = 0;
        model.CreatedBy = User.Identity?.Name ?? "admin";
        model.CreatedDate = CanHappy.Common.EasternTime.Now;
        model.ModifiedBY = null;
        model.ModifiedDate = null;
        model.DeletedInd = false;

        context.Provinces.Add(model);
        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Provinces));
    }

    [HttpGet]
    public async Task<IActionResult> EditProvince(int id)
    {
        var province = await context.Provinces.FirstOrDefaultAsync(item => item.ProvinceId == id && !item.DeletedInd);
        if (province is null)
        {
            return NotFound();
        }

        await PopulateCountrySelectListAsync(province.CountryId);
        return View(province);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProvince(int id, Province model)
    {
        var province = await context.Provinces.FirstOrDefaultAsync(item => item.ProvinceId == id && !item.DeletedInd);
        if (province is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await PopulateCountrySelectListAsync(model.CountryId);
            model.ProvinceId = id;
            return View(model);
        }

        province.CountryId = model.CountryId;
        province.Name = model.Name;
        province.Code = model.Code;
        province.Description = model.Description;
        province.SampleInd = model.SampleInd;
        province.ModifiedBY = User.Identity?.Name ?? "admin";
        province.ModifiedDate = CanHappy.Common.EasternTime.Now;

        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Provinces));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProvince(int id)
    {
        var province = await context.Provinces.FirstOrDefaultAsync(item => item.ProvinceId == id && !item.DeletedInd);
        if (province is null)
        {
            return NotFound();
        }

        province.DeletedInd = true;
        province.ModifiedBY = User.Identity?.Name ?? "admin";
        province.ModifiedDate = CanHappy.Common.EasternTime.Now;
        await context.SaveChangesAsync();

        return RedirectToAction(nameof(Provinces));
    }

    [HttpGet]
    public async Task<IActionResult> Listings()
    {
        var listings = await context.Listings
            .AsNoTracking()
            .Include(listing => listing.Category)
            .Include(listing => listing.Subcategory)
            .Include(listing => listing.Province)
            .Include(listing => listing.City)
            .Where(listing => !listing.DeletedInd)
            .OrderByDescending(listing => listing.CreatedDate)
            .ToListAsync();

        return View(listings);
    }

    [HttpGet]
    public async Task<IActionResult> CreateListing()
    {
        await PopulateListingLookupSelectListsAsync();
        return View(new Listing());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateListing(Listing model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateListingLookupSelectListsAsync(model.CategoryId, model.SubcategoryId, model.ProvinceId, model.CityId);
            return View(model);
        }

        model.ListingGUID = Guid.NewGuid();
        model.UserId = model.UserId == Guid.Empty ? Guid.NewGuid() : model.UserId;
        model.CreatedBy = User.Identity?.Name ?? "admin";
        model.CreatedDate = CanHappy.Common.EasternTime.Now;
        model.ModifiedBY = null;
        model.ModifiedDate = null;
        model.DeletedInd = false;

        context.Listings.Add(model);
        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Listings));
    }

    [HttpGet]
    public async Task<IActionResult> EditListing(Guid id)
    {
        var listing = await context.Listings.FirstOrDefaultAsync(item => item.ListingGUID == id && !item.DeletedInd);
        if (listing is null)
        {
            return NotFound();
        }

        await PopulateListingLookupSelectListsAsync(listing.CategoryId, listing.SubcategoryId, listing.ProvinceId, listing.CityId);
        return View(listing);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditListing(Guid id, Listing model)
    {
        var listing = await context.Listings.FirstOrDefaultAsync(item => item.ListingGUID == id && !item.DeletedInd);
        if (listing is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await PopulateListingLookupSelectListsAsync(model.CategoryId, model.SubcategoryId, model.ProvinceId, model.CityId);
            model.ListingGUID = id;
            return View(model);
        }

        listing.CategoryId = model.CategoryId;
        listing.SubcategoryId = model.SubcategoryId;
        listing.Subject = model.Subject;
        listing.Description = model.Description;
        listing.KeyWords = model.KeyWords;
        listing.ProvinceId = model.ProvinceId;
        listing.CityId = model.CityId;
        listing.PostalCode = model.PostalCode;
        listing.ViewCount = model.ViewCount;
        listing.ClickCount = model.ClickCount;
        listing.SampleInd = model.SampleInd;
        listing.UserId = model.UserId == Guid.Empty ? listing.UserId : model.UserId;
        listing.ModifiedBY = User.Identity?.Name ?? "admin";
        listing.ModifiedDate = CanHappy.Common.EasternTime.Now;

        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Listings));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteListing(Guid id)
    {
        var listing = await context.Listings.FirstOrDefaultAsync(item => item.ListingGUID == id && !item.DeletedInd);
        if (listing is null)
        {
            return NotFound();
        }

        listing.DeletedInd = true;
        listing.ModifiedBY = User.Identity?.Name ?? "admin";
        listing.ModifiedDate = CanHappy.Common.EasternTime.Now;
        await context.SaveChangesAsync();

        return RedirectToAction(nameof(Listings));
    }

    [HttpGet]
    public async Task<IActionResult> EditRole(string id)
    {
        var role = await roleManager.FindByIdAsync(id);
        if (role is null)
        {
            return NotFound();
        }

        return View(new AdminRoleEditViewModel
        {
            Id = role.Id,
            Name = role.Name
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditRole(AdminRoleEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var role = await roleManager.FindByIdAsync(model.Id!);
        if (role is null)
        {
            return NotFound();
        }

        role.Name = model.Name!.Trim();
        role.NormalizedName = model.Name.Trim().ToUpperInvariant();

        var result = await roleManager.UpdateAsync(role);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result.Errors);
            return View(model);
        }

        return RedirectToAction(nameof(Roles));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRole(string id)
    {
        var role = await roleManager.FindByIdAsync(id);
        if (role is null)
        {
            return NotFound();
        }

        var result = await roleManager.DeleteAsync(role);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result.Errors);
            return RedirectToAction(nameof(Roles));
        }

        return RedirectToAction(nameof(Roles));
    }

    private async Task PopulateRolesAsync(AdminUserEditViewModel model)
    {
        model.AvailableRoles = await roleManager.Roles
            .OrderBy(role => role.Name)
            .Select(role => role.Name!)
            .ToListAsync();
    }

    private async Task PopulateCitySelectListAsync(int? selectedCityId = null)
    {
        var cities = await context.Cities
            .AsNoTracking()
            .Where(city => !city.DeletedInd)
            .OrderBy(city => city.Name)
            .Select(city => new { city.CityId, city.Name })
            .ToListAsync();

        ViewData["CityId"] = new SelectList(cities, "CityId", "Name", selectedCityId);
    }

    private async Task PopulateProvinceSelectListAsync(int? selectedProvinceId = null)
    {
        var provinces = await context.Provinces
            .AsNoTracking()
            .Where(province => !province.DeletedInd)
            .OrderBy(province => province.Name)
            .Select(province => new { province.ProvinceId, province.Name })
            .ToListAsync();

        ViewData["ProvinceId"] = new SelectList(provinces, "ProvinceId", "Name", selectedProvinceId);
    }

    private async Task PopulateCountrySelectListAsync(int? selectedCountryId = null)
    {
        var countries = await context.Countries
            .AsNoTracking()
            .Where(country => !country.DeletedInd)
            .OrderBy(country => country.Name)
            .Select(country => new { country.CountryId, country.Name })
            .ToListAsync();

        ViewData["CountryId"] = new SelectList(countries, "CountryId", "Name", selectedCountryId);
    }

    private async Task PopulateListingLookupSelectListsAsync(
        int? selectedCategoryId = null,
        int? selectedSubcategoryId = null,
        int? selectedProvinceId = null,
        int? selectedCityId = null)
    {
        var categories = await context.Categories
            .AsNoTracking()
            .Where(category => !category.DeletedInd)
            .OrderBy(category => category.Name)
            .Select(category => new { category.CategoryId, category.Name })
            .ToListAsync();

        var subcategories = await context.Subcategories
            .AsNoTracking()
            .Where(subcategory => !subcategory.DeletedInd)
            .OrderBy(subcategory => subcategory.Name)
            .Select(subcategory => new { subcategory.SubcategoryId, subcategory.Name })
            .ToListAsync();

        var provinces = await context.Provinces
            .AsNoTracking()
            .Where(province => !province.DeletedInd)
            .OrderBy(province => province.Name)
            .Select(province => new { province.ProvinceId, province.Name })
            .ToListAsync();

        var cities = await context.Cities
            .AsNoTracking()
            .Where(city => !city.DeletedInd)
            .OrderBy(city => city.Name)
            .Select(city => new { city.CityId, city.Name })
            .ToListAsync();

        ViewData["CategoryId"] = new SelectList(categories, "CategoryId", "Name", selectedCategoryId);
        ViewData["SubcategoryId"] = new SelectList(subcategories, "SubcategoryId", "Name", selectedSubcategoryId);
        ViewData["ProvinceId"] = new SelectList(provinces, "ProvinceId", "Name", selectedProvinceId);
        ViewData["CityId"] = new SelectList(cities, "CityId", "Name", selectedCityId);
    }

    private void AddIdentityErrors(IEnumerable<IdentityError> errors)
    {
        foreach (var error in errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }
}
