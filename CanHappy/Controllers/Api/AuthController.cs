using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CanHappy.Models.Api;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace CanHappy.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    UserManager<IdentityUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IConfiguration configuration) : ControllerBase
{
    private const string RegisteredUserRoleName = "RegisteredUser";

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var user = new IdentityUser
        {
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors.Select(e => e.Description));
        }

        if (!await roleManager.RoleExistsAsync(RegisteredUserRoleName))
        {
            var createRoleResult = await roleManager.CreateAsync(new IdentityRole(RegisteredUserRoleName));
            if (!createRoleResult.Succeeded)
            {
                return BadRequest(createRoleResult.Errors.Select(e => e.Description));
            }
        }

        var addRoleResult = await userManager.AddToRoleAsync(user, RegisteredUserRoleName);
        if (!addRoleResult.Succeeded)
        {
            return BadRequest(addRoleResult.Errors.Select(e => e.Description));
        }

        return Ok();
    }

    [HttpPost("token")]
    public async Task<ActionResult<TokenResponse>> Token(LoginRequest request)
    {
        var user = await userManager.FindByEmailAsync(request.Email);
        if (user is null || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            return Unauthorized();
        }

        var jwtKey = configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT signing key is missing.");
        var jwtIssuer = configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT issuer is missing.");
        var jwtAudience = configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT audience is missing.");
        var expiresMinutes = configuration.GetValue("Jwt:TokenExpirationMinutes", 60);
        var expiresAt = DateTime.UtcNow.AddMinutes(expiresMinutes);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? user.Email ?? string.Empty),
            new(ClaimTypes.Email, user.Email ?? string.Empty)
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        var token = new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
        return Ok(new TokenResponse(token, expiresAt));
    }
}