using WinSmtpRelay.Core.Models;

namespace WinSmtpRelay.Core.Interfaces;

public interface ISendConnectorService
{
    Task<IReadOnlyList<SendConnector>> GetAllAsync(CancellationToken ct = default);
    Task<SendConnector?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<SendConnector?> GetDefaultAsync(CancellationToken ct = default);
    Task<SendConnector> CreateAsync(SendConnector connector, CancellationToken ct = default);
    Task UpdateAsync(SendConnector connector, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
