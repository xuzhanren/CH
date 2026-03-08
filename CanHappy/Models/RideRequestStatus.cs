using System.ComponentModel.DataAnnotations;

namespace CanHappy.Models;

public class RideRequestStatus
{
    [Key]
    public int RideRequestStatusId { get; set; }

    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Description { get; set; }

    public bool DeletedInd { get; set; }

    [StringLength(50)]
    public string? CreatedBy { get; set; }

    [StringLength(50)]
    public string? ModifiedBy { get; set; }

    public DateTime CreatedDate { get; set; } = CanHappy.Common.EasternTime.Now;

    public DateTime? ModifiedDate { get; set; }

    public List<RideRequest> RideRequests { get; set; } = [];
}
