namespace CanHappy.Models.Messaging;

public class MessageDetailsViewModel
{
    public Guid UserMessageGUID { get; set; }

    public Guid ListingGUID { get; set; }

    public string ListingSubject { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; }

    public bool IsRead { get; set; }

    public string SenderName { get; set; } = string.Empty;

    public string RecipientName { get; set; } = string.Empty;

    public bool IsInboxMessage { get; set; }
}
