using System.ComponentModel.DataAnnotations;

namespace CanHappy.Models;

public class ListingImage
{
    [Key]
    public Guid ListingImageGUID { get; set; } = Guid.NewGuid();

    public Guid ListingGUID { get; set; }

    [StringLength(50)]
    public string? Title { get; set; }

    public int SorOrder { get; set; }

    [StringLength(200)]
    public string? ThumbnailURL { get; set; }

    [StringLength(200)]
    public string? ImageURL { get; set; }

    public bool DeletedInd { get; set; }

    public bool SampleInd { get; set; }

    [StringLength(100)]
    public string? CreatedBy { get; set; }

    public DateTime CreatedDate { get; set; } = CanHappy.Common.EasternTime.Now;

    public Listing? Listing { get; set; }
}

