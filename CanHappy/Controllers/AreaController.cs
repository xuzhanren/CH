using CanHappy.Data;
using CanHappy.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CanHappy.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AreaController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Area>>> GetAll()
    {
        return Ok(await context.Areas
            .AsNoTracking()
            .Where(a => !a.DeletedInd)
            .OrderBy(a => a.Name)
            .ToListAsync());
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<Area>> GetById(int id)
    {
        var area = await context.Areas.FindAsync(id);
        if (area is null || area.DeletedInd)
        {
            return NotFound();
        }

        return Ok(area);
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<Area>> Create(Area area)
    {
        area.AreaId = 0;
        area.CreatedBy = User.Identity?.Name ?? "api-user";
        area.CreatedDate = DateTime.UtcNow;
        area.ModifiedBY = null;
        area.ModifiedDate = null;
        area.DeletedInd = false;

        context.Areas.Add(area);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = area.AreaId }, area);
    }

    [HttpPut("{id:int}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> Update(int id, Area updatedArea)
    {
        var area = await context.Areas.FindAsync(id);
        if (area is null || area.DeletedInd)
        {
            return NotFound();
        }

        area.CityId = updatedArea.CityId;
        area.Name = updatedArea.Name;
        area.Code = updatedArea.Code;
        area.Description = updatedArea.Description;
        area.SampleInd = updatedArea.SampleInd;
        area.ModifiedBY = User.Identity?.Name ?? "api-user";
        area.ModifiedDate = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> Delete(int id)
    {
        var area = await context.Areas.FindAsync(id);
        if (area is null)
        {
            return NotFound();
        }

        area.DeletedInd = true;
        area.ModifiedBY = User.Identity?.Name ?? "api-user";
        area.ModifiedDate = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return NoContent();
    }
}
