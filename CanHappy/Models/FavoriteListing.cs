using System.ComponentModel.DataAnnotations;

namespace CanHappy.Models;

public class FavoriteListing
{
    [Key]
    public Guid FavoriteListingGUID { get; set; } = Guid.NewGuid();

    public Guid ListingGUID { get; set; }
    public Guid UserId { get; set; }

    [StringLength(200)]
    public string ListingURL { get; set; } = string.Empty;

    [StringLength(50)]
    public string ListingSubject { get; set; } = string.Empty;

    public int SortOrder { get; set; }
    public bool DeletedInd { get; set; }

    [StringLength(50)]
    public string CreatedBy { get; set; } = string.Empty;

    [StringLength(50)]
    public string ModifiedBy { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }

    public Listing? Listing { get; set; }
}
