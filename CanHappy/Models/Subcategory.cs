using System.ComponentModel.DataAnnotations;

namespace CanHappy.Models;

public class Subcategory
{
    [Key]
    public int SubcategoryId { get; set; }

    public int CategoryId { get; set; }

    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;

    [StringLength(50)]
    public string? Code { get; set; }

    [StringLength(100)]
    public string? Description { get; set; }

    public int SortOrder { get; set; }

    public bool DeletedInd { get; set; }

    public bool SampleInd { get; set; }

    [StringLength(100)]
    public string? CreatedBy { get; set; }

    [StringLength(100)]
    public string? ModifiedBY { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? ModifiedDate { get; set; }

    public Category? Category { get; set; }
}