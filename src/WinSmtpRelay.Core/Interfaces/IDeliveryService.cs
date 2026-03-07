using WinSmtpRelay.Core.Models;

namespace WinSmtpRelay.Core.Interfaces;

public interface IDeliveryService
{
    Task DeliverAsync(QueuedMessage message, CancellationToken cancellationToken = default);
}
