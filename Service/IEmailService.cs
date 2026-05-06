using SendGrid;
using SendGrid.Helpers.Mail;

namespace FoodiGOAPI.Services;

public interface IEmailService
{
    Task SendResetEmailAsync(string toEmail, string resetLink);
}


public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendResetEmailAsync(string toEmail, string resetLink)
    {
        var apiKey = _config["SendGrid:ApiKey"];
        var client = new SendGridClient(apiKey);

        var from = new EmailAddress("no-reply@yourapp.com", "FoodiGO");
        var subject = "Reset Your Password";

        var to = new EmailAddress(toEmail);

        var htmlContent = $@"
            <h2>Password Reset</h2>
            <p>Click the link below to reset your password:</p>
            <a href='{resetLink}'>Reset Password</a>
            <p>This link expires in 1 hour.</p>
        ";

        var msg = MailHelper.CreateSingleEmail(from, to, subject, "", htmlContent);

        await client.SendEmailAsync(msg);
    }
}
