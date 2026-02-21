using CanHappy.Data;
using CanHappy.Models;
using CanHappy.Models.Api;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CanHappy.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class SubcategoryController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Subcategory>>> GetAll()
    {
        return Ok(await context.Subcategories
            .Where(s => !s.DeletedInd)
            .OrderBy(s => s.SortOrder)
            .ToListAsync());
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<Subcategory>> GetById(int id)
    {
        var subcategory = await context.Subcategories.FindAsync(id);
        if (subcategory is null || subcategory.DeletedInd)
        {
            return NotFound();
        }
        return Ok(subcategory);
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<Subcategory>> Create(SubcategoryUpsertRequest request)
    {
        var subcategory = new Subcategory
        {
            CategoryId = request.CategoryId,
            Name = request.Name,
            Code = request.Code,
            Description = request.Description,
            KeyWords = request.KeyWords,
            SortOrder = request.SortOrder,
            SampleInd = request.SampleInd,
            CreatedBy = User.Identity?.Name ?? "api-user",
            CreatedDate = DateTime.UtcNow
        };
        context.Subcategories.Add(subcategory);
        await context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = subcategory.SubcategoryId }, subcategory);
    }

    [HttpPut("{id:int}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> Update(int id, SubcategoryUpsertRequest request)
    {
        var subcategory = await context.Subcategories.FindAsync(id);
        if (subcategory is null || subcategory.DeletedInd)
        {
            return NotFound();
        }
        subcategory.CategoryId = request.CategoryId;
        subcategory.Name = request.Name;
        subcategory.Code = request.Code;
        subcategory.Description = request.Description;
        subcategory.KeyWords = request.KeyWords;
        subcategory.SortOrder = request.SortOrder;
        subcategory.SampleInd = request.SampleInd;
        subcategory.ModifiedBY = User.Identity?.Name ?? "api-user";
        subcategory.ModifiedDate = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> Delete(int id)
    {
        var subcategory = await context.Subcategories.FindAsync(id);
        if (subcategory is null)
        {
            return NotFound();
        }
        subcategory.DeletedInd = true;
        subcategory.ModifiedBY = User.Identity?.Name ?? "api-user";
        subcategory.ModifiedDate = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return NoContent();
    }
}
