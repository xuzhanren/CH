using System.ComponentModel.DataAnnotations;

namespace CanHappy.Models;

public class Ad
{
    [Key]
    public Guid AdGUID { get; set; } = Guid.NewGuid();

    public int CityId { get; set; }

    [StringLength(15)]
    public string? PostalCode { get; set; }

    [Required]
    [StringLength(50)]
    public string Subject { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Description { get; set; }

    [StringLength(200)]
    public string? TargetURL { get; set; }

    [StringLength(200)]
    public string? ImageURL { get; set; }

    public decimal? Price { get; set; }

    [StringLength(10)]
    public string? CurrencyCode { get; set; }

    [StringLength(20)]
    public string Status { get; set; } = "Draft";

    public bool IsFeatured { get; set; }

    public DateTime PublishDate { get; set; } = DateTime.UtcNow;

    public DateTime? ExpiryDate { get; set; }

    public int ViewCount { get; set; }

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

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedDate { get; set; }

    public City? City { get; set; }
}