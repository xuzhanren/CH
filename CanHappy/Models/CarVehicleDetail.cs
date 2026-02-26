using System.ComponentModel.DataAnnotations;

namespace CanHappy.Models;

public class CarVehicleDetail
{
    [Key]
    public Guid CarVehicleDetailGUID { get; set; } = Guid.NewGuid();

    public Guid ListingGUID { get; set; }

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

    public bool DeletedInd { get; set; }

    [StringLength(50)]
    public string? CreatedBy { get; set; }

    [StringLength(50)]
    public string? ModifiedBy { get; set; }

    public DateTime CreatedDate { get; set; } = CanHappy.Common.EasternTime.Now;

    public DateTime? ModifiedDate { get; set; }

    public Listing? Listing { get; set; }
}