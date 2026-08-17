using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using Unity.Services.RemoteConfig;

namespace RKW.Backend
{
    internal interface IUgsAuthenticationClient
    {
        bool IsServicesInitialized { get; }
        bool IsDevelopmentEnvironmentConfirmed { get; }
        bool IsSignedIn { get; }
        Task InitializeAsync(string environmentName);
        Task SignInAnonymouslyAsync();
    }

    internal interface IUgsCloudDataClient
    {
        bool IsAuthenticatedForDevelopment { get; }
        Task SaveJsonAsync(string key, string json);
        Task<string> LoadJsonAsync(string key);
    }

    internal interface IRemoteConfigClient
    {
        bool IsAuthenticatedForDevelopment { get; }

        Task<RemoteFeatureFlags> FetchAsync();
    }

    internal static class UgsRuntimeEnvironment
    {
        private static string s_confirmedEnvironment;

        internal static bool IsDevelopmentConfirmed => string.Equals(
            s_confirmedEnvironment,
            UgsAuthenticationService.EnvironmentName,
            StringComparison.Ordinal);

        internal static void Confirm(string environmentName)
        {
            if (!string.Equals(
                    environmentName,
                    UgsAuthenticationService.EnvironmentName,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "The UGS foundation can initialize only the development environment.");
            }

            s_confirmedEnvironment = environmentName;
        }
    }

    internal sealed class UnityUgsAuthenticationClient : IUgsAuthenticationClient
    {
        public bool IsServicesInitialized =>
            UnityServices.State == ServicesInitializationState.Initialized;

        public bool IsDevelopmentEnvironmentConfirmed =>
            UgsRuntimeEnvironment.IsDevelopmentConfirmed;

        public bool IsSignedIn =>
            IsServicesInitialized &&
            IsDevelopmentEnvironmentConfirmed &&
            AuthenticationService.Instance.IsSignedIn;

        public async Task InitializeAsync(string environmentName)
        {
            if (UnityServices.State != ServicesInitializationState.Uninitialized)
            {
                if (!IsServicesInitialized || !IsDevelopmentEnvironmentConfirmed)
                {
                    throw new InvalidOperationException(
                        "UGS was initialized or started outside the approved development foundation.");
                }

                return;
            }

            var options = new InitializationOptions().SetEnvironmentName(environmentName);
            await UnityServices.InitializeAsync(options);
            UgsRuntimeEnvironment.Confirm(environmentName);
        }

        public Task SignInAnonymouslyAsync()
        {
            if (!IsDevelopmentEnvironmentConfirmed)
            {
                throw new InvalidOperationException(
                    "UGS development must be confirmed before anonymous authentication.");
            }

            return AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    internal sealed class UnityUgsCloudDataClient : IUgsCloudDataClient
    {
        public bool IsAuthenticatedForDevelopment =>
            UgsRuntimeEnvironment.IsDevelopmentConfirmed &&
            UnityServices.State == ServicesInitializationState.Initialized &&
            AuthenticationService.Instance.IsSignedIn;

        public async Task SaveJsonAsync(string key, string json)
        {
            await CloudSaveService.Instance.Data.Player.SaveAsync(
                new Dictionary<string, object>
                {
                    { key, json }
                });
        }

        public async Task<string> LoadJsonAsync(string key)
        {
            var values = await CloudSaveService.Instance.Data.Player.LoadAsync(
                new HashSet<string> { key });

            if (!values.TryGetValue(key, out var item))
            {
                throw new KeyNotFoundException(
                    "The requested Cloud Save JSON key does not exist.");
            }

            return item.Value.GetAsString();
        }
    }

    internal sealed class UnityRemoteConfigClient : IRemoteConfigClient
    {
        private struct EmptyUserAttributes
        {
        }

        private struct EmptyAppAttributes
        {
        }

        public bool IsAuthenticatedForDevelopment =>
            UgsRuntimeEnvironment.IsDevelopmentConfirmed &&
            UnityServices.State == ServicesInitializationState.Initialized &&
            AuthenticationService.Instance.IsSignedIn;

        public async Task<RemoteFeatureFlags> FetchAsync()
        {
            if (!IsAuthenticatedForDevelopment)
            {
                throw new InvalidOperationException(
                    "Remote Config requires authenticated UGS development initialization.");
            }

            var config = await RemoteConfigService.Instance.FetchConfigsAsync(
                new EmptyUserAttributes(),
                new EmptyAppAttributes());

            return new RemoteFeatureFlags(
                config.GetBool(RemoteFeatureFlags.EnableMultiplayerKey, false),
                config.GetBool(RemoteFeatureFlags.EnableChampionshipKey, false),
                config.GetBool(RemoteFeatureFlags.EnableSchoolKey, false),
                config.GetBool(RemoteFeatureFlags.EnableAdsKey, false));
        }
    }
}
