using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CanHappy.Data;

public static class DbInitializer
{
    private const string AdminRoleName = "Admin";
    private const string RegisteredUserRoleName = "RegisteredUser";

    public static async Task InitializeAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();
        await EnsureRoleExistsAsync(context, RegisteredUserRoleName);
        await SeedAdminRoleAssignmentAsync(context, services);
    }

    private static async Task SeedAdminRoleAssignmentAsync(ApplicationDbContext context, IServiceProvider services)
    {
        var adminRole = await EnsureRoleExistsAsync(context, AdminRoleName);

        var configuration = services.GetService<IConfiguration>();
        var configuredAdminEmail = configuration?["SeedAdmin:Email"];

        IdentityUser? adminUser = null;

        if (!string.IsNullOrWhiteSpace(configuredAdminEmail))
        {
            var normalizedEmail = configuredAdminEmail.Trim().ToUpperInvariant();
            adminUser = await context.Users.FirstOrDefaultAsync(user => user.NormalizedEmail == normalizedEmail);
        }

        adminUser ??= await context.Users.OrderBy(user => user.Id).FirstOrDefaultAsync();
        if (adminUser is null)
        {
            return;
        }

        var alreadyMapped = await context.UserRoles
            .AnyAsync(userRole => userRole.UserId == adminUser.Id && userRole.RoleId == adminRole.Id);

        if (alreadyMapped)
        {
            return;
        }

        context.UserRoles.Add(new IdentityUserRole<string>
        {
            UserId = adminUser.Id,
            RoleId = adminRole.Id
        });

        await context.SaveChangesAsync();
    }

    private static async Task<IdentityRole> EnsureRoleExistsAsync(ApplicationDbContext context, string roleName)
    {
        var role = await context.Roles.FirstOrDefaultAsync(item => item.Name == roleName);
        if (role is not null)
        {
            return role;
        }

        role = new IdentityRole(roleName)
        {
            NormalizedName = roleName.ToUpperInvariant()
        };

        context.Roles.Add(role);
        await context.SaveChangesAsync();
        return role;
    }
}
