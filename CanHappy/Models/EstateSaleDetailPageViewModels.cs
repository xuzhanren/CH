using System.ComponentModel.DataAnnotations;

namespace CanHappy.Models;

public class EstateSaleDetailIndexPageViewModel
{
    public Guid? ListingGUIDFilter { get; set; }
    public BuySellDetailListingCardViewModel? ListingCard { get; set; }
    public EstateSaleDetailEditViewModel? FocusDetail { get; set; }
    public bool CanManageFocusedListing { get; set; }
    public bool HasExistingFocusDetail { get; set; }
    public List<BuySellDetailListingImageViewModel> ListingImages { get; set; } = [];
    public List<EstateRoomEditViewModel> Rooms { get; set; } = [];
}

public class EstateSaleDetailEditViewModel
{
    public Guid? EstateSaleDetailGUID { get; set; }

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
    public string? Address { get; set; }

    public int EstitateTypeId { get; set; }
    public string? EstitateTypeName { get; set; }

    public int YearBuilt { get; set; }
    public int SquareFeet { get; set; }
    public int LandSizeSqFt { get; set; }

    [StringLength(50)]
    public string? LandDimensionWxD { get; set; }

    public int NumberOfStoreys { get; set; }
    public bool PriceNegotiableInd { get; set; }
    public decimal? AnuualPropertyTax { get; set; }
    public int Bedrooms { get; set; }
    public int Washrooms { get; set; }
    public int Baths { get; set; }
    public int ParkingSpots { get; set; }

    [StringLength(30)]
    public string? ParkingType { get; set; }

    [StringLength(30)]
    public string? FoundationType { get; set; }

    [StringLength(30)]
    public string? HydroType { get; set; }

    [StringLength(30)]
    public string? WaterType { get; set; }

    [StringLength(30)]
    public string? SewerType { get; set; }

    [StringLength(50)]
    public string? ExternalStructures { get; set; }

    [StringLength(30)]
    public string? CoolingType { get; set; }

    [StringLength(30)]
    public string? HeatingType { get; set; }

    public bool WaterFrontInd { get; set; }
    public bool SoldByOwnerInd { get; set; }

    [StringLength(50)]
    public string? AppliancesIncluded { get; set; }

    public bool HasBasementInd { get; set; }
    public bool BasementFinishedInd { get; set; }

    [StringLength(50)]
    public string? RentalEquipment { get; set; }

    [StringLength(50)]
    public string? CommunityName { get; set; }

    public bool CloseToSchoolInd { get; set; }
    public bool CloseToDaycareInd { get; set; }
    public bool CloseToBusInd { get; set; }
    public bool CloseToShoppingCenterInd { get; set; }
    public bool FurnishedInd { get; set; }
    public bool HasFireplaceInd { get; set; }

    [StringLength(500)]
    public string? AdditionalInfo { get; set; }
}

public class EstateRoomEditViewModel
{
    public Guid? EstateRoomGUID { get; set; }
    public Guid EstateSaleDetailGUID { get; set; }

    [StringLength(50)]
    public string? RoomName { get; set; }

    [StringLength(30)]
    public string? RoomSizeFtxFt { get; set; }

    public int SortOrder { get; set; }

    public int OnFloorNumber { get; set; }

    public string? ThumbnailURL { get; set; }
    public string? ImageURL { get; set; }
}
