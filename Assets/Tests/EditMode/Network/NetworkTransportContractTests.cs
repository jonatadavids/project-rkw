using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace RKW.Network.Tests.EditMode
{
    public sealed class NetworkTransportContractTests
    {
        [Test]
        public async Task MockTransport_ReportsSuccessfulLifecycle()
        {
            INetworkTransport transport = new SuccessfulMockTransport();

            var result = await transport.ConnectAsync("edit-mode-contract", 1f);

            Assert.That(result.Status, Is.EqualTo(NetworkConnectionStatus.Connected));
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(transport.IsConnected, Is.True);

            await transport.DisconnectAsync();
            Assert.That(transport.IsConnected, Is.False);
        }

        private sealed class SuccessfulMockTransport : INetworkTransport
        {
            public bool IsConnected { get; private set; }

            public Task<NetworkConnectionResult> ConnectAsync(
                string sessionName,
                float timeoutSeconds,
                CancellationToken cancellationToken = default)
            {
                IsConnected = true;
                return Task.FromResult(new NetworkConnectionResult(
                    NetworkConnectionStatus.Connected,
                    string.Empty));
            }

            public Task DisconnectAsync()
            {
                IsConnected = false;
                return Task.CompletedTask;
            }
        }
    }
}
