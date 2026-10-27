using System.ComponentModel.DataAnnotations;

namespace CanHappy.Models;

public class Province
{
    [Key]
    public int ProvinceId { get; set; }

    public int CountryId { get; set; }

    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Code { get; set; }

    [StringLength(100)]
    public string? Description { get; set; }

    public bool DeletedInd { get; set; }

    public bool SampleInd { get; set; }

    [StringLength(100)]
    public string? CreatedBy { get; set; }

    [StringLength(100)]
    public string? ModifiedBY { get; set; }

    public DateTime CreatedDate { get; set; } = CanHappy.Common.EasternTime.Now;

    public DateTime? ModifiedDate { get; set; }

    public Country? Country { get; set; }

    public ICollection<City> Cities { get; set; } = [];
}
