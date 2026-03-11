using System.ComponentModel.DataAnnotations;

namespace CanHappy.Models;

public class CarPoolDetailIndexPageViewModel
{
    public Guid? ListingGUIDFilter { get; set; }
    public bool MineOnly { get; set; }
    public CarPoolListingCardViewModel? ListingCard { get; set; }
    public CarPoolDetailEditViewModel? FocusDetail { get; set; }
    public string? FocusCarPoolTypeName { get; set; }
    public string? FocusCarPoolStatusName { get; set; }
    public bool CanManageFocusedListing { get; set; }
    public bool HasExistingFocusDetail { get; set; }
    public List<CarPoolLookupOptionViewModel> CarPoolTypes { get; set; } = [];
    public List<CarPoolLookupOptionViewModel> CarPoolStatuses { get; set; } = [];
    public List<CarPoolRelatedRideRequestViewModel> RelatedRideRequests { get; set; } = [];
    public List<BuySellDetailListingImageViewModel> ListingImages { get; set; } = [];
    public List<CarPoolDetailIndexItemViewModel> Items { get; set; } = [];
}

public class CarPoolRelatedRideRequestViewModel
{
    public Guid RideRequestGUID { get; set; }
    public Guid RiderUserID { get; set; }
    public string? RiderName { get; set; }
    public string? RequestMessage { get; set; }
    public string? RideRequestStatusName { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class CarPoolLookupOptionViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class CarPoolListingCardViewModel
{
    public Guid ListingGUID { get; set; }
    public Guid UserId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? KeyWords { get; set; }
    public string? CategoryName { get; set; }
    public string? SubcategoryName { get; set; }
    public string? ProvinceName { get; set; }
    public string? CityName { get; set; }
    public string? Address { get; set; }
    public string? PostalCode { get; set; }
    public decimal? Price { get; set; }
    public int Quantity { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactName { get; set; }
    public bool ShowContactInd { get; set; }
}

public class CarPoolDetailIndexItemViewModel
{
    public Guid CarPoolDetailGUID { get; set; }
    public Guid ListingGUID { get; set; }
    public Guid ListingUserId { get; set; }

    public string Subject { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public string? FromCity { get; set; }
    public string? DestinationCity { get; set; }
    public DateTime? LeavingDate { get; set; }
    public string? LeavingTime { get; set; }
    public string? CarPoolTypeName { get; set; }
    public string? CarPoolStatusName { get; set; }

    public bool CanManage { get; set; }
}

public class CarPoolDetailEditViewModel
{
    public Guid? CarPoolDetailGUID { get; set; }

    [Required]
    public Guid ListingGUID { get; set; }

    [Required]
    public int CarPoolTypeId { get; set; } = 1;

    [Required]
    public int CarPoolStatusId { get; set; } = 1;

    [StringLength(50)]
    public string? FromCity { get; set; }

    [DataType(DataType.Date)]
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
}
