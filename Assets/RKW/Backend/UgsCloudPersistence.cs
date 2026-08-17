using System;
using System.Threading;
using System.Threading.Tasks;

namespace RKW.Backend
{
    /// <summary>
    /// Minimal Cloud Save player-data adapter for versioned JSON strings.
    /// It validates JSON syntax and applies finite configurable timeouts. Caller
    /// cancellation is best-effort because the wrapped SDK operations do not take
    /// a CancellationToken. It deliberately contains no profile reconciliation
    /// or offline cache.
    /// </summary>
    public sealed class UgsCloudPersistence : ICloudPersistence
    {
        /// <summary>
        /// Defensive limit for one JSON value in this foundation. This does not
        /// enforce the future aggregate profile budget across multiple keys.
        /// </summary>
        public const int MaxJsonPayloadBytes = CloudPersistenceValidation.MaximumJsonBytes;

        private readonly IUgsCloudDataClient _client;
        private readonly UgsOperationTimeouts _timeouts;

        public UgsCloudPersistence(UgsOperationTimeouts timeouts = null)
            : this(new UnityUgsCloudDataClient(), timeouts)
        {
        }

        internal UgsCloudPersistence(
            IUgsCloudDataClient client,
            UgsOperationTimeouts timeouts = null)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _timeouts = timeouts ?? UgsOperationTimeouts.Default;
        }

        public async Task SaveJsonAsync(
            string key,
            string json,
            CancellationToken cancellationToken = default)
        {
            key = CloudPersistenceValidation.RequiredKey(key);
            json = CloudPersistenceValidation.RequiredJson(json);
            cancellationToken.ThrowIfCancellationRequested();
            EnsureAuthenticated();

            var operation = _client.SaveJsonAsync(key, json);
            await UgsOperationCoordinator.WaitAsync(
                operation,
                _timeouts.Save,
                cancellationToken,
                "UGS development Cloud Save write");

            cancellationToken.ThrowIfCancellationRequested();
        }

        public async Task<string> LoadJsonAsync(
            string key,
            CancellationToken cancellationToken = default)
        {
            key = CloudPersistenceValidation.RequiredKey(key);
            cancellationToken.ThrowIfCancellationRequested();
            EnsureAuthenticated();

            var operation = _client.LoadJsonAsync(key);
            var json = await UgsOperationCoordinator.WaitAsync(
                operation,
                _timeouts.Load,
                cancellationToken,
                "UGS development Cloud Save read");

            cancellationToken.ThrowIfCancellationRequested();
            return CloudPersistenceValidation.RequiredJson(json);
        }

        private void EnsureAuthenticated()
        {
            if (!_client.IsAuthenticatedForDevelopment)
            {
                throw new InvalidOperationException(
                    "Anonymous UGS authentication in development is required before Cloud Save access.");
            }
        }
    }
}
