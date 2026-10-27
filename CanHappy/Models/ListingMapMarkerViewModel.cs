namespace CanHappy.Models;

public class ListingMapMarkerViewModel
{
    public Guid ListingGUID { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public decimal? Price { get; set; }
    public string DetailUrl { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}
