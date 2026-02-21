using System.ComponentModel.DataAnnotations;

namespace CanHappy.Models;

public class BuySellDetailIndexPageViewModel
{
    public Guid? ListingGUIDFilter { get; set; }
    public BuySellDetailListingCardViewModel? ListingCard { get; set; }
    public BuySellDetailEditViewModel? FocusDetail { get; set; }
    public bool CanManageFocusedListing { get; set; }
    public bool HasExistingFocusDetail { get; set; }
    public List<BuySellDetailListingImageViewModel> ListingImages { get; set; } = [];
    public List<BuySellDetailIndexItemViewModel> Items { get; set; } = [];
}

public class BuySellDetailListingCardViewModel
{
    public Guid ListingGUID { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public int SubcategoryId { get; set; }
    public int ProvinceId { get; set; }
    public int CityId { get; set; }
    public decimal? Price { get; set; }
    public string? Brand { get; set; }
    public string Condition { get; set; } = "Used";
    public int? ManufactureYear { get; set; }
    public string? Model { get; set; }
    public int Quantity { get; set; }
}

public class BuySellDetailListingImageViewModel
{
    public Guid ListingImageGUID { get; set; }
    public Guid ListingGUID { get; set; }
    public string? Title { get; set; }
    public int SorOrder { get; set; }
    public string? ThumbnailURL { get; set; }
    public string? ImageURL { get; set; }
}

public class BuySellDetailIndexItemViewModel
{
    public Guid BuySellDetailGUID { get; set; }
    public Guid ListingGUID { get; set; }

    public string Subject { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public int SubcategoryId { get; set; }
    public int ProvinceId { get; set; }
    public int CityId { get; set; }
    public decimal? Price { get; set; }
    public string? Brand { get; set; }
    public string Condition { get; set; } = "Used";
    public int? ManufactureYear { get; set; }
    public string? ListingModel { get; set; }
    public int Quantity { get; set; }

    public string? BuySellModel { get; set; }
    public string? Material { get; set; }
    public bool NegotiablePriceInd { get; set; }
    public bool DeliveryAvailableInd { get; set; }
    public bool PickupAvailableInd { get; set; }
    public string? WarrantyInfo { get; set; }
    public string? AdditionalDetails { get; set; }

    public bool CanManage { get; set; }
}

public class BuySellDetailEditViewModel
{
    public Guid? BuySellDetailGUID { get; set; }

    [Required]
    public Guid ListingGUID { get; set; }

    public string Subject { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public int SubcategoryId { get; set; }
    public int ProvinceId { get; set; }
    public int CityId { get; set; }
    public decimal? Price { get; set; }
    public string? Brand { get; set; }
    public string Condition { get; set; } = "Used";
    public int? ManufactureYear { get; set; }
    public string? ListingModel { get; set; }
    public int Quantity { get; set; }

    [StringLength(50)]
    [Display(Name = "Model")]
    public string? BuySellModel { get; set; }

    [StringLength(50)]
    public string? Material { get; set; }

    public bool NegotiablePriceInd { get; set; }
    public bool DeliveryAvailableInd { get; set; }
    public bool PickupAvailableInd { get; set; }

    [StringLength(100)]
    public string? WarrantyInfo { get; set; }

    [StringLength(500)]
    public string? AdditionalDetails { get; set; }
}
