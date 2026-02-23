using System.ComponentModel.DataAnnotations;

namespace CanHappy.Models;

public class Ad
{
    [Key]
    public Guid AdGUID { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public int? CategoryId { get; set; }

    public int? SubcategoryId { get; set; }

    public int? ProvinceId { get; set; }

    public int CityId { get; set; }

    [StringLength(15)]
    public string? PostalCode { get; set; }

    [Required]
    [StringLength(50)]
    public string Subject { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Description { get; set; }

    [StringLength(100)]
    public string? KeyWords { get; set; }

    [StringLength(200)]
    public string? TargetURL { get; set; }

    [StringLength(200)]
    public string? ImageURL { get; set; }

    public decimal? Price { get; set; }

    [StringLength(10)]
    public string? CurrencyCode { get; set; }

    public int AdStatusId { get; set; } = 1;

    public int? AdSizeId { get; set; }

    public bool IsFeatured { get; set; }

    public DateTime PublishDate { get; set; } = CanHappy.Common.EasternTime.Now;

    public DateTime? ExpiryDate { get; set; }

    public int ViewCount { get; set; }

    public int ClickCount { get; set; }

    public bool PaidInd { get; set; }

    public bool ActiveInd { get; set; }

    [StringLength(100)]
    public string? ContactName { get; set; }

    [StringLength(100)]
    public string? ContactEmail { get; set; }

    [StringLength(30)]
    public string? ContactPhone { get; set; }

    public bool DeletedInd { get; set; }

    public bool SampleInd { get; set; }

    [StringLength(100)]
    public string? CreatedBy { get; set; }

    [StringLength(100)]
    public string? ModifiedBY { get; set; }

    public DateTime CreatedDate { get; set; } = CanHappy.Common.EasternTime.Now;

    public DateTime? ModifiedDate { get; set; }

    public Category? Category { get; set; }

    public Subcategory? Subcategory { get; set; }

    public Province? Province { get; set; }

    public City? City { get; set; }

    public AdStatus? AdStatus { get; set; }

    public AdSize? AdSizeOption { get; set; }
}
