using System.ComponentModel.DataAnnotations;

namespace CanHappy.Models;

public class EstateRoom
{
    [Key]
    public Guid EstateRoomGUID { get; set; } = Guid.NewGuid();

    public Guid EstateSaleDetailGUID { get; set; }

    [StringLength(50)]
    public string? RoomName { get; set; }

    [StringLength(30)]
    public string? RoomSizeFtxFt { get; set; }

    public int SortOrder { get; set; }

    public int OnFloorNumber { get; set; }

    [StringLength(200)]
    public string? ThumbnailURL { get; set; }

    [StringLength(200)]
    public string? ImageURL { get; set; }

    public bool DeletedInd { get; set; }

    [StringLength(50)]
    public string? CreatedBy { get; set; }

    [StringLength(50)]
    public string? ModifiedBy { get; set; }

    public DateTime CreatedDate { get; set; } = CanHappy.Common.EasternTime.Now;

    public DateTime? ModifiedDate { get; set; }

    public EstateSaleDetail? EstateSaleDetail { get; set; }
}
