using CanHappy.Data;
using CanHappy.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CanHappy.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CountryController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Country>>> GetAll()
    {
        return Ok(await context.Countries
            .AsNoTracking()
            .Where(c => !c.DeletedInd)
            .OrderBy(c => c.Name)
            .ToListAsync());
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<Country>> GetById(int id)
    {
        var country = await context.Countries.FindAsync(id);
        if (country is null || country.DeletedInd)
        {
            return NotFound();
        }

        return Ok(country);
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<Country>> Create(Country country)
    {
        country.CountryId = 0;
        country.CreatedBy = User.Identity?.Name ?? "api-user";
        country.CreatedDate = DateTime.UtcNow;
        country.ModifiedBY = null;
        country.ModifiedDate = null;
        country.DeletedInd = false;

        context.Countries.Add(country);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = country.CountryId }, country);
    }

    [HttpPut("{id:int}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> Update(int id, Country updatedCountry)
    {
        var country = await context.Countries.FindAsync(id);
        if (country is null || country.DeletedInd)
        {
            return NotFound();
        }

        country.Name = updatedCountry.Name;
        country.Code = updatedCountry.Code;
        country.Description = updatedCountry.Description;
        country.SampleInd = updatedCountry.SampleInd;
        country.ModifiedBY = User.Identity?.Name ?? "api-user";
        country.ModifiedDate = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> Delete(int id)
    {
        var country = await context.Countries.FindAsync(id);
        if (country is null)
        {
            return NotFound();
        }

        country.DeletedInd = true;
        country.ModifiedBY = User.Identity?.Name ?? "api-user";
        country.ModifiedDate = DateTime.UtcNow;

        await context.SaveChangesAsync();
        return NoContent();
    }
}
