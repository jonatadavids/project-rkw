using System.Threading;
using System.Threading.Tasks;

namespace RKW.Network
{
    /// <summary>
    /// Minimal connection lifecycle required by the Photon foundation.
    /// Gameplay transport operations are added only when they have consumers.
    /// </summary>
    public interface INetworkTransport
    {
        bool IsConnected { get; }

        Task<NetworkConnectionResult> ConnectAsync(
            string sessionName,
            float timeoutSeconds,
            CancellationToken cancellationToken = default);

        Task DisconnectAsync();
    }
}
