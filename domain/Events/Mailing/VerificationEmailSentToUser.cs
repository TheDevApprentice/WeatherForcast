namespace domain.Events.Mailing
{
    public class VerificationEmailSentToUser : INotification
    {
        public string ToEmail { get; }
        public DateTime SentAt { get; }

        public VerificationEmailSentToUser(string toEmail)
        {
            ToEmail = toEmail;
            SentAt = DateTime.UtcNow;
        }
    }
}
