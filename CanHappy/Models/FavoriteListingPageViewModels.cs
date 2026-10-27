namespace CanHappy.Models;

public class FavoriteListingIndexPageViewModel
{
    public List<FavoriteListingListItemViewModel> Items { get; set; } = [];
}

public class FavoriteListingListItemViewModel
{
    public Guid FavoriteListingGUID { get; set; }
    public Guid ListingGUID { get; set; }
    public string ListingURL { get; set; } = string.Empty;
    public string ListingSubject { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTime CreatedDate { get; set; }
}
