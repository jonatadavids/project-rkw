using System.Threading;
using System.Threading.Tasks;

namespace RKW.Backend
{
    /// <summary>
    /// Minimal authentication boundary consumed by the application bootstrap.
    /// </summary>
    public interface IAuthenticationService
    {
        bool IsSignedIn { get; }

        Task<bool> SignInAnonymouslyAsync(
            CancellationToken cancellationToken = default);
    }
}
