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
        private sealed class ConnectionAttempt
        {
            public ConnectionAttempt(NetworkRunner runner, CancellationTokenSource cancellation)
            {
                Runner = runner;
                Cancellation = cancellation;
            }

            public NetworkRunner Runner { get; }
            public CancellationTokenSource Cancellation { get; }
            public Task<StartOutcome> StartTask { get; set; }
            public Task ReleaseTask { get; set; }
            public bool StopRequested { get; set; }
            public bool Connected { get; set; }
        }

        private readonly struct StartOutcome
        {
            public StartOutcome(bool succeeded, string failureReason)
            {
                Succeeded = succeeded;
                FailureReason = failureReason ?? string.Empty;
            }

            public bool Succeeded { get; }
            public string FailureReason { get; }
        }

        private readonly object _lifecycleGate = new object();
        private ConnectionAttempt _activeAttempt;
        private bool _destroyed;

        // Deterministic seam for lifecycle tests. Production always uses NetworkRunner.StartGame.
        internal Func<CancellationToken, Task> StartGameOverride { get; set; }

        public bool IsConnected
        {
            get
            {
                lock (_lifecycleGate)
                {
                    return !_destroyed &&
                           _activeAttempt != null &&
                           !_activeAttempt.StopRequested &&
                           _activeAttempt.Connected &&
                           _activeAttempt.Runner != null &&
                           _activeAttempt.Runner.IsRunning;
                }
            }
        }

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

            if (cancellationToken.IsCancellationRequested)
            {
                return new NetworkConnectionResult(NetworkConnectionStatus.Cancelled, "Cancelled");
            }

            ConnectionAttempt attempt;
            lock (_lifecycleGate)
            {
                if (_destroyed)
                {
                    return new NetworkConnectionResult(NetworkConnectionStatus.Cancelled, "Transport destroyed");
                }

                if (_activeAttempt != null)
                {
                    if (IsConnected)
                    {
                        return new NetworkConnectionResult(NetworkConnectionStatus.Connected, string.Empty);
                    }

                    throw new InvalidOperationException("A Photon connection attempt is already active.");
                }

                var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                linkedCancellation.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

                var runnerObject = new GameObject("RKW Photon Development Runner");
                runnerObject.transform.SetParent(transform, false);
                var runner = runnerObject.AddComponent<NetworkRunner>();
                runner.ProvideInput = false;

                attempt = new ConnectionAttempt(runner, linkedCancellation);
                _activeAttempt = attempt;
                attempt.StartTask = StartAttemptAsync(attempt, sessionName);
            }

            try
            {
                var outcome = await attempt.StartTask;
                var acceptConnection = false;

                lock (_lifecycleGate)
                {
                    if (outcome.Succeeded &&
                        ReferenceEquals(_activeAttempt, attempt) &&
                        !_destroyed &&
                        !attempt.StopRequested &&
                        !attempt.Cancellation.IsCancellationRequested)
                    {
                        attempt.Connected = true;
                        acceptConnection = true;
                    }
                }

                if (acceptConnection)
                {
                    Debug.Log("Connected to Photon");
                    return new NetworkConnectionResult(NetworkConnectionStatus.Connected, string.Empty);
                }

                var stoppedStatus = ResolveStoppedStatus(attempt, cancellationToken);
                await EnsureRunnerReleasedAsync(attempt);

                if (stoppedStatus.HasValue)
                {
                    Debug.LogWarning($"Photon connection ended with status {stoppedStatus.Value}.");
                    return new NetworkConnectionResult(stoppedStatus.Value, stoppedStatus.Value.ToString());
                }

                Debug.LogWarning($"Photon connection failed ({outcome.FailureReason}).");
                return new NetworkConnectionResult(NetworkConnectionStatus.Failed, outcome.FailureReason);
            }
            catch (OperationCanceledException)
            {
                var status = ResolveStoppedStatus(attempt, cancellationToken) ?? NetworkConnectionStatus.Cancelled;
                await EnsureRunnerReleasedAsync(attempt);
                Debug.LogWarning($"Photon connection ended with status {status}.");
                return new NetworkConnectionResult(status, status.ToString());
            }
            catch (Exception exception)
            {
                await EnsureRunnerReleasedAsync(attempt);
                Debug.LogWarning($"Photon connection failed ({exception.GetType().Name}).");
                return new NetworkConnectionResult(NetworkConnectionStatus.Failed, exception.GetType().Name);
            }
            finally
            {
                lock (_lifecycleGate)
                {
                    if (!attempt.Connected && ReferenceEquals(_activeAttempt, attempt))
                    {
                        _activeAttempt = null;
                    }
                }
            }
        }

        public async Task DisconnectAsync()
        {
            ConnectionAttempt attempt;
            lock (_lifecycleGate)
            {
                attempt = _activeAttempt;
                if (attempt == null)
                {
                    return;
                }

                attempt.StopRequested = true;
                attempt.Connected = false;
                attempt.Cancellation.Cancel();
            }

            await EnsureRunnerReleasedAsync(attempt);

            lock (_lifecycleGate)
            {
                if (ReferenceEquals(_activeAttempt, attempt))
                {
                    _activeAttempt = null;
                }
            }
        }

        private async Task<StartOutcome> StartAttemptAsync(ConnectionAttempt attempt, string sessionName)
        {
            if (StartGameOverride != null)
            {
                await StartGameOverride(attempt.Cancellation.Token);
                return new StartOutcome(true, string.Empty);
            }

            var result = await attempt.Runner.StartGame(new StartGameArgs
            {
                GameMode = Fusion.GameMode.Shared,
                SessionName = sessionName,
                PlayerCount = 1,
                IsOpen = false,
                IsVisible = false,
                StartGameCancellationToken = attempt.Cancellation.Token
            });

            return new StartOutcome(result.Ok, result.ShutdownReason.ToString());
        }

        private NetworkConnectionStatus? ResolveStoppedStatus(
            ConnectionAttempt attempt,
            CancellationToken externalCancellation)
        {
            lock (_lifecycleGate)
            {
                if (_destroyed || attempt.StopRequested || externalCancellation.IsCancellationRequested)
                {
                    return NetworkConnectionStatus.Cancelled;
                }

                return attempt.Cancellation.IsCancellationRequested
                    ? NetworkConnectionStatus.TimedOut
                    : (NetworkConnectionStatus?)null;
            }
        }

        private Task EnsureRunnerReleasedAsync(ConnectionAttempt attempt)
        {
            lock (_lifecycleGate)
            {
                if (attempt.ReleaseTask == null)
                {
                    attempt.ReleaseTask = ReleaseRunnerAsync(attempt);
                }

                return attempt.ReleaseTask;
            }
        }

        private static async Task ReleaseRunnerAsync(ConnectionAttempt attempt)
        {
            try
            {
                if (attempt.StartTask != null)
                {
                    try
                    {
                        await attempt.StartTask;
                    }
                    catch
                    {
                        // Start failures are converted to a connection result by ConnectAsync.
                    }
                }

                if (attempt.Runner != null && attempt.Runner.IsRunning)
                {
                    await attempt.Runner.Shutdown();
                }
            }
            finally
            {
                if (attempt.Runner != null)
                {
                    Destroy(attempt.Runner.gameObject);
                }

                attempt.Cancellation.Dispose();
            }
        }

        private void OnDestroy()
        {
            ConnectionAttempt attempt;
            lock (_lifecycleGate)
            {
                _destroyed = true;
                attempt = _activeAttempt;
                if (attempt == null)
                {
                    return;
                }

                attempt.StopRequested = true;
                attempt.Connected = false;
                attempt.Cancellation.Cancel();
            }

            _ = EnsureRunnerReleasedAsync(attempt);
        }
    }
}
