using System.ComponentModel.DataAnnotations;

namespace CanHappy.Models.Api;

public class SubcategoryUpsertRequest
{
    [Required]
    public int CategoryId { get; set; }

    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Code { get; set; }

    [StringLength(100)]
    public string? Description { get; set; }

    [StringLength(800)]
    public string? KeyWords { get; set; }

    public int SortOrder { get; set; }
    public bool SampleInd { get; set; }
}
