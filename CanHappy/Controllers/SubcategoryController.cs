using CanHappy.Data;
using CanHappy.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CanHappy.Controllers;

public class SubcategoryController(ApplicationDbContext context) : Controller
{
    [AllowAnonymous]
    public async Task<IActionResult> Index()
    {
        var subcategories = await context.Subcategories
            .Include(s => s.Category)
            .Where(s => !s.DeletedInd)
            .OrderBy(s => s.SortOrder)
            .ToListAsync();
        return View(subcategories);
    }

    [AllowAnonymous]
    public async Task<IActionResult> Details(int? id)
    {
        if (id is null)
            return NotFound();
        var subcategory = await context.Subcategories
            .Include(s => s.Category)
            .FirstOrDefaultAsync(s => s.SubcategoryId == id && !s.DeletedInd);
        if (subcategory is null)
            return NotFound();
        return View(subcategory);
    }

    [Authorize]
    public IActionResult Create()
    {
        ViewData["Categories"] = context.Categories.OrderBy(c => c.Name).ToList();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Create([Bind("CategoryId,Name,Code,Description,KeyWords,SortOrder,SampleInd")] Subcategory subcategory)
    {
        if (!ModelState.IsValid)
        {
            ViewData["Categories"] = context.Categories.OrderBy(c => c.Name).ToList();
            return View(subcategory);
        }
        subcategory.CreatedBy = User.Identity?.Name ?? "web-user";
        subcategory.CreatedDate = CanHappy.Common.EasternTime.Now;
        context.Subcategories.Add(subcategory);
        await context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [Authorize]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id is null)
            return NotFound();
        var subcategory = await context.Subcategories.FindAsync(id);
        if (subcategory is null)
            return NotFound();
        ViewData["Categories"] = context.Categories.OrderBy(c => c.Name).ToList();
        return View(subcategory);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Edit(int id, [Bind("SubcategoryId,CategoryId,Name,Code,Description,KeyWords,SortOrder,SampleInd,DeletedInd,CreatedBy,CreatedDate")] Subcategory subcategory)
    {
        if (id != subcategory.SubcategoryId)
            return NotFound();
        if (!ModelState.IsValid)
        {
            ViewData["Categories"] = context.Categories.OrderBy(c => c.Name).ToList();
            return View(subcategory);
        }
        try
        {
            subcategory.ModifiedBY = User.Identity?.Name ?? "web-user";
            subcategory.ModifiedDate = CanHappy.Common.EasternTime.Now;
            context.Update(subcategory);
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await SubcategoryExists(subcategory.SubcategoryId))
                return NotFound();
            throw;
        }
        return RedirectToAction(nameof(Index));
    }

    [Authorize]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id is null)
            return NotFound();
        var subcategory = await context.Subcategories
            .Include(s => s.Category)
            .FirstOrDefaultAsync(s => s.SubcategoryId == id && !s.DeletedInd);
        if (subcategory is null)
            return NotFound();
        return View(subcategory);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var subcategory = await context.Subcategories.FindAsync(id);
        if (subcategory is not null)
        {
            subcategory.DeletedInd = true;
            subcategory.ModifiedBY = User.Identity?.Name ?? "web-user";
            subcategory.ModifiedDate = CanHappy.Common.EasternTime.Now;
            await context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    private Task<bool> SubcategoryExists(int id)
    {
        return context.Subcategories.AnyAsync(e => e.SubcategoryId == id);
    }
}

