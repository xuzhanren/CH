using System.ComponentModel.DataAnnotations;

namespace CanHappy.Models;

public class Listing
{
    [Key]
    public Guid ListingGUID { get; set; } = Guid.NewGuid();

    public int CategoryId { get; set; }

    public int SubcategoryId { get; set; }

    [Required]
    [StringLength(50)]
    public string Subject { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(50)]
    public string? KeyWords { get; set; }

    public int ProvinceId { get; set; }

    public int CityId { get; set; }

    [StringLength(15)]
    public string? PostalCode { get; set; }

    [StringLength(200)]
    public string? ThumbnailURL { get; set; }

    public decimal? Price { get; set; }

    public decimal DiscountPercent { get; set; }

    public DateTime? DiscountBeginDate { get; set; }

    public DateTime? DiscountEndDate { get; set; }

    public int ViewCount { get; set; }

    public int ClickCount { get; set; }

    public bool DeletedInd { get; set; }

    public bool SampleInd { get; set; }

    public Guid UserId { get; set; }

    [StringLength(100)]
    public string? CreatedBy { get; set; }

    [StringLength(100)]
    public string? ModifiedBY { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedDate { get; set; }

    public Category? Category { get; set; }

    public Subcategory? Subcategory { get; set; }

    public Province? Province { get; set; }

    public City? City { get; set; }
}