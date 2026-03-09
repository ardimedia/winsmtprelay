using WinSmtpRelay.Core.Models;

namespace WinSmtpRelay.Core.Interfaces;

public interface IIpAccessRuleService
{
    Task<IReadOnlyList<IpAccessRule>> GetAllAsync(CancellationToken ct = default);
    Task<IpAccessRule> CreateAsync(IpAccessRule rule, CancellationToken ct = default);
    Task UpdateAsync(IpAccessRule rule, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
