using System.ComponentModel.DataAnnotations;

namespace CanHappy.Models;

public class BusinessYellowPageDetailIndexPageViewModel
{
    public Guid? ListingGUIDFilter { get; set; }
    public BuySellDetailListingCardViewModel? ListingCard { get; set; }
    public BusinessYellowPageDetailEditViewModel? FocusDetail { get; set; }
    public bool CanManageFocusedListing { get; set; }
    public bool IsFavorited { get; set; }
    public bool HasExistingFocusDetail { get; set; }
    public List<BuySellDetailListingImageViewModel> ListingImages { get; set; } = [];
    public List<BusinessYellowPageSimilarListingViewModel> SimilarBusinesses { get; set; } = [];
}

public class BusinessYellowPageSimilarListingViewModel
{
    public Guid ListingGUID { get; set; }
    public string? Subject { get; set; }
    public string? ThumbnailURL { get; set; }
}

public class SalesSpecialsImageViewModel
{
    public Guid SalesSpecialsImageGUID { get; set; }
    public Guid ListingGUID { get; set; }
    public string? Title { get; set; }
    public int SortOrder { get; set; }
    public decimal? Price { get; set; }
    public decimal? SalePrice { get; set; }
    public string? PercentOff { get; set; }
    public DateTime? SaleBegin { get; set; }
    public DateTime? SaleEnd { get; set; }
    public string? Description { get; set; }
    public string? ThumbnailURL { get; set; }
    public string? ImageURL { get; set; }
}

public class BusinessYellowPageDetailEditViewModel
{
    public Guid? BusinessYellowPageDetailGUID { get; set; }

    [Required]
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
}
