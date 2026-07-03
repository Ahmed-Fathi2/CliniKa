using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using CliniKa.Application.Abstraction;
using CliniKa.Infrastructure.Options;

namespace CliniKa.Infrastructure.Services
{
    public class SendGridEmailSender : IEmailSender
    {
        private readonly SendGridOptions _options;

        public SendGridEmailSender(IOptions<SendGridOptions> options)
        {
            _options = options.Value;
        }
        public async Task SendAsync(string toEmail, string subject, string htmlBody)
        {
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                throw new InvalidOperationException("SendGrid API key is missing. Configure SendGridSettings:ApiKey.");
            }

            var client = new SendGridClient(_options.ApiKey);
            var from = new EmailAddress(_options.SenderEmail, _options.SenderName);
            var to = new EmailAddress(toEmail);

            var msg = MailHelper.CreateSingleEmail(
                from,
                to,
                subject,
                plainTextContent: null,
                htmlContent: htmlBody);

            var response = await client.SendEmailAsync(msg);

            if ((int)response.StatusCode >= 400)
            {
                var responseBody = response.Body is null
                    ? string.Empty
                    : await response.Body.ReadAsStringAsync();

                throw new InvalidOperationException(
                    $"SendGrid failed with status code {(int)response.StatusCode}. {responseBody}");
            }
        }
    }
}
