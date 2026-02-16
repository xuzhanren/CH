using CanHappy.Data;
using CanHappy.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CanHappy.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class CategoryController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Category>>> GetAll()
    {
        return Ok(await context.Categories
            .Where(c => !c.DeletedInd)
            .OrderBy(c => c.SortOrder)
            .ToListAsync());
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<Category>> GetById(int id)
    {
        var category = await context.Categories.FindAsync(id);
        if (category is null || category.DeletedInd)
        {
            return NotFound();
        }

        return Ok(category);
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<Category>> Create(Category category)
    {
        category.CategoryId = 0;
        category.CreatedBy = User.Identity?.Name ?? "api-user";
        category.CreatedDate = DateTime.UtcNow;

        context.Categories.Add(category);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = category.CategoryId }, category);
    }

    [HttpPut("{id:int}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> Update(int id, Category updatedCategory)
    {
        var category = await context.Categories.FindAsync(id);
        if (category is null || category.DeletedInd)
        {
            return NotFound();
        }

        category.Name = updatedCategory.Name;
        category.Code = updatedCategory.Code;
        category.Description = updatedCategory.Description;
        category.SortOrder = updatedCategory.SortOrder;
        category.SampleInd = updatedCategory.SampleInd;
        category.ModifiedBy = User.Identity?.Name ?? "api-user";
        category.ModifiedDate = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await context.Categories.FindAsync(id);
        if (category is null)
        {
            return NotFound();
        }

        category.DeletedInd = true;
        category.ModifiedBy = User.Identity?.Name ?? "api-user";
        category.ModifiedDate = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return NoContent();
    }
}