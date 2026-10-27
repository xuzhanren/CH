using System.ComponentModel.DataAnnotations;

namespace CanHappy.Models;

public class CarPoolDetail
{
    [Key]
    public Guid CarPoolDetailGUID { get; set; } = Guid.NewGuid();

    public Guid ListingGUID { get; set; }

    public int CarPoolTypeId { get; set; } = 1;

    public int CarPoolStatusId { get; set; } = 1;

    [StringLength(50)]
    public string? FromCity { get; set; }

    public DateTime? LeavingDate { get; set; }

    [StringLength(50)]
    public string? WeekDays { get; set; }

    [StringLength(50)]
    public string? LeavingTime { get; set; }

    [StringLength(100)]
    public string? PickupLocation { get; set; }

    [StringLength(50)]
    public string? DestinationCity { get; set; }

    [StringLength(100)]
    public string? DropoffLocation { get; set; }

    [StringLength(100)]
    public string? TripStops { get; set; }

    [StringLength(50)]
    public string? VehicleModelYear { get; set; }

    [StringLength(50)]
    public string? VehicleLicensePlateNumber { get; set; }

    [StringLength(50)]
    public string? VehicleColor { get; set; }

    public bool WeeklyScheduleInd { get; set; }

    public bool SmallBagAllowedInd { get; set; }

    public bool MediumBagAllowedInd { get; set; }

    public bool OneLargeBagAllowedInd { get; set; }

    [StringLength(200)]
    public string? AdditionalInfo { get; set; }

    public bool DeletedInd { get; set; }

    [StringLength(50)]
    public string? CreatedBy { get; set; }

    [StringLength(50)]
    public string? ModifiedBy { get; set; }

    public DateTime CreatedDate { get; set; } = CanHappy.Common.EasternTime.Now;

    public DateTime? ModifiedDate { get; set; }

    public Listing? Listing { get; set; }

    public CarPoolType? CarPoolType { get; set; }

    public CarPoolStatus? CarPoolStatus { get; set; }

    public List<RideRequest> RideRequests { get; set; } = [];
}
