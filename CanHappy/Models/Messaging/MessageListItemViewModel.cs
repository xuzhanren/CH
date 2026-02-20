namespace CanHappy.Models.Messaging;

public class MessageListItemViewModel
{
    public Guid UserMessageGUID { get; set; }

    public Guid ListingGUID { get; set; }

    public string ListingSubject { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string BodyPreview { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; }

    public bool IsRead { get; set; }

    public string CounterpartyName { get; set; } = string.Empty;
}
