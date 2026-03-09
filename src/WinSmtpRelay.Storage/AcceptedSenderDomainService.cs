using Microsoft.EntityFrameworkCore;
using WinSmtpRelay.Core.Interfaces;
using WinSmtpRelay.Core.Models;

namespace WinSmtpRelay.Storage;

public class AcceptedSenderDomainService(RelayDbContext db) : IAcceptedSenderDomainService
{
    public async Task<IReadOnlyList<AcceptedSenderDomain>> GetAllAsync(CancellationToken ct = default)
    {
        return await db.AcceptedSenderDomains.AsNoTracking().OrderBy(d => d.Domain).ToListAsync(ct);
    }

    public async Task<AcceptedSenderDomain> CreateAsync(string domain, CancellationToken ct = default)
    {
        var entry = new AcceptedSenderDomain { Domain = domain.ToLowerInvariant().Trim() };
        db.AcceptedSenderDomains.Add(entry);
        await db.SaveChangesAsync(ct);
        return entry;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await db.AcceptedSenderDomains.Where(d => d.Id == id).ExecuteDeleteAsync(ct);
    }

    public async Task<bool> ExistsAsync(string domain, CancellationToken ct = default)
    {
        return await db.AcceptedSenderDomains.AsNoTracking()
            .AnyAsync(d => d.Domain == domain.ToLowerInvariant().Trim(), ct);
    }
}
