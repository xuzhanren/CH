using System.ComponentModel.DataAnnotations;

namespace CanHappy.Models;

public class BuySellDetail
{
    [Key]
    public Guid BuySellDetailGUID { get; set; } = Guid.NewGuid();

    public Guid ListingGUID { get; set; }

    [StringLength(50)]
    public string? Model { get; set; }

    [StringLength(30)]
    public string? PostalCode { get; set; }

    [StringLength(50)]
    public string? Material { get; set; }

    public bool NegotiablePriceInd { get; set; }

    public bool DeliveryAvailableInd { get; set; }

    public bool PickupAvailableInd { get; set; }

    [StringLength(100)]
    public string? WarrantyInfo { get; set; }

    [StringLength(500)]
    public string? AdditionalDetails { get; set; }

    [StringLength(30)]
    public string? PickupTime { get; set; }

    [StringLength(50)]
    public string? PickupLocation { get; set; }

    public bool DeletedInd { get; set; }

    [StringLength(100)]
    public string? CreatedBy { get; set; }

    [StringLength(100)]
    public string? ModifiedBY { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedDate { get; set; }

    public Listing? Listing { get; set; }
}