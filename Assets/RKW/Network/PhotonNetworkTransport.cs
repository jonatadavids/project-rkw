using System;
using System.Threading;
using System.Threading.Tasks;
using Fusion;
using UnityEngine;

namespace RKW.Network
{
    /// <summary>
    /// Minimal Photon Fusion 2 connection lifecycle for development validation.
    /// Uses Shared Mode and deliberately contains no gameplay networking.
    /// </summary>
    public sealed class PhotonNetworkTransport : MonoBehaviour, INetworkTransport
    {
        private NetworkRunner _runner;
        private CancellationTokenSource _activeConnection;

        public bool IsConnected => _runner != null && _runner.IsRunning;

        internal static int ActiveRunnerCount =>
            FindObjectsByType<NetworkRunner>(FindObjectsSortMode.None).Length;

        public async Task<NetworkConnectionResult> ConnectAsync(
            string sessionName,
            float timeoutSeconds,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(sessionName))
            {
                throw new ArgumentException("A development session name is required.", nameof(sessionName));
            }

            if (timeoutSeconds <= 0f || float.IsNaN(timeoutSeconds) || float.IsInfinity(timeoutSeconds))
            {
                throw new ArgumentOutOfRangeException(nameof(timeoutSeconds), "Timeout must be finite and positive.");
            }

            if (_activeConnection != null)
            {
                throw new InvalidOperationException("A Photon connection attempt is already active.");
            }

            if (IsConnected)
            {
                return new NetworkConnectionResult(NetworkConnectionStatus.Connected, string.Empty);
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return new NetworkConnectionResult(NetworkConnectionStatus.Cancelled, "Cancelled");
            }

            using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            linkedCancellation.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
            _activeConnection = linkedCancellation;

            var runnerObject = new GameObject("RKW Photon Development Runner");
            runnerObject.transform.SetParent(transform, false);
            _runner = runnerObject.AddComponent<NetworkRunner>();
            _runner.ProvideInput = false;

            try
            {
                var result = await _runner.StartGame(new StartGameArgs
                {
                    GameMode = Fusion.GameMode.Shared,
                    SessionName = sessionName,
                    PlayerCount = 1,
                    IsOpen = false,
                    IsVisible = false,
                    StartGameCancellationToken = linkedCancellation.Token
                });

                if (result.Ok)
                {
                    Debug.Log("Connected to Photon");
                    return new NetworkConnectionResult(NetworkConnectionStatus.Connected, string.Empty);
                }

                if (linkedCancellation.IsCancellationRequested)
                {
                    var cancelledStatus = cancellationToken.IsCancellationRequested
                        ? NetworkConnectionStatus.Cancelled
                        : NetworkConnectionStatus.TimedOut;

                    await ReleaseRunnerAsync();
                    Debug.LogWarning($"Photon connection ended with status {cancelledStatus}.");
                    return new NetworkConnectionResult(cancelledStatus, cancelledStatus.ToString());
                }

                await ReleaseRunnerAsync();
                Debug.LogWarning($"Photon connection failed ({result.ShutdownReason}).");
                return new NetworkConnectionResult(NetworkConnectionStatus.Failed, result.ShutdownReason.ToString());
            }
            catch (OperationCanceledException)
            {
                var status = cancellationToken.IsCancellationRequested
                    ? NetworkConnectionStatus.Cancelled
                    : NetworkConnectionStatus.TimedOut;

                await ReleaseRunnerAsync();
                Debug.LogWarning($"Photon connection ended with status {status}.");
                return new NetworkConnectionResult(status, status.ToString());
            }
            catch (Exception exception)
            {
                await ReleaseRunnerAsync();
                Debug.LogWarning($"Photon connection failed ({exception.GetType().Name}).");
                return new NetworkConnectionResult(NetworkConnectionStatus.Failed, exception.GetType().Name);
            }
            finally
            {
                _activeConnection = null;
            }
        }

        public async Task DisconnectAsync()
        {
            _activeConnection?.Cancel();
            await ReleaseRunnerAsync();
        }

        private async Task ReleaseRunnerAsync()
        {
            if (_runner == null)
            {
                return;
            }

            var runner = _runner;
            _runner = null;

            if (runner.IsRunning)
            {
                await runner.Shutdown();
            }

            if (runner != null)
            {
                Destroy(runner.gameObject);
            }
        }

        private void OnDestroy()
        {
            _activeConnection?.Cancel();

            if (_runner != null)
            {
                _runner.Shutdown();
                _runner = null;
            }
        }
    }
}
