using CanHappy.Data;
using CanHappy.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CanHappy.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProvinceController(ApplicationDbContext context) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Province>>> GetAll()
    {
        return Ok(await context.Provinces
            .AsNoTracking()
            .Where(p => !p.DeletedInd)
            .OrderBy(p => p.Name)
            .ToListAsync());
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<Province>> GetById(int id)
    {
        var province = await context.Provinces.FindAsync(id);
        if (province is null || province.DeletedInd)
        {
            return NotFound();
        }

        return Ok(province);
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<Province>> Create(Province province)
    {
        province.ProvinceId = 0;
        province.CreatedBy = User.Identity?.Name ?? "api-user";
        province.CreatedDate = CanHappy.Common.EasternTime.Now;
        province.ModifiedBY = null;
        province.ModifiedDate = null;
        province.DeletedInd = false;

        context.Provinces.Add(province);
        await context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = province.ProvinceId }, province);
    }

    [HttpPut("{id:int}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> Update(int id, Province updatedProvince)
    {
        var province = await context.Provinces.FindAsync(id);
        if (province is null || province.DeletedInd)
        {
            return NotFound();
        }

        province.CountryId = updatedProvince.CountryId;
        province.Name = updatedProvince.Name;
        province.Code = updatedProvince.Code;
        province.Description = updatedProvince.Description;
        province.SampleInd = updatedProvince.SampleInd;
        province.ModifiedBY = User.Identity?.Name ?? "api-user";
        province.ModifiedDate = CanHappy.Common.EasternTime.Now;

        await context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<IActionResult> Delete(int id)
    {
        var province = await context.Provinces.FindAsync(id);
        if (province is null)
        {
            return NotFound();
        }

        province.DeletedInd = true;
        province.ModifiedBY = User.Identity?.Name ?? "api-user";
        province.ModifiedDate = CanHappy.Common.EasternTime.Now;

        await context.SaveChangesAsync();
        return NoContent();
    }
}

