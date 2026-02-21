using CanHappy.Data;
using CanHappy.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CanHappy.Controllers;

public class CategoryController(ApplicationDbContext context) : Controller
{
    [AllowAnonymous]
    public async Task<IActionResult> Index(string? name)
    {
        var query = context.Categories
            .AsNoTracking()
            .Where(c => !c.DeletedInd);

        if (!string.IsNullOrWhiteSpace(name))
        {
            query = query.Where(c => c.Name == name);
        }

        var categories = await query
            .OrderBy(c => c.SortOrder)
            .ToListAsync();

        ViewData["SelectedCategoryName"] = name;
        return View(categories);
    }

    [AllowAnonymous]
    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var category = await context.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.CategoryId == id && !m.DeletedInd);

        if (category is null)
        {
            return NotFound();
        }

        return View(category);
    }

    [Authorize]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Create([Bind("Name,Code,Description,KeyWords,SortOrder,SampleInd")] Category category)
    {
        if (!ModelState.IsValid)
        {
            return View(category);
        }

        category.CreatedBy = User.Identity?.Name ?? "web-user";
        category.CreatedDate = CanHappy.Common.EasternTime.Now;
        context.Categories.Add(category);
        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [Authorize]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var category = await context.Categories.FindAsync(id);
        if (category is null)
        {
            return NotFound();
        }

        return View(category);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Edit(int id, [Bind("CategoryId,Name,Code,Description,KeyWords,SortOrder,SampleInd,DeletedInd,CreatedBy,CreatedDate")] Category category)
    {
        if (id != category.CategoryId)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(category);
        }

        try
        {
            category.ModifiedBy = User.Identity?.Name ?? "web-user";
            category.ModifiedDate = CanHappy.Common.EasternTime.Now;
            context.Update(category);
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await CategoryExists(category.CategoryId))
            {
                return NotFound();
            }

            throw;
        }

        return RedirectToAction(nameof(Index));
    }

    [Authorize]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var category = await context.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.CategoryId == id && !m.DeletedInd);

        if (category is null)
        {
            return NotFound();
        }

        return View(category);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var category = await context.Categories.FindAsync(id);
        if (category is not null)
        {
            category.DeletedInd = true;
            category.ModifiedBy = User.Identity?.Name ?? "web-user";
            category.ModifiedDate = CanHappy.Common.EasternTime.Now;
            await context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private Task<bool> CategoryExists(int id)
    {
        return context.Categories.AnyAsync(e => e.CategoryId == id);
    }
}
