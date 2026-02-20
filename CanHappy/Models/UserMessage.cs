using System.ComponentModel.DataAnnotations;

namespace CanHappy.Models;

public class UserMessage
{
    [Key]
    public Guid UserMessageGUID { get; set; } = Guid.NewGuid();

    public Guid ListingGUID { get; set; }

    public Guid SenderUserId { get; set; }

    public Guid RecipientUserId { get; set; }

    [Required]
    [StringLength(120)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [StringLength(2000)]
    public string Body { get; set; } = string.Empty;

    public bool IsRead { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public Listing? Listing { get; set; }
}
