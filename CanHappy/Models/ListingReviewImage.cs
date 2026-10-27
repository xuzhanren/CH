using System.ComponentModel.DataAnnotations;

namespace CanHappy.Models;

public class ListingReviewImage
{
    [Key]
    public Guid ListingReviewImageGUID { get; set; } = Guid.NewGuid();

    public Guid ListingReviewGUID { get; set; }

    [StringLength(100)]
    public string? Title { get; set; }

    public int SortOrder { get; set; }

    [StringLength(200)]
    public string? ThumbnailURL { get; set; }

    [StringLength(200)]
    public string? ImageURL { get; set; }

    public bool DeletedInd { get; set; }

    [StringLength(50)]
    public string? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; } = CanHappy.Common.EasternTime.Now;

    public ListingReview? ListingReview { get; set; }
}
