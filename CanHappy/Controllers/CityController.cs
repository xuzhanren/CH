using CanHappy.Data;
using CanHappy.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CanHappy.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CityController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<City>>> GetAll()
    {
        return Ok(await context.Cities
            .AsNoTracking()
            .Where(c => !c.DeletedInd)
            .OrderBy(c => c.Name)
            .ToListAsync());
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<City>> GetById(int id)
    {
        var city = await context.Cities.FindAsync(id);
        if (city is null || city.DeletedInd)
        {
            return NotFound();
        }

        return Ok(city);
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<City>> Create(City city)
    {
        city.CityId = 0;
        city.CreatedBy = User.Identity?.Name ?? "api-user";
        city.CreatedDate = DateTime.UtcNow;
        city.ModifiedBY = null;
        city.ModifiedDate = null;
        city.DeletedInd = false;

        context.Cities.Add(city);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = city.CityId }, city);
    }

    [HttpPut("{id:int}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> Update(int id, City updatedCity)
    {
        var city = await context.Cities.FindAsync(id);
        if (city is null || city.DeletedInd)
        {
            return NotFound();
        }

        city.ProvinceId = updatedCity.ProvinceId;
        city.Name = updatedCity.Name;
        city.Code = updatedCity.Code;
        city.Description = updatedCity.Description;
        city.SampleInd = updatedCity.SampleInd;
        city.ModifiedBY = User.Identity?.Name ?? "api-user";
        city.ModifiedDate = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> Delete(int id)
    {
        var city = await context.Cities.FindAsync(id);
        if (city is null)
        {
            return NotFound();
        }

        city.DeletedInd = true;
        city.ModifiedBY = User.Identity?.Name ?? "api-user";
        city.ModifiedDate = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return NoContent();
    }
}
