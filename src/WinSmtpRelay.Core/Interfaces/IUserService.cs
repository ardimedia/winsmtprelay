using WinSmtpRelay.Core.Models;

namespace WinSmtpRelay.Core.Interfaces;

public interface IUserService
{
    Task<bool> ValidateCredentialsAsync(string username, string password, CancellationToken cancellationToken = default);
    Task<RelayUser?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task CreateUserAsync(string username, string password, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RelayUser>> GetAllUsersAsync(CancellationToken cancellationToken = default);
    Task DeleteUserAsync(int userId, CancellationToken cancellationToken = default);
}
