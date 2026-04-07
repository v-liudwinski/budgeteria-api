using MailKit.Net.Smtp;
using MimeKit;

namespace Budgeteria.Api.Services;

public class EmailSettings
{
    public string SmtpHost { get; set; } = "";
    public int SmtpPort { get; set; } = 587;
    public string SmtpUser { get; set; } = "";
    public string SmtpPass { get; set; } = "";
    public string FromEmail { get; set; } = "noreply@budgeteria.app";
    public string FromName { get; set; } = "Budgeteria";
    public string FrontendUrl { get; set; } = "http://localhost:5173";
}

public class EmailService(EmailSettings settings, ILogger<EmailService> logger) : IEmailService
{
    public async Task SendInviteAsync(string toEmail, string toName, string inviterName, string planName, string token)
    {
        var acceptUrl = $"{settings.FrontendUrl}/invite/accept?token={token}";

        var html = BuildInviteHtml(toName, inviterName, planName, acceptUrl);

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(settings.FromName, settings.FromEmail));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = $"{inviterName} invited you to {planName} on Budgeteria";

        message.Body = new BodyBuilder
        {
            HtmlBody = html,
            TextBody = $"{inviterName} invited you to join \"{planName}\" on Budgeteria.\n\nAccept here: {acceptUrl}\n\nThis link expires in 7 days."
        }.ToMessageBody();

        try
        {
            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(settings.SmtpHost, settings.SmtpPort, MailKit.Security.SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(settings.SmtpUser, settings.SmtpPass);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
            logger.LogInformation("Invite email sent to {Email} for plan {Plan}", toEmail, planName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send invite email to {Email}", toEmail);
            throw;
        }
    }

    private static string BuildInviteHtml(string toName, string inviterName, string planName, string acceptUrl)
    {
        return $$"""
        <!DOCTYPE html>
        <html lang="en">
        <head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head>
        <body style="margin:0;padding:0;background-color:#f0f4ff;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;">
          <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background-color:#f0f4ff;padding:40px 16px;">
            <tr><td align="center">
              <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:480px;background-color:#ffffff;border-radius:24px;border:1px solid #e8ecf4;box-shadow:0 2px 12px rgba(0,0,0,0.06);overflow:hidden;">

                <!-- Header gradient -->
                <tr><td style="background:linear-gradient(135deg,#dbeafe 0%,#ffffff 50%,#fce7f3 100%);padding:32px 32px 20px;text-align:center;">
                  <!-- Paw icon -->
                  <div style="display:inline-block;width:56px;height:56px;border-radius:16px;background:linear-gradient(135deg,rgba(59,130,246,0.15),rgba(236,72,153,0.15));line-height:56px;font-size:28px;margin-bottom:12px;">
                    🐾
                  </div>
                  <h1 style="margin:0;font-size:22px;font-weight:800;background:linear-gradient(135deg,#3b82f6,#ec4899);-webkit-background-clip:text;-webkit-text-fill-color:transparent;background-clip:text;">
                    Budgeteria
                  </h1>
                </td></tr>

                <!-- Body -->
                <tr><td style="padding:24px 32px 32px;">
                  <h2 style="margin:0 0 8px;font-size:18px;font-weight:700;color:#1e293b;">
                    You're invited! 🎉
                  </h2>
                  <p style="margin:0 0 20px;font-size:14px;color:#64748b;line-height:1.6;">
                    <strong style="color:#1e293b;">{{inviterName}}</strong> has invited you to join their family budget plan <strong style="color:#1e293b;">"{{planName}}"</strong> on Budgeteria.
                  </p>

                  <!-- Features -->
                  <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="margin-bottom:24px;">
                    <tr><td style="padding:8px 0;">
                      <table role="presentation" cellpadding="0" cellspacing="0"><tr>
                        <td style="width:32px;height:32px;border-radius:8px;background-color:#f0f4ff;text-align:center;line-height:32px;font-size:14px;">📊</td>
                        <td style="padding-left:12px;font-size:13px;color:#64748b;">Track spending together in real time</td>
                      </tr></table>
                    </td></tr>
                    <tr><td style="padding:8px 0;">
                      <table role="presentation" cellpadding="0" cellspacing="0"><tr>
                        <td style="width:32px;height:32px;border-radius:8px;background-color:#f0f4ff;text-align:center;line-height:32px;font-size:14px;">🎯</td>
                        <td style="padding-left:12px;font-size:13px;color:#64748b;">Share savings goals and celebrate wins</td>
                      </tr></table>
                    </td></tr>
                    <tr><td style="padding:8px 0;">
                      <table role="presentation" cellpadding="0" cellspacing="0"><tr>
                        <td style="width:32px;height:32px;border-radius:8px;background-color:#f0f4ff;text-align:center;line-height:32px;font-size:14px;">🫧</td>
                        <td style="padding-left:12px;font-size:13px;color:#64748b;">Beautiful bubble budget visualization</td>
                      </tr></table>
                    </td></tr>
                  </table>

                  <!-- CTA button -->
                  <table role="presentation" width="100%" cellpadding="0" cellspacing="0">
                    <tr><td align="center">
                      <a href="{{acceptUrl}}" target="_blank" style="display:inline-block;padding:14px 40px;background:linear-gradient(135deg,#3b82f6,#60a5fa);color:#ffffff;font-size:14px;font-weight:700;text-decoration:none;border-radius:14px;box-shadow:0 4px 14px rgba(59,130,246,0.3);">
                        Join {{planName}}
                      </a>
                    </td></tr>
                  </table>

                  <p style="margin:20px 0 0;font-size:12px;color:#94a3b8;text-align:center;line-height:1.5;">
                    This invitation expires in 7 days.<br>
                    If you didn't expect this, you can safely ignore it.
                  </p>
                </td></tr>

                <!-- Footer -->
                <tr><td style="padding:16px 32px;border-top:1px solid #e8ecf4;text-align:center;">
                  <p style="margin:0;font-size:11px;color:#94a3b8;">
                    Budgeteria — Your family's budget companion 🐾<br>
                    Free for families. No credit card needed.
                  </p>
                </td></tr>

              </table>
            </td></tr>
          </table>
        </body>
        </html>
        """;
    }
}
