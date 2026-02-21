using CanHappy.Data;
using CanHappy.Models.Api;
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
    public async Task<ActionResult<Category>> Create(CategoryUpsertRequest request)
    {
        var category = new Category
        {
            Name = request.Name,
            Code = request.Code,
            Description = request.Description,
            KeyWords = request.KeyWords,
            SortOrder = request.SortOrder,
            SampleInd = request.SampleInd
        };

        category.CategoryId = 0;
        category.CreatedBy = User.Identity?.Name ?? "api-user";
        category.CreatedDate = DateTime.UtcNow;

        context.Categories.Add(category);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = category.CategoryId }, category);
    }

    [HttpPut("{id:int}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> Update(int id, CategoryUpsertRequest request)
    {
        var category = await context.Categories.FindAsync(id);
        if (category is null || category.DeletedInd)
        {
            return NotFound();
        }

        category.Name = request.Name;
        category.Code = request.Code;
        category.Description = request.Description;
        category.KeyWords = request.KeyWords;
        category.SortOrder = request.SortOrder;
        category.SampleInd = request.SampleInd;
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