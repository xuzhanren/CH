using System.ComponentModel.DataAnnotations;

namespace CanHappy.Models.Admin;

public class AdminUserEditViewModel : IValidatableObject
{
    public string? Id { get; set; }

    [Required]
    [StringLength(256)]
    public string? UserName { get; set; }

    [EmailAddress]
    [StringLength(256)]
    public string? Email { get; set; }

    public bool EmailConfirmed { get; set; }

    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6)]
    public string? Password { get; set; }

    [DataType(DataType.Password)]
    [Compare(nameof(Password))]
    public string? ConfirmPassword { get; set; }

    public List<string> SelectedRoleNames { get; set; } = [];
    public List<string> AvailableRoles { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var isCreate = string.IsNullOrWhiteSpace(Id);
        if (isCreate && string.IsNullOrWhiteSpace(Password))
        {
            yield return new ValidationResult("Password is required when creating a user.", [nameof(Password)]);
        }
    }
}