using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Resend;

namespace CulinaryCommand.Services;

public interface IEmailSender
{
    Task SendInviteEmailAsync(string toEmail, string firstName, string inviteToken);
}

public class EmailSender : IEmailSender
{
    private readonly IResend _resend;
    private readonly ILogger<EmailSender> _logger;
    private readonly string _fromAddress = "noreply@culinary-command.com";
    private readonly string _fromName = "Culinary Command";
    private readonly string _appBaseUrl = "https://culinary-command.com";

    public EmailSender(IResend resend, ILogger<EmailSender> logger, IConfiguration configuration)
    {
        _resend = resend;
        _logger = logger;
    }

    public async Task SendInviteEmailAsync(string toEmail, string firstName, string inviteToken)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
            throw new ArgumentException("Recipient email is required.", nameof(toEmail));

        if (string.IsNullOrWhiteSpace(inviteToken))
            throw new ArgumentException("Invite token is required.", nameof(inviteToken));

        var safeName = WebUtility.HtmlEncode(firstName ?? string.Empty);
        var inviteLink = $"{_appBaseUrl.TrimEnd('/')}/account/setup?token={Uri.EscapeDataString(inviteToken)}";

        var message = new EmailMessage
        {
            From = $"{_fromName} <{_fromAddress}>",
            Subject = "Set up your Culinary Command account",
            HtmlBody = $@"
                <div style=""font-family: Arial, sans-serif; line-height: 1.6;"">
                    <p>Hi {safeName},</p>
                    <p>You’ve been invited to join <strong>Culinary Command</strong>.</p>
                    <p>
                        <a href=""{inviteLink}""
                           style=""display:inline-block;padding:10px 16px;background:#111;color:#fff;text-decoration:none;border-radius:6px;"">
                           Set up your account
                        </a>
                    </p>
                    <p>Or copy and paste this link into your browser:</p>
                    <p>{WebUtility.HtmlEncode(inviteLink)}</p>
                    <p>If you weren’t expecting this email, you can ignore it.</p>
                </div>"
        };

        message.To.Add(toEmail);

        try
        {
            var response = await _resend.EmailSendAsync(message);

            if (!response.Success)
            {
                var error = response.Exception?.Message ?? "Unknown Resend error";

                _logger.LogError("Resend failed for {Email}. Error: {Error}", toEmail, error);

                throw new InvalidOperationException(
                    $"Failed to send invite email to {toEmail}: {error}");
            }

            _logger.LogInformation("Invite email sent successfully to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while sending invite email to {Email}", toEmail);
            throw;
        }
    }
}