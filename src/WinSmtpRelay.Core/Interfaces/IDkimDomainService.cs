using WinSmtpRelay.Core.Models;

namespace WinSmtpRelay.Core.Interfaces;

public interface IDkimDomainService
{
    Task<IReadOnlyList<DkimDomain>> GetAllAsync(CancellationToken ct = default);
    Task<DkimDomain?> GetByDomainAsync(string domain, CancellationToken ct = default);
    Task<DkimDomain> CreateAsync(DkimDomain dkim, CancellationToken ct = default);
    Task UpdateAsync(DkimDomain dkim, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
