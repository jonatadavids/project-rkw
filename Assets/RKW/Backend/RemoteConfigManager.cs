using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace RKW.Backend
{
    /// <summary>
    /// Immutable, allow-list-only feature flags. A remote value can enable an
    /// implementation already shipped in the app; it cannot introduce content.
    /// </summary>
    public readonly struct RemoteFeatureFlags
    {
        public const string EnableMultiplayerKey = "enable_multiplayer";
        public const string EnableChampionshipKey = "enable_championship";
        public const string EnableSchoolKey = "enable_school";
        public const string EnableAdsKey = "enable_ads";

        public static RemoteFeatureFlags SafeDefaults => new RemoteFeatureFlags(
            false,
            false,
            false,
            false);

        public RemoteFeatureFlags(
            bool enableMultiplayer,
            bool enableChampionship,
            bool enableSchool,
            bool enableAds)
        {
            EnableMultiplayer = enableMultiplayer;
            EnableChampionship = enableChampionship;
            EnableSchool = enableSchool;
            EnableAds = enableAds;
        }

        public bool EnableMultiplayer { get; }
        public bool EnableChampionship { get; }
        public bool EnableSchool { get; }
        public bool EnableAds { get; }
    }

    /// <summary>
    /// Boundary consumed by Bootstrap after development authentication succeeds.
    /// </summary>
    public interface IRemoteConfigService
    {
        RemoteFeatureFlags Flags { get; }

        Task<RemoteFeatureFlags> LoadAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Fetches the small approved feature-flag allow-list from UGS development.
    /// Failure and timeout retain safe local defaults so menu loading can proceed.
    /// </summary>
    public sealed class RemoteConfigManager : IRemoteConfigService
    {
        public static readonly TimeSpan DefaultFetchTimeout = TimeSpan.FromSeconds(10);

        private readonly object _operationGate = new object();
        private readonly IRemoteConfigClient _client;
        private readonly TimeSpan _fetchTimeout;
        private Task<RemoteFeatureFlags> _fetchTask;

        public RemoteConfigManager(TimeSpan? fetchTimeout = null)
            : this(new UnityRemoteConfigClient(), fetchTimeout)
        {
        }

        internal RemoteConfigManager(
            IRemoteConfigClient client,
            TimeSpan? fetchTimeout = null)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _fetchTimeout = RequiredFinite(fetchTimeout ?? DefaultFetchTimeout);
            Flags = RemoteFeatureFlags.SafeDefaults;
        }

        public RemoteFeatureFlags Flags { get; private set; }

        public async Task<RemoteFeatureFlags> LoadAsync(
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_client.IsAuthenticatedForDevelopment)
            {
                return UseSafeDefaults("authentication was not confirmed");
            }

            try
            {
                var flags = await UgsOperationCoordinator.WaitAsync(
                    GetOrStartFetch(),
                    _fetchTimeout,
                    cancellationToken,
                    "UGS development Remote Config fetch");
                cancellationToken.ThrowIfCancellationRequested();
                Flags = flags;
                Debug.Log("UGS development Remote Config fetch succeeded.");
                return Flags;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                return UseSafeDefaults(exception.GetType().Name);
            }
        }

        private Task<RemoteFeatureFlags> GetOrStartFetch()
        {
            lock (_operationGate)
            {
                if (_fetchTask == null || _fetchTask.IsCompleted)
                {
                    _fetchTask = _client.FetchAsync();
                }

                return _fetchTask;
            }
        }

        private RemoteFeatureFlags UseSafeDefaults(string reason)
        {
            Flags = RemoteFeatureFlags.SafeDefaults;
            Debug.LogWarning(
                $"UGS development Remote Config unavailable ({reason}); local feature defaults remain active.");
            return Flags;
        }

        private static TimeSpan RequiredFinite(TimeSpan value)
        {
            if (value <= TimeSpan.Zero || value.TotalMilliseconds > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(value),
                    "Remote Config timeout must be finite, positive, and supported by CancellationTokenSource.");
            }

            return value;
        }
    }
}
