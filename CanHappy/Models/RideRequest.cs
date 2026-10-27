using System.ComponentModel.DataAnnotations;

namespace CanHappy.Models;

public class RideRequest
{
    [Key]
    public Guid RideRequestGUID { get; set; } = Guid.NewGuid();

    public Guid CarPoolDetailGUID { get; set; }

    public int RideRequestStatusId { get; set; } = 1;

    public Guid RiderUserID { get; set; }

    [StringLength(200)]
    public string? RequestMessage { get; set; }

    public bool DeletedInd { get; set; }

    [StringLength(50)]
    public string? CreatedBy { get; set; }

    [StringLength(50)]
    public string? ModifiedBy { get; set; }

    public DateTime CreatedDate { get; set; } = CanHappy.Common.EasternTime.Now;

    public DateTime? ModifiedDate { get; set; }

    public CarPoolDetail? CarPoolDetail { get; set; }

    public RideRequestStatus? RideRequestStatus { get; set; }
}
