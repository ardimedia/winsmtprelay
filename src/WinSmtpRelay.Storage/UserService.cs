using Microsoft.EntityFrameworkCore;
using WinSmtpRelay.Core.Interfaces;
using WinSmtpRelay.Core.Models;

namespace WinSmtpRelay.Storage;

public class UserService(RelayDbContext db) : IUserService
{
    public async Task<bool> ValidateCredentialsAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var user = await db.RelayUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == username && u.IsEnabled, cancellationToken);

        if (user == null)
            return false;

        return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
    }

    public async Task<RelayUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return await db.RelayUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
    }

    public async Task CreateUserAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var hash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

        db.RelayUsers.Add(new RelayUser
        {
            Username = username,
            PasswordHash = hash
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RelayUser>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        return await db.RelayUsers.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task DeleteUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        await db.RelayUsers.Where(u => u.Id == userId).ExecuteDeleteAsync(cancellationToken);
    }
}
