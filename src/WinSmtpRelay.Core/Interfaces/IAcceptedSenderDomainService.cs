using WinSmtpRelay.Core.Models;

namespace WinSmtpRelay.Core.Interfaces;

public interface IAcceptedSenderDomainService
{
    Task<IReadOnlyList<AcceptedSenderDomain>> GetAllAsync(CancellationToken ct = default);
    Task<AcceptedSenderDomain> CreateAsync(string domain, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task<bool> ExistsAsync(string domain, CancellationToken ct = default);
}
