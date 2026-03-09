using Microsoft.EntityFrameworkCore;
using WinSmtpRelay.Core.Interfaces;
using WinSmtpRelay.Core.Models;

namespace WinSmtpRelay.Storage;

public class AcceptedDomainService(RelayDbContext db) : IAcceptedDomainService
{
    public async Task<IReadOnlyList<AcceptedDomain>> GetAllAsync(CancellationToken ct = default)
    {
        return await db.AcceptedDomains.AsNoTracking().OrderBy(d => d.Domain).ToListAsync(ct);
    }

    public async Task<AcceptedDomain> CreateAsync(string domain, CancellationToken ct = default)
    {
        var entry = new AcceptedDomain { Domain = domain.ToLowerInvariant().Trim() };
        db.AcceptedDomains.Add(entry);
        await db.SaveChangesAsync(ct);
        return entry;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await db.AcceptedDomains.Where(d => d.Id == id).ExecuteDeleteAsync(ct);
    }

    public async Task<bool> ExistsAsync(string domain, CancellationToken ct = default)
    {
        return await db.AcceptedDomains.AsNoTracking()
            .AnyAsync(d => d.Domain == domain.ToLowerInvariant().Trim(), ct);
    }
}
