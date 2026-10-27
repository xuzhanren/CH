using System.ComponentModel.DataAnnotations;

namespace CanHappy.Models;

public class EstateSaleDetail
{
    [Key]
    public Guid EstateSaleDetailGUID { get; set; } = Guid.NewGuid();

    public Guid ListingGUID { get; set; }

    public decimal? AnnualManagementFee { get; set; }

    public int EstitateTypeId { get; set; }

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

    public bool DeletedInd { get; set; }

    [StringLength(50)]
    public string? CreatedBy { get; set; }

    [StringLength(50)]
    public string? ModifiedBy { get; set; }

    public DateTime CreatedDate { get; set; } = CanHappy.Common.EasternTime.Now;

    public DateTime? ModifiedDate { get; set; }

    public Listing? Listing { get; set; }

    public EstateType? EstateType { get; set; }

    public List<EstateRoom> EstateRooms { get; set; } = [];
}
