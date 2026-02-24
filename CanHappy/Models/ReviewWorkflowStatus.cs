namespace CanHappy.Models;

public static class ReviewWorkflowStatus
{
    public const string Submitted = "Submitted";
    public const string Reviewed = "Reviewed";
    public const string Approved = "Approved";
    public const string ReleasedToSite = "Released To Site";

    public static readonly string[] All =
    [
        Submitted,
        Reviewed,
        Approved,
        ReleasedToSite
    ];
}
