using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using MimeKit;
using Slush.Application.Interfaces;

namespace Slush.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string message)
    {
        var emailMessage = new MimeMessage();

        emailMessage.From.Add(new MailboxAddress(
            _configuration["EmailSettings:SenderName"],
            _configuration["EmailSettings:SenderEmail"]));

        emailMessage.To.Add(new MailboxAddress("", toEmail));
        emailMessage.Subject = subject;
        emailMessage.Body = new TextPart(MimeKit.Text.TextFormat.Html)
        {
            Text = message
        };

        using var client = new SmtpClient();

        try
        {
            await client.ConnectAsync(
                _configuration["EmailSettings:SmtpServer"],
                int.Parse(_configuration["EmailSettings:SmtpPort"]!),
                SecureSocketOptions.StartTls);

            await client.AuthenticateAsync(
                _configuration["EmailSettings:SenderEmail"],
                _configuration["EmailSettings:SenderPassword"]);

            await client.SendAsync(emailMessage);
        }
        finally
        {
            await client.DisconnectAsync(true);
        }
    }
}