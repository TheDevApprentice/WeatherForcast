namespace domain.Events.Mailing
{
    public class EmailSentToUser : INotification
    {
        public string ToEmail { get; }
        public string Subject { get; }
        public string Body { get; }
        public DateTime SentAt { get; }

        public EmailSentToUser(string toEmail, string subject, string body)
        {
            ToEmail = toEmail;
            Subject = subject;
            Body = body;
            SentAt = DateTime.UtcNow;
        }
    }
}
