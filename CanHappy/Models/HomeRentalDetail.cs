using System.ComponentModel.DataAnnotations;

namespace CanHappy.Models;

public class HomeRentalDetail
{
    [Key]
    public Guid HomeRentalDetailGUID { get; set; } = Guid.NewGuid();

    public Guid ListingGUID { get; set; }

    public int PropertyTypeId { get; set; }

    public int NumberOfStoreys { get; set; }

    public bool PriceNegotiableInd { get; set; }

    public int RentalPropertyTypeId { get; set; }

    public int RentalSquareFeet { get; set; }

    public int Bedrooms { get; set; }

    public int BedroomsForRental { get; set; }

    public int Washrooms { get; set; }

    public bool SharedWashroomInd { get; set; } = true;

    public int Baths { get; set; }

    public int YearBuilt { get; set; }

    public int ParkingSpots { get; set; }

    public int RentalParkingSpots { get; set; }

    public bool ParkingIncludedInd { get; set; }

    public DateTime? PreferredRentalStartDate { get; set; }

    [StringLength(30)]
    public string? PreferredRentalTerm { get; set; }

    public bool RentalTermNegotiableInd { get; set; }

    public bool FurnishedInd { get; set; }

    public bool WaterIncludedInd { get; set; }

    public bool HeatingIncludedInd { get; set; }

    public bool HydroElectricityIncludedInd { get; set; }

    public bool InternetWiFiIncludedInd { get; set; }

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

    public PropertyType? PropertyType { get; set; }

    public PropertyType? RentalPropertyType { get; set; }
}