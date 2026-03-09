using WinSmtpRelay.Core.Models;

namespace WinSmtpRelay.Core.Interfaces;

public interface IReceiveConnectorService
{
    Task<IReadOnlyList<ReceiveConnector>> GetAllAsync(CancellationToken ct = default);
    Task<ReceiveConnector?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<ReceiveConnector> CreateAsync(ReceiveConnector connector, CancellationToken ct = default);
    Task UpdateAsync(ReceiveConnector connector, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
