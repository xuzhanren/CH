using System.ComponentModel.DataAnnotations;

namespace CanHappy.Models;

public class BusinessYellowPageDetail
{
    [Key]
    public Guid BusinessYellowPageDetailGUID { get; set; } = Guid.NewGuid();

    public Guid ListingGUID { get; set; }

    [StringLength(200)]
    public string? BusinessHours { get; set; }

    [StringLength(100)]
    public string? BusinessStyle { get; set; }

    [StringLength(1000)]
    public string? ProductsAndServices { get; set; }

    [StringLength(100)]
    public string? Specialties { get; set; }

    [StringLength(100)]
    public string? LanguagesSpoken { get; set; }

    [StringLength(50)]
    public string? GeneralBeforeTaxPayPerPerson { get; set; }

    [StringLength(100)]
    public string? MethodOfPayments { get; set; }

    [StringLength(100)]
    public string? HowToGetThere { get; set; }

    [StringLength(500)]
    public string? AdditionalInfo { get; set; }

    [StringLength(200)]
    public string? WebSiteURL { get; set; }

    public bool ActiveInd { get; set; }

    public bool DeletedInd { get; set; }

    [StringLength(50)]
    public string? CreatedBy { get; set; }

    [StringLength(50)]
    public string? ModifiedBy { get; set; }

    public DateTime CreatedDate { get; set; } = CanHappy.Common.EasternTime.Now;

    public DateTime? ModifiedDate { get; set; }

    public Listing? Listing { get; set; }
}
