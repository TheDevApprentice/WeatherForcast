using System.Threading;
using System.Threading.Tasks;

namespace domain.Events
{
    public interface IPublisher
    {
        Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification;
    }
}
