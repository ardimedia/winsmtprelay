using WinSmtpRelay.Core.Models;

namespace WinSmtpRelay.Core.Interfaces;

public interface IMessageFilterService
{
    Task<IReadOnlyList<HeaderRewriteEntry>> GetHeaderRulesAsync(CancellationToken ct = default);
    Task<HeaderRewriteEntry> CreateHeaderRuleAsync(HeaderRewriteEntry rule, CancellationToken ct = default);
    Task UpdateHeaderRuleAsync(HeaderRewriteEntry rule, CancellationToken ct = default);
    Task DeleteHeaderRuleAsync(int id, CancellationToken ct = default);

    Task<IReadOnlyList<SenderRewriteEntry>> GetSenderRulesAsync(CancellationToken ct = default);
    Task<SenderRewriteEntry> CreateSenderRuleAsync(SenderRewriteEntry rule, CancellationToken ct = default);
    Task UpdateSenderRuleAsync(SenderRewriteEntry rule, CancellationToken ct = default);
    Task DeleteSenderRuleAsync(int id, CancellationToken ct = default);
}
