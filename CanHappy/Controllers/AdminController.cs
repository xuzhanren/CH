using CanHappy.Models.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CanHappy.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager) : Controller
{
    public async Task<IActionResult> Users()
    {
        var users = userManager.Users.OrderBy(user => user.UserName).ToList();
        var model = new List<AdminUserListItemViewModel>(users.Count);

        foreach (var user in users)
        {
            var userRoles = await userManager.GetRolesAsync(user);
            model.Add(new AdminUserListItemViewModel
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                EmailConfirmed = user.EmailConfirmed,
                Roles = userRoles.ToList()
            });
        }

        model = model.OrderBy(user => user.UserName).ToList();

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> CreateUser()
    {
        var model = new AdminUserEditViewModel();
        await PopulateRolesAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(AdminUserEditViewModel model)
    {
        await PopulateRolesAsync(model);
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = new IdentityUser
        {
            UserName = model.UserName,
            Email = model.Email,
            EmailConfirmed = model.EmailConfirmed
        };

        var createResult = await userManager.CreateAsync(user, model.Password!);
        if (!createResult.Succeeded)
        {
            AddIdentityErrors(createResult.Errors);
            return View(model);
        }

        var selectedRoles = model.SelectedRoleNames.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (selectedRoles.Length > 0)
        {
            var roleResult = await userManager.AddToRolesAsync(user, selectedRoles);
            if (!roleResult.Succeeded)
            {
                AddIdentityErrors(roleResult.Errors);
                return View(model);
            }
        }

        return RedirectToAction(nameof(Users));
    }

    [HttpGet]
    public async Task<IActionResult> EditUser(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var roles = await userManager.GetRolesAsync(user);
        var model = new AdminUserEditViewModel
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            EmailConfirmed = user.EmailConfirmed,
            SelectedRoleNames = roles.ToList()
        };

        await PopulateRolesAsync(model);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(AdminUserEditViewModel model)
    {
        await PopulateRolesAsync(model);
        if (string.IsNullOrWhiteSpace(model.Id))
        {
            return NotFound();
        }

        var user = await userManager.FindByIdAsync(model.Id);
        if (user is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        user.UserName = model.UserName;
        user.Email = model.Email;
        user.EmailConfirmed = model.EmailConfirmed;

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            AddIdentityErrors(updateResult.Errors);
            return View(model);
        }

        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            var removePasswordResult = await userManager.RemovePasswordAsync(user);
            if (!removePasswordResult.Succeeded)
            {
                AddIdentityErrors(removePasswordResult.Errors);
                return View(model);
            }

            var addPasswordResult = await userManager.AddPasswordAsync(user, model.Password);
            if (!addPasswordResult.Succeeded)
            {
                AddIdentityErrors(addPasswordResult.Errors);
                return View(model);
            }
        }

        var currentRoles = await userManager.GetRolesAsync(user);
        var desiredRoles = model.SelectedRoleNames.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        var removeRoles = currentRoles.Except(desiredRoles, StringComparer.OrdinalIgnoreCase).ToArray();
        if (removeRoles.Length > 0)
        {
            var removeRolesResult = await userManager.RemoveFromRolesAsync(user, removeRoles);
            if (!removeRolesResult.Succeeded)
            {
                AddIdentityErrors(removeRolesResult.Errors);
                return View(model);
            }
        }

        var addRoles = desiredRoles.Except(currentRoles, StringComparer.OrdinalIgnoreCase).ToArray();
        if (addRoles.Length > 0)
        {
            var addRolesResult = await userManager.AddToRolesAsync(user, addRoles);
            if (!addRolesResult.Succeeded)
            {
                AddIdentityErrors(addRolesResult.Errors);
                return View(model);
            }
        }

        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result.Errors);
            var users = userManager.Users.OrderBy(item => item.UserName).ToList();
            var model = new List<AdminUserListItemViewModel>(users.Count);

            foreach (var item in users)
            {
                var userRoles = await userManager.GetRolesAsync(item);
                model.Add(new AdminUserListItemViewModel
                {
                    Id = item.Id,
                    UserName = item.UserName,
                    Email = item.Email,
                    EmailConfirmed = item.EmailConfirmed,
                    Roles = userRoles.ToList()
                });
            }

            return View(nameof(Users), model);
        }

        return RedirectToAction(nameof(Users));
    }

    public IActionResult Roles()
    {
        var roles = roleManager.Roles
            .OrderBy(role => role.Name)
            .Select(role => new AdminRoleViewModel
            {
                Id = role.Id,
                Name = role.Name
            })
            .ToList();

        return View(roles);
    }

    [HttpGet]
    public IActionResult CreateRole()
    {
        return View(new AdminRoleEditViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateRole(AdminRoleEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var role = new IdentityRole(model.Name!.Trim());
        var result = await roleManager.CreateAsync(role);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result.Errors);
            return View(model);
        }

        return RedirectToAction(nameof(Roles));
    }

    [HttpGet]
    public async Task<IActionResult> EditRole(string id)
    {
        var role = await roleManager.FindByIdAsync(id);
        if (role is null)
        {
            return NotFound();
        }

        return View(new AdminRoleEditViewModel
        {
            Id = role.Id,
            Name = role.Name
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditRole(AdminRoleEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var role = await roleManager.FindByIdAsync(model.Id!);
        if (role is null)
        {
            return NotFound();
        }

        role.Name = model.Name!.Trim();
        role.NormalizedName = model.Name.Trim().ToUpperInvariant();

        var result = await roleManager.UpdateAsync(role);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result.Errors);
            return View(model);
        }

        return RedirectToAction(nameof(Roles));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteRole(string id)
    {
        var role = await roleManager.FindByIdAsync(id);
        if (role is null)
        {
            return NotFound();
        }

        var result = await roleManager.DeleteAsync(role);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result.Errors);
            return RedirectToAction(nameof(Roles));
        }

        return RedirectToAction(nameof(Roles));
    }

    private async Task PopulateRolesAsync(AdminUserEditViewModel model)
    {
        model.AvailableRoles = await roleManager.Roles
            .OrderBy(role => role.Name)
            .Select(role => role.Name!)
            .ToListAsync();
    }

    private void AddIdentityErrors(IEnumerable<IdentityError> errors)
    {
        foreach (var error in errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }
}