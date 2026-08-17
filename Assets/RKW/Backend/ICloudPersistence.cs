using System.Threading;
using System.Threading.Tasks;

namespace RKW.Backend
{
    /// <summary>
    /// Minimal JSON persistence contract required by the UGS foundation.
    /// Typed profile persistence is added only when it has a real consumer.
    /// Caller cancellation is best-effort: it returns control without waiting for
    /// an SDK operation that cannot itself be canceled. A write already submitted
    /// to the service may still be completed by the server.
    /// </summary>
    public interface ICloudPersistence
    {
        Task SaveJsonAsync(
            string key,
            string json,
            CancellationToken cancellationToken = default);

        Task<string> LoadJsonAsync(
            string key,
            CancellationToken cancellationToken = default);
    }
}
