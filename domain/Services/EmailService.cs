using domain.Constants;
using domain.Events;
using domain.Events.Mailing;
using domain.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace domain.Services
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly IPublisher _publisher;
        private readonly EmailOptions _options;
        private readonly SemaphoreSlim _smtpConcurrency;

        public EmailService(ILogger<EmailService> logger, IPublisher publisher, IOptions<EmailOptions> options)
        {
            _logger = logger;
            _publisher = publisher;
            _options = options.Value;
            var maxConc = _options.MaxConcurrency <= 0 ? 1 : _options.MaxConcurrency;
            _smtpConcurrency = new SemaphoreSlim(initialCount: maxConc, maxCount: maxConc);
        }

        private async Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_options.Host) || string.IsNullOrWhiteSpace(_options.From))
                {
                    _logger.LogWarning("[Email] Configuration incomplète. Host/From requis. Log-only: To={To} | Subject={Subject}", toEmail, subject);
                    return;
                }

                using var message = new MailMessage();
                message.From = new MailAddress(_options.From);
                message.To.Add(new MailAddress(toEmail));
                message.Subject = subject;
                message.Body = body;
                message.IsBodyHtml = true;

                using var client = new SmtpClient(_options.Host, _options.Port)
                {
                    EnableSsl = _options.EnableSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = string.IsNullOrWhiteSpace(_options.UserName)
                        ? CredentialCache.DefaultNetworkCredentials
                        : new NetworkCredential(_options.UserName, _options.Password)
                };

                await _smtpConcurrency.WaitAsync(cancellationToken);
                try
                {
#if NET6_0_OR_GREATER
                    await client.SendMailAsync(message, cancellationToken);
#else
                    await client.SendMailAsync(message);
#endif
                }
                finally
                {
                    _smtpConcurrency.Release();
                }

                _logger.LogInformation("[Email] Envoyé à {To} | Subject={Subject}", toEmail, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'envoi d'un email à {Email}", toEmail);
                throw;
            }
        }

        private async Task SendBulkAsync(IEnumerable<string> toEmails, string subject, string body, CancellationToken cancellationToken = default)
        {
            var emailList = toEmails?.Where(e => !string.IsNullOrWhiteSpace(e))?.Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? new();
            if (emailList.Count == 0) return;

            var batchSize = _options.BatchSize <= 0 ? 200 : _options.BatchSize;
            for (int i = 0; i < emailList.Count; i += batchSize)
            {
                var batch = emailList.Skip(i).Take(batchSize).ToList();

                var tasks = batch.Select(async toEmail =>
                {
                    try
                    {
                        await SendAsync(toEmail, subject, body, cancellationToken);
                        _logger.LogInformation("[Email] To={To} | Subject={Subject}", toEmail, subject);
                        await _publisher.Publish(new EmailSentToUser(toEmail, subject, body), cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erreur lors de l'envoi d'un email en masse à {Email}", toEmail);
                        // Continue avec les autres destinataires
                    }
                });

                await Task.WhenAll(tasks);
            }
        }

        public async Task SendEmailConfirmationAsync(string toEmail, CancellationToken cancellationToken = default)
        {
            // NOTE: Ici on simule un envoi. En prod: générer un token & URL et utiliser un provider SMTP/SendGrid/etc.
            var subject = "Confirmez votre adresse email";
            var body = "Merci pour votre inscription. Cliquez sur le lien de confirmation envoyé par l'application (démo).";
            // await SendAsync(toEmail, subject, body, cancellationToken);
            await _publisher.Publish(new VerificationEmailSentToUser(toEmail), cancellationToken);
        }

        public async Task SendEmailForgotPasswordAsync(string toEmail, CancellationToken cancellationToken = default)
        {
            // NOTE: Ici on simule un envoi. En prod: générer un token & URL et utiliser un provider SMTP/SendGrid/etc.
            var subject = "Mot de passe oublié";
            var body = "Vous avez demandé un nouveau mot de passe. Cliquez sur le lien de confirmation envoyé par l'application (démo).";
            // await SendAsync(toEmail, subject, body, cancellationToken);
            await _publisher.Publish(new VerificationEmailSentToUser(toEmail), cancellationToken);
        }

        public async Task SendBulkEmailAsync(IEnumerable<string> toEmails, string subject, string body, CancellationToken cancellationToken = default)
        {
            // NOTE: Ici on simule un envoi. En prod: générer un token & URL et utiliser un provider SMTP/SendGrid/etc.
            await SendBulkAsync(toEmails, subject, body, cancellationToken);
        }
    }
}
