using System;
using System.Threading;
using System.Threading.Tasks;
using RKW.Backend;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RKW.UI
{
    public sealed class BootstrapController : MonoBehaviour
    {
        internal static Func<IAuthenticationService> AuthenticationFactoryOverride;

        [SerializeField] private BootstrapStatusView statusView;
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        private CancellationTokenSource _lifetimeCancellation;
        private IAuthenticationService _authenticationService;
        private bool _operationInProgress;

        private void Awake()
        {
            _lifetimeCancellation = new CancellationTokenSource();
            _authenticationService = AuthenticationFactoryOverride?.Invoke()
                ?? new UgsAuthenticationService();
        }

        private void Start()
        {
            BeginAuthentication();
        }

        private void OnDestroy()
        {
            _lifetimeCancellation?.Cancel();
            _lifetimeCancellation?.Dispose();
            _lifetimeCancellation = null;
        }

        internal static void ResetTestOverrides()
        {
            AuthenticationFactoryOverride = null;
        }

        private async void BeginAuthentication()
        {
            if (_operationInProgress || _lifetimeCancellation == null)
            {
                return;
            }

            _operationInProgress = true;
            statusView.gameObject.SetActive(true);
            statusView.ShowLoading();
            var shouldShowFailure = false;

            try
            {
                var authenticated = _authenticationService.IsSignedIn
                    || await _authenticationService.SignInAnonymouslyAsync(
                        _lifetimeCancellation.Token);

                if (!authenticated || _lifetimeCancellation.IsCancellationRequested)
                {
                    shouldShowFailure = true;
                }
                else
                {
                    await LoadMainMenuAsync(_lifetimeCancellation.Token);
                    if (!_lifetimeCancellation.IsCancellationRequested)
                    {
                        statusView.Hide();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Destruction owns cancellation; there is no user-facing error to show.
            }
            catch (Exception)
            {
                shouldShowFailure = true;
            }
            finally
            {
                _operationInProgress = false;
            }

            if (shouldShowFailure)
            {
                ShowFailureUnlessDestroyed();
            }
        }

        private async Task LoadMainMenuAsync(CancellationToken cancellationToken)
        {
            var existing = SceneManager.GetSceneByName(mainMenuSceneName);
            if (!existing.isLoaded)
            {
                var loadOperation = SceneManager.LoadSceneAsync(
                    mainMenuSceneName,
                    LoadSceneMode.Additive);

                if (loadOperation == null)
                {
                    throw new InvalidOperationException("Main menu loading could not be started.");
                }

                while (!loadOperation.isDone)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Yield();
                }

                existing = SceneManager.GetSceneByName(mainMenuSceneName);
            }

            cancellationToken.ThrowIfCancellationRequested();
            if (!existing.isLoaded)
            {
                throw new InvalidOperationException("Main menu did not finish loading.");
            }

            SceneManager.SetActiveScene(existing);
        }

        private void ShowFailureUnlessDestroyed()
        {
            if (_lifetimeCancellation == null || _lifetimeCancellation.IsCancellationRequested)
            {
                return;
            }

            statusView.ShowFailure(BeginAuthentication);
        }
    }
}
