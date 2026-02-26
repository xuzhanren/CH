using System.ComponentModel.DataAnnotations;

namespace CanHappy.Models;

public class BuySellDetailIndexPageViewModel
{
    public Guid? ListingGUIDFilter { get; set; }
    public BuySellDetailListingCardViewModel? ListingCard { get; set; }
    public BuySellDetailEditViewModel? FocusDetail { get; set; }
    public bool CanManageFocusedListing { get; set; }
    public bool HasExistingFocusDetail { get; set; }
    public List<BuySellDetailListingImageViewModel> ListingImages { get; set; } = [];
    public List<BuySellDetailIndexItemViewModel> Items { get; set; } = [];
}

public class BuySellDetailListingCardViewModel
{
    public Guid ListingGUID { get; set; }
    public Guid UserId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public int SubcategoryId { get; set; }
    public int ProvinceId { get; set; }
    public int CityId { get; set; }
    public string? KeyWords { get; set; }
    public string? CategoryName { get; set; }
    public string? SubcategoryName { get; set; }
    public string? ProvinceName { get; set; }
    public string? CityName { get; set; }
    public string? PostalCode { get; set; }
    public decimal? Price { get; set; }
    public decimal DiscountPercent { get; set; }
    public DateTime? DiscountEndDate { get; set; }
    public string? Brand { get; set; }
    public string Condition { get; set; } = "Used";
    public int? ManufactureYear { get; set; }
    public string? Model { get; set; }
    public int Quantity { get; set; }
}

public class BuySellDetailListingImageViewModel
{
    public Guid ListingImageGUID { get; set; }
    public Guid ListingGUID { get; set; }
    public string? Title { get; set; }
    public int SorOrder { get; set; }
    public string? ThumbnailURL { get; set; }
    public string? ImageURL { get; set; }
}

public class BuySellDetailIndexItemViewModel
{
    public Guid BuySellDetailGUID { get; set; }
    public Guid ListingGUID { get; set; }

    public string Subject { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public int SubcategoryId { get; set; }
    public int ProvinceId { get; set; }
    public int CityId { get; set; }
    public decimal? Price { get; set; }
    public string? Brand { get; set; }
    public string Condition { get; set; } = "Used";
    public int? ManufactureYear { get; set; }
    public string? ListingModel { get; set; }
    public int Quantity { get; set; }

    public string? BuySellModel { get; set; }
    public string? Material { get; set; }
    public bool NegotiablePriceInd { get; set; }
    public bool DeliveryAvailableInd { get; set; }
    public bool PickupAvailableInd { get; set; }
    public string? PickupTime { get; set; }
    public string? PickupLocation { get; set; }
    public string? WarrantyInfo { get; set; }
    public string? AdditionalDetails { get; set; }

    public bool CanManage { get; set; }
}

public class BuySellDetailEditViewModel
{
    public Guid? BuySellDetailGUID { get; set; }

    [Required]
    public Guid ListingGUID { get; set; }

    public string Subject { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public int SubcategoryId { get; set; }
    public int ProvinceId { get; set; }
    public int CityId { get; set; }
    public decimal? Price { get; set; }
    public string? Brand { get; set; }
    public string Condition { get; set; } = "Used";
    public int? ManufactureYear { get; set; }
    public string? ListingModel { get; set; }
    public int Quantity { get; set; }

    [StringLength(50)]
    [Display(Name = "Model")]
    public string? BuySellModel { get; set; }

    [StringLength(50)]
    public string? Material { get; set; }

    public bool NegotiablePriceInd { get; set; }
    public bool DeliveryAvailableInd { get; set; }
    public bool PickupAvailableInd { get; set; }

    [StringLength(30)]
    public string? PickupTime { get; set; }

    [StringLength(50)]
    public string? PickupLocation { get; set; }

    [StringLength(100)]
    public string? WarrantyInfo { get; set; }

    [StringLength(500)]
    public string? AdditionalDetails { get; set; }
}

public class CarVehicleDetailIndexPageViewModel
{
    public Guid? ListingGUIDFilter { get; set; }
    public BuySellDetailListingCardViewModel? ListingCard { get; set; }
    public CarVehicleDetailEditViewModel? FocusDetail { get; set; }
    public bool CanManageFocusedListing { get; set; }
    public bool HasExistingFocusDetail { get; set; }
    public List<BuySellDetailListingImageViewModel> ListingImages { get; set; } = [];
    public List<CarVehicleDetailIndexItemViewModel> Items { get; set; } = [];
}

public class CarVehicleDetailIndexItemViewModel
{
    public Guid CarVehicleDetailGUID { get; set; }
    public Guid ListingGUID { get; set; }

    public string Subject { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public int SubcategoryId { get; set; }
    public int ProvinceId { get; set; }
    public int CityId { get; set; }
    public decimal? Price { get; set; }
    public string? Brand { get; set; }
    public string Condition { get; set; } = "Used";
    public int? ManufactureYear { get; set; }
    public string? ListingModel { get; set; }
    public int Quantity { get; set; }

    public int Kilometers { get; set; }
    public int Doors { get; set; }
    public int Seats { get; set; }
    public string? BodyStyle { get; set; }
    public string? Engine { get; set; }
    public string? ExteriorColor { get; set; }
    public string? InteriorColor { get; set; }
    public string? Transmission { get; set; }
    public string? Drivetrain { get; set; }
    public string? FuelType { get; set; }
    public string? SellerType { get; set; }
    public bool LeatherSeatsInd { get; set; }
    public bool BackupCameraInd { get; set; }
    public bool AlloyWheelsInd { get; set; }
    public bool BluetoothInd { get; set; }
    public bool HeatedSeatsInd { get; set; }
    public bool CarPlayInd { get; set; }
    public bool AndroidAutoInd { get; set; }
    public bool NavigationMapInd { get; set; }
    public bool RemoteStartInd { get; set; }
    public bool SunroofInd { get; set; }
    public bool MoonroofInd { get; set; }
    public bool BlindSpotMonitoringInd { get; set; }
    public bool LaneTrackingInd { get; set; }
    public bool AdaptiveCruiseInd { get; set; }
    public bool AssistedParkingCameraInd { get; set; }

    public bool CanManage { get; set; }
}

public class CarVehicleDetailEditViewModel
{
    public Guid? CarVehicleDetailGUID { get; set; }

    [Required]
    public Guid ListingGUID { get; set; }

    public string Subject { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public int SubcategoryId { get; set; }
    public int ProvinceId { get; set; }
    public int CityId { get; set; }
    public decimal? Price { get; set; }
    public string? Brand { get; set; }
    public string Condition { get; set; } = "Used";
    public int? ManufactureYear { get; set; }
    public string? ListingModel { get; set; }
    public int Quantity { get; set; }

    public int Kilometers { get; set; }
    public int Doors { get; set; }
    public int Seats { get; set; }

    [StringLength(30)]
    public string? BodyStyle { get; set; }

    [StringLength(30)]
    public string? Engine { get; set; }

    [StringLength(30)]
    public string? ExteriorColor { get; set; }

    [StringLength(30)]
    public string? InteriorColor { get; set; }

    [StringLength(30)]
    public string? Transmission { get; set; }

    [StringLength(30)]
    public string? Drivetrain { get; set; }

    [StringLength(30)]
    public string? FuelType { get; set; }

    [StringLength(30)]
    public string? SellerType { get; set; }

    public bool LeatherSeatsInd { get; set; }
    public bool BackupCameraInd { get; set; }
    public bool AlloyWheelsInd { get; set; }
    public bool BluetoothInd { get; set; }
    public bool HeatedSeatsInd { get; set; }
    public bool CarPlayInd { get; set; }
    public bool AndroidAutoInd { get; set; }
    public bool NavigationMapInd { get; set; }
    public bool RemoteStartInd { get; set; }
    public bool SunroofInd { get; set; }
    public bool MoonroofInd { get; set; }
    public bool BlindSpotMonitoringInd { get; set; }
    public bool LaneTrackingInd { get; set; }
    public bool AdaptiveCruiseInd { get; set; }
    public bool AssistedParkingCameraInd { get; set; }
}
