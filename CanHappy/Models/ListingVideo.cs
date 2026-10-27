using System;

namespace CanHappy.Models;

public class ListingVideo
{
    public Guid ListingVideoGUID { get; set; }
    public Guid ListingGUID { get; set; }

    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string VideoSize { get; set; } = string.Empty;
    public string ThumbnailURL { get; set; } = string.Empty;
    public string VideoURL { get; set; } = string.Empty;
    public bool DeletedInd { get; set; }

    public string CreatedBy { get; set; } = string.Empty;
    public string ModifiedBy { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public DateTime ModifiedDate { get; set; }

    public Listing? Listing { get; set; }
}