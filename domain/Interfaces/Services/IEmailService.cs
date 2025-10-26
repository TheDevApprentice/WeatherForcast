namespace domain.Interfaces.Services
{
    public interface IEmailService
    {
        // Task SendAsync(string toEmail, string subject, string body, CancellationToken cancellationToken = default);
        Task SendEmailConfirmationAsync(string toEmail, CancellationToken cancellationToken = default);
        Task SendEmailForgotPasswordAsync(string toEmail, CancellationToken cancellationToken = default);
        Task SendBulkEmailAsync(IEnumerable<string> toEmails, string subject, string body, CancellationToken cancellationToken = default);
    }
}
