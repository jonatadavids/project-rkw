using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace RKW.Backend
{
    /// <summary>
    /// Initializes only the approved UGS development environment and performs
    /// the default anonymous sign-in required by M1-T05. Initialization and
    /// authentication have finite configurable timeouts; caller cancellation is
    /// best-effort when the wrapped SDK operation is already in progress.
    /// </summary>
    public sealed class UgsAuthenticationService
    {
        public const string EnvironmentName = "development";

        private readonly object _operationGate = new object();
        private readonly IUgsAuthenticationClient _client;
        private readonly UgsOperationTimeouts _timeouts;
        private Task _initializationTask;
        private Task _authenticationTask;

        public UgsAuthenticationService(UgsOperationTimeouts timeouts = null)
            : this(new UnityUgsAuthenticationClient(), timeouts)
        {
        }

        internal UgsAuthenticationService(
            IUgsAuthenticationClient client,
            UgsOperationTimeouts timeouts = null)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _timeouts = timeouts ?? UgsOperationTimeouts.Default;
        }

        public bool IsSignedIn => _client.IsSignedIn;

        public async Task<bool> SignInAnonymouslyAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var initialization = GetOrStartInitialization();
                await UgsOperationCoordinator.WaitAsync(
                    initialization,
                    _timeouts.Initialization,
                    cancellationToken,
                    "UGS development initialization");

                cancellationToken.ThrowIfCancellationRequested();

                var authentication = GetOrStartAuthentication();
                await UgsOperationCoordinator.WaitAsync(
                    authentication,
                    _timeouts.Authentication,
                    cancellationToken,
                    "UGS development anonymous authentication");

                cancellationToken.ThrowIfCancellationRequested();
                Debug.Log("UGS development anonymous authentication succeeded.");
                return true;
            }
            catch (TimeoutException)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"UGS development anonymous authentication failed ({exception.GetType().Name}).");
                return false;
            }
        }

        private Task GetOrStartInitialization()
        {
            lock (_operationGate)
            {
                if (_client.IsServicesInitialized)
                {
                    if (!_client.IsDevelopmentEnvironmentConfirmed)
                    {
                        return Task.FromException(new InvalidOperationException(
                            "UGS is initialized without confirmed development configuration."));
                    }

                    return Task.CompletedTask;
                }

                if (_initializationTask == null || _initializationTask.IsCompleted)
                {
                    _initializationTask = _client.InitializeAsync(EnvironmentName);
                }

                return _initializationTask;
            }
        }

        private Task GetOrStartAuthentication()
        {
            lock (_operationGate)
            {
                if (_client.IsSignedIn)
                {
                    return Task.CompletedTask;
                }

                if (_authenticationTask == null || _authenticationTask.IsCompleted)
                {
                    _authenticationTask = _client.SignInAnonymouslyAsync();
                }

                return _authenticationTask;
            }
        }
    }
}
