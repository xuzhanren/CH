namespace CanHappy.Services.Email;

public interface IMessageNotificationEmailSender
{
    Task SendSellerMessageNotificationAsync(
        string sellerEmail,
        string sellerDisplayName,
        string senderEmail,
        string senderDisplayName,
        string listingSubject,
        string messageSubject,
        string messageBody,
        string listingUrl,
        string sellerInboxUrl,
        CancellationToken cancellationToken = default);
}
