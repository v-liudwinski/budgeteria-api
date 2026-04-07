namespace Budgeteria.Api.Services;

public interface IEmailService
{
    Task SendInviteAsync(string toEmail, string toName, string inviterName, string planName, string token);
}
