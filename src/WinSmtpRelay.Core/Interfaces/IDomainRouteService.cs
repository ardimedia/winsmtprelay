using WinSmtpRelay.Core.Models;

namespace WinSmtpRelay.Core.Interfaces;

public interface IDomainRouteService
{
    Task<IReadOnlyList<DomainRoute>> GetAllAsync(CancellationToken ct = default);
    Task<DomainRoute> CreateAsync(DomainRoute route, CancellationToken ct = default);
    Task UpdateAsync(DomainRoute route, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
