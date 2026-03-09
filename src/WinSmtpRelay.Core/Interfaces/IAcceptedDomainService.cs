using WinSmtpRelay.Core.Models;

namespace WinSmtpRelay.Core.Interfaces;

public interface IAcceptedDomainService
{
    Task<IReadOnlyList<AcceptedDomain>> GetAllAsync(CancellationToken ct = default);
    Task<AcceptedDomain> CreateAsync(string domain, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task<bool> ExistsAsync(string domain, CancellationToken ct = default);
}
