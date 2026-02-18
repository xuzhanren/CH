using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CanHappy.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class RegisterModel(
    UserManager<IdentityUser> userManager,
    SignInManager<IdentityUser> signInManager,
    IUserStore<IdentityUser> userStore,
    RoleManager<IdentityRole> roleManager,
    ILogger<RegisterModel> logger) : PageModel
{
    private const string RegisteredUserRoleName = "RegisteredUser";

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    public class InputModel
    {
        [Required]
        [StringLength(256)]
        [Display(Name = "User Name")]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = new IdentityUser();
        await userStore.SetUserNameAsync(user, Input.UserName, CancellationToken.None);
        var emailStore = GetEmailStore();
        await emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

        var createResult = await userManager.CreateAsync(user, Input.Password);
        if (!createResult.Succeeded)
        {
            AddErrors(createResult);
            return Page();
        }

        if (!await roleManager.RoleExistsAsync(RegisteredUserRoleName))
        {
            var createRoleResult = await roleManager.CreateAsync(new IdentityRole(RegisteredUserRoleName));
            if (!createRoleResult.Succeeded)
            {
                AddErrors(createRoleResult);
                await userManager.DeleteAsync(user);
                return Page();
            }
        }

        var addRoleResult = await userManager.AddToRoleAsync(user, RegisteredUserRoleName);
        if (!addRoleResult.Succeeded)
        {
            AddErrors(addRoleResult);
            await userManager.DeleteAsync(user);
            return Page();
        }

        logger.LogInformation("User created a new account with password and assigned role RegisteredUser.");

        await signInManager.SignInAsync(user, isPersistent: false);
        return LocalRedirect(returnUrl);
    }

    private IUserEmailStore<IdentityUser> GetEmailStore()
    {
        if (!userManager.SupportsUserEmail)
        {
            throw new NotSupportedException("The default UI requires a user store with email support.");
        }

        return (IUserEmailStore<IdentityUser>)userStore;
    }

    private void AddErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }
}
