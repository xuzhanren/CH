using System.ComponentModel.DataAnnotations;

namespace CanHappy.Models;

public class ListingReview
{
    [Key]
    public Guid ListingReviewGUID { get; set; } = Guid.NewGuid();

    public Guid ListingGUID { get; set; }

    public float Rating { get; set; }

    [StringLength(120)]
    public string? ReviewTitle { get; set; }

    [StringLength(500)]
    public string? ReviewMessage { get; set; }

    [StringLength(30)]
    public string Status { get; set; } = ReviewWorkflowStatus.Submitted;

    public bool VerifiedPurchaseInd { get; set; }

    public int HelpfulCount { get; set; }

    public int ReportedCount { get; set; }

    public bool DeletedInd { get; set; }

    [StringLength(50)]
    public string? CreatedBy { get; set; }

    [StringLength(50)]
    public string? ModifiedBy { get; set; }

    public DateTime CreatedDate { get; set; } = CanHappy.Common.EasternTime.Now;

    public DateTime? ModifiedDate { get; set; }

    public Listing? Listing { get; set; }

    public List<ListingReviewImage> Images { get; set; } = [];

    public List<ReviewReply> Replies { get; set; } = [];
}
