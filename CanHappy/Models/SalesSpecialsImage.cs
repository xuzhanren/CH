using System.ComponentModel.DataAnnotations;

namespace CanHappy.Models;

public class SalesSpecialsImage
{
    [Key]
    public Guid SalesSpecialsImageGUID { get; set; } = Guid.NewGuid();

    public Guid ListingGUID { get; set; }

    [StringLength(50)]
    public string? Title { get; set; }

    public int SortOrder { get; set; }

    public decimal? Price { get; set; }

    public decimal? SalePrice { get; set; }

    [StringLength(10)]
    public string? PercentOff { get; set; }

    public DateTime? SaleBegin { get; set; }

    public DateTime? SaleEnd { get; set; }

    [StringLength(200)]
    public string? Description { get; set; }

    [StringLength(200)]
    public string? ThumbnailURL { get; set; }

    [StringLength(200)]
    public string? ImageURL { get; set; }

    public bool DeletedInd { get; set; }

    public bool SampleInd { get; set; }

    [StringLength(50)]
    public string? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; } = CanHappy.Common.EasternTime.Now;

    [StringLength(50)]
    public string? ModifiedBy { get; set; }

    public DateTime? ModifiedDate { get; set; }

    public Listing? Listing { get; set; }
}