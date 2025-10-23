using MediatR;

namespace domain.Events.Admin
{
    /// <summary>
    /// Événement déclenché quand un utilisateur se déconnecte
    /// </summary>
    public class UserLoggedOutEvent : INotification
    {
        public string UserId { get; }
        public string Email { get; }
        public DateTime LoggedOutAt { get; }

        public UserLoggedOutEvent(string userId, string email, DateTime timeLogout)
        {
            UserId = userId;
            Email = email;
            LoggedOutAt = timeLogout;
        }
    }
}
