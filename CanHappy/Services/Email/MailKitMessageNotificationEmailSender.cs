using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace CanHappy.Services.Email;

public class MailKitMessageNotificationEmailSender(IOptions<EmailSettings> settingsOptions) : IMessageNotificationEmailSender
{
    private readonly EmailSettings settings = settingsOptions.Value;

    public async Task SendSellerMessageNotificationAsync(
        string sellerEmail,
        string sellerDisplayName,
        string senderEmail,
        string senderDisplayName,
        string listingSubject,
        string messageSubject,
        string messageBody,
        string listingUrl,
        string sellerInboxUrl,
        CancellationToken cancellationToken = default)
    {
        ValidateSettings();

        var fromEmail = settings.FromEmail!;

        var emailMessage = new MimeMessage();
        emailMessage.To.Add(new MailboxAddress(sellerDisplayName, sellerEmail));
        emailMessage.From.Add(new MailboxAddress(settings.FromName ?? "CanHappy", fromEmail));

        emailMessage.Subject = $"New message about your listing: {listingSubject}";

        var textBody = $"""
            Hi {sellerDisplayName},

            You received a new message from {senderDisplayName}.
            Sender email: {senderEmail}

            Listing: {listingSubject}
            Listing link: {listingUrl}

            Message subject: {messageSubject}
            Message body:
            {messageBody}

            Reply from your message box:
            {sellerInboxUrl}
            """;

        emailMessage.Body = new TextPart("plain")
        {
            Text = textBody
        };

        using var client = new SmtpClient();
        //var socketOptions = settings.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
        var socketOptions = settings.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable;
        await client.ConnectAsync(settings.SmtpHost, settings.SmtpPort, socketOptions, cancellationToken);

        if (!string.IsNullOrWhiteSpace(settings.UserName))
        {
            await client.AuthenticateAsync(settings.UserName, settings.Password, cancellationToken);
        }

        await client.SendAsync(emailMessage, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }

    private void ValidateSettings()
    {
        if (string.IsNullOrWhiteSpace(settings.SmtpHost))
        {
            throw new InvalidOperationException("Email:SmtpHost is not configured.");
        }

        if (string.IsNullOrWhiteSpace(settings.FromEmail))
        {
            throw new InvalidOperationException("Email:FromEmail is not configured.");
        }
    }
}
