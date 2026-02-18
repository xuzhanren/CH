using System.ComponentModel.DataAnnotations;

namespace CanHappy.Models.Admin;

public class AdminRoleEditViewModel
{
    public string? Id { get; set; }

    [Required]
    [StringLength(256)]
    public string? Name { get; set; }
}