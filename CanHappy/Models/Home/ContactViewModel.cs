using System.ComponentModel.DataAnnotations;

namespace CanHappy.Models.Home;

public class ContactViewModel
{
    [Required]
    [StringLength(120)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [StringLength(2000)]
    public string Body { get; set; } = string.Empty;
}
