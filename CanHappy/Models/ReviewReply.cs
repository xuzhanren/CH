using System.ComponentModel.DataAnnotations;

namespace CanHappy.Models;

public class ReviewReply
{
    [Key]
    public Guid ReviewReplyGUID { get; set; } = Guid.NewGuid();

    public Guid ListingReviewGUID { get; set; }

    [StringLength(500)]
    public string? ReplyMessage { get; set; }

    [StringLength(30)]
    public string Status { get; set; } = ReviewWorkflowStatus.Submitted;

    public bool OfficialReplyInd { get; set; }

    public bool DeletedInd { get; set; }

    [StringLength(50)]
    public string? CreatedBy { get; set; }

    [StringLength(50)]
    public string? ModifiedBy { get; set; }

    public DateTime CreatedDate { get; set; } = CanHappy.Common.EasternTime.Now;

    public DateTime? ModifiedDate { get; set; }

    public ListingReview? ListingReview { get; set; }
}
