using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fusion;
using Fusion.Photon.Realtime;

namespace RKW.Network
{
    public enum PhotonRegionDiscoveryStatus
    {
        Succeeded,
        NoRegionsReturned,
        Failed
    }

    /// <summary>
    /// Sanitized local settings relevant to Photon region discovery. It deliberately
    /// records only presence/configuration state, never the App ID or FixedRegion value.
    /// </summary>
    public readonly struct PhotonRegionDiscoveryConfiguration
    {
        public PhotonRegionDiscoveryConfiguration(bool appIdPresent, bool useNameServer, bool fixedRegionConfigured, string connectionProtocol)
        {
            AppIdPresent = appIdPresent;
            UseNameServer = useNameServer;
            FixedRegionConfigured = fixedRegionConfigured;
            ConnectionProtocol = connectionProtocol ?? "unknown";
        }

        public bool AppIdPresent { get; }
        public bool UseNameServer { get; }
        public bool FixedRegionConfigured { get; }
        public string ConnectionProtocol { get; }
    }

    /// <summary>
    /// Outcome of one global Fusion discovery. A non-empty response can still omit a
    /// candidate region or contain an invalid ping; those are not global failures.
    /// </summary>
    public readonly struct PhotonRegionDiscoveryResult
    {
        private PhotonRegionDiscoveryResult(PhotonRegionDiscoveryStatus status, IReadOnlyList<PhotonRegionPing> regions, string failureKind)
        {
            Status = status;
            Regions = regions ?? Array.Empty<PhotonRegionPing>();
            FailureKind = failureKind;
        }

        public PhotonRegionDiscoveryStatus Status { get; }
        public IReadOnlyList<PhotonRegionPing> Regions { get; }
        public int ReturnedRegionCount => Regions.Count;
        public string FailureKind { get; }

        public static PhotonRegionDiscoveryResult Succeeded(IReadOnlyList<PhotonRegionPing> regions)
        {
            if (regions == null)
            {
                throw new ArgumentNullException(nameof(regions));
            }

            return regions.Count == 0
                ? NoRegionsReturned()
                : new PhotonRegionDiscoveryResult(PhotonRegionDiscoveryStatus.Succeeded, regions, null);
        }

        public static PhotonRegionDiscoveryResult NoRegionsReturned()
        {
            return new PhotonRegionDiscoveryResult(PhotonRegionDiscoveryStatus.NoRegionsReturned, Array.Empty<PhotonRegionPing>(), null);
        }

        public static PhotonRegionDiscoveryResult Failed(Exception exception)
        {
            return new PhotonRegionDiscoveryResult(PhotonRegionDiscoveryStatus.Failed, Array.Empty<PhotonRegionPing>(), exception?.GetType().Name ?? "UnknownFailure");
        }
    }

    public readonly struct PhotonRegionPing
    {
        public PhotonRegionPing(string regionCode, int pingMilliseconds)
        {
            RegionCode = regionCode ?? string.Empty;
            PingMilliseconds = pingMilliseconds;
        }

        public string RegionCode { get; }
        public int PingMilliseconds { get; }
        public bool IsSuccess => !string.IsNullOrEmpty(RegionCode) && PingMilliseconds >= 0;
    }

    /// <summary>
    /// One cached-capable discovery snapshot returned by Fusion. It must not be treated
    /// as an independent time series or used to calculate median, P95, jitter or loss.
    /// </summary>
    public readonly struct PhotonRegionDiscoverySnapshot
    {
        private static readonly string[] CandidateRegions = { "sa", "ussc", "us" };

        public PhotonRegionDiscoverySnapshot(DateTimeOffset capturedAtUtc, PhotonRegionDiscoveryResult result, PhotonRegionDiscoveryConfiguration configuration)
        {
            CapturedAtUtc = capturedAtUtc;
            Status = result.Status;
            FailureKind = result.FailureKind;
            Regions = result.Regions;
            Configuration = configuration;
            RecommendedRegion = RecommendCandidate(result.Regions);
        }

        public DateTimeOffset CapturedAtUtc { get; }
        public PhotonRegionDiscoveryStatus Status { get; }
        public string FailureKind { get; }
        public IReadOnlyList<PhotonRegionPing> Regions { get; }
        public int ReturnedRegionCount => Regions.Count;
        public PhotonRegionDiscoveryConfiguration Configuration { get; }
        public PhotonRegionPing? RecommendedRegion { get; }

        public PhotonRegionPing? FindRegion(string regionCode)
        {
            foreach (var region in Regions)
            {
                if (string.Equals(region.RegionCode, regionCode, StringComparison.Ordinal))
                {
                    return region;
                }
            }

            return null;
        }

        public string ToSanitizedLog()
        {
            var configuration = string.Format(
                CultureInfo.InvariantCulture,
                "appIdPresent={0};useNameServer={1};fixedRegion={2};protocol={3}",
                Configuration.AppIdPresent ? "yes" : "no",
                Configuration.UseNameServer ? "yes" : "no",
                Configuration.FixedRegionConfigured ? "configured" : "empty",
                Configuration.ConnectionProtocol);
            if (Status != PhotonRegionDiscoveryStatus.Succeeded)
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "Photon region snapshot: status={0};returned={1};failure={2};{3}.",
                    Status,
                    ReturnedRegionCount,
                    FailureKind ?? "none",
                    configuration);
            }

            var candidateValues = new List<string>();
            foreach (var regionCode in CandidateRegions)
            {
                var region = FindRegion(regionCode);
                if (!region.HasValue)
                {
                    candidateValues.Add($"{regionCode}=unavailable");
                    continue;
                }

                candidateValues.Add(region.Value.IsSuccess
                    ? string.Format(CultureInfo.InvariantCulture, "{0}={1}ms", regionCode, region.Value.PingMilliseconds)
                    : $"{regionCode}=invalidPing");
            }

            var candidates = string.Join(", ", candidateValues);
            return $"Photon region snapshot: returned={ReturnedRegionCount}; {candidates}; recommended={RecommendedRegion?.RegionCode ?? "none"}; {configuration}.";
        }

        private static PhotonRegionPing? RecommendCandidate(IEnumerable<PhotonRegionPing> regions)
        {
            return regions
                .Where(region => region.IsSuccess && CandidateRegions.Contains(region.RegionCode, StringComparer.Ordinal))
                .OrderBy(region => region.PingMilliseconds)
                .ThenBy(region => region.RegionCode, StringComparer.Ordinal)
                .Cast<PhotonRegionPing?>()
                .FirstOrDefault();
        }
    }

    /// <summary>
    /// Pure aggregation reserved for independent heartbeat samples from a real Photon
    /// session. Discovery snapshots from GetAvailableRegions must never be passed here.
    /// </summary>
    public static class PhotonHeartbeatLatencyStatistics
    {
        public static PhotonHeartbeatLatencySummary Summarize(string regionCode, IReadOnlyList<int> independentPingsMilliseconds, int failedHeartbeats)
        {
            if (independentPingsMilliseconds == null)
            {
                throw new ArgumentNullException(nameof(independentPingsMilliseconds));
            }

            if (failedHeartbeats < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(failedHeartbeats));
            }

            var samples = independentPingsMilliseconds.ToArray();
            if (samples.Any(sample => sample < 0))
            {
                throw new ArgumentOutOfRangeException(nameof(independentPingsMilliseconds));
            }

            if (samples.Length == 0)
            {
                return new PhotonHeartbeatLatencySummary(regionCode, 0, failedHeartbeats, 0, 0d, 0d, 0, 0, 0d);
            }

            var ordered = samples.OrderBy(sample => sample).ToArray();
            var variationTotal = 0d;
            for (var index = 1; index < samples.Length; index++)
            {
                variationTotal += Math.Abs(samples[index] - samples[index - 1]);
            }

            return new PhotonHeartbeatLatencySummary(
                regionCode,
                samples.Length,
                failedHeartbeats,
                ordered[0],
                samples.Average(),
                Median(ordered),
                NearestRank(ordered, 0.95d),
                ordered[ordered.Length - 1],
                samples.Length > 1 ? variationTotal / (samples.Length - 1) : 0d);
        }

        public static PhotonHeartbeatLatencySummary? Recommend(IEnumerable<PhotonHeartbeatLatencySummary> summaries)
        {
            if (summaries == null)
            {
                throw new ArgumentNullException(nameof(summaries));
            }

            var candidates = summaries.Where(summary => summary.ValidSamples > 0).ToArray();
            if (candidates.Length == 0)
            {
                return null;
            }

            return candidates
                .OrderBy(summary => summary.MedianMilliseconds)
                .ThenBy(summary => summary.P95Milliseconds)
                .ThenBy(summary => summary.FailurePercent)
                .ThenBy(summary => summary.RegionCode, StringComparer.Ordinal)
                .First();
        }

        private static double Median(IReadOnlyList<int> ordered)
        {
            var middle = ordered.Count / 2;
            return ordered.Count % 2 == 0 ? (ordered[middle - 1] + ordered[middle]) / 2d : ordered[middle];
        }

        private static int NearestRank(IReadOnlyList<int> ordered, double percentile)
        {
            var oneBasedIndex = Math.Max(1, (int)Math.Ceiling(percentile * ordered.Count));
            return ordered[oneBasedIndex - 1];
        }
    }

    public readonly struct PhotonHeartbeatLatencySummary
    {
        public PhotonHeartbeatLatencySummary(string regionCode, int validSamples, int failedHeartbeats, int minimumMilliseconds, double averageMilliseconds, double medianMilliseconds, int p95Milliseconds, int maximumMilliseconds, double meanAbsoluteVariationMilliseconds)
        {
            if (string.IsNullOrWhiteSpace(regionCode))
            {
                throw new ArgumentException("A Photon region code is required.", nameof(regionCode));
            }

            RegionCode = regionCode;
            ValidSamples = validSamples;
            FailedHeartbeats = failedHeartbeats;
            MinimumMilliseconds = minimumMilliseconds;
            AverageMilliseconds = averageMilliseconds;
            MedianMilliseconds = medianMilliseconds;
            P95Milliseconds = p95Milliseconds;
            MaximumMilliseconds = maximumMilliseconds;
            MeanAbsoluteVariationMilliseconds = meanAbsoluteVariationMilliseconds;
        }

        public string RegionCode { get; }
        public int ValidSamples { get; }
        public int FailedHeartbeats { get; }
        public int TotalAttempts => ValidSamples + FailedHeartbeats;
        public int MinimumMilliseconds { get; }
        public double AverageMilliseconds { get; }
        public double MedianMilliseconds { get; }
        public int P95Milliseconds { get; }
        public int MaximumMilliseconds { get; }
        public double MeanAbsoluteVariationMilliseconds { get; }
        public double FailurePercent => TotalAttempts == 0 ? 0d : (100d * FailedHeartbeats) / TotalAttempts;
    }

    /// <summary>
    /// Reusable single-call Fusion region discovery. Repeated calls are intentionally not
    /// exposed as samples because Fusion 2.1.1 caches region information for 10 seconds.
    /// </summary>
    public sealed class PhotonRegionLatencyProbe
    {
        private readonly Func<CancellationToken, Task<PhotonRegionDiscoveryResult>> _discoverAsync;
        private readonly PhotonRegionDiscoveryConfiguration _configuration;

        public PhotonRegionLatencyProbe()
            : this(DiscoverWithFusionAsync, ReadLocalConfiguration())
        {
        }

        internal PhotonRegionLatencyProbe(Func<CancellationToken, Task<PhotonRegionDiscoveryResult>> discoverAsync, PhotonRegionDiscoveryConfiguration configuration)
        {
            _discoverAsync = discoverAsync ?? throw new ArgumentNullException(nameof(discoverAsync));
            _configuration = configuration;
        }

        public async Task<PhotonRegionDiscoverySnapshot> DiscoverSnapshotAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            PhotonRegionDiscoveryResult result;
            try
            {
                result = await _discoverAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                result = PhotonRegionDiscoveryResult.Failed(exception);
            }

            return new PhotonRegionDiscoverySnapshot(DateTimeOffset.UtcNow, result, _configuration);
        }

        private static PhotonRegionDiscoveryConfiguration ReadLocalConfiguration()
        {
            var appSettings = PhotonAppSettings.Global.AppSettings;
            return new PhotonRegionDiscoveryConfiguration(
                !string.IsNullOrWhiteSpace(appSettings.AppIdFusion),
                appSettings.UseNameServer,
                !string.IsNullOrWhiteSpace(appSettings.FixedRegion),
                appSettings.Protocol.ToString());
        }

        private static async Task<PhotonRegionDiscoveryResult> DiscoverWithFusionAsync(CancellationToken cancellationToken)
        {
            var appId = PhotonAppSettings.Global.AppSettings.AppIdFusion;
            if (string.IsNullOrWhiteSpace(appId))
            {
                return PhotonRegionDiscoveryResult.Failed(new InvalidOperationException("MissingLocalFusionConfiguration"));
            }

            try
            {
                var regions = await NetworkRunner.GetAvailableRegions(appId, cancellationToken);
                var result = new List<PhotonRegionPing>();
                foreach (Fusion.Photon.Realtime.RegionInfo region in regions)
                {
                    result.Add(new PhotonRegionPing(region.RegionCode, region.RegionPing));
                }

                return PhotonRegionDiscoveryResult.Succeeded(result);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                return PhotonRegionDiscoveryResult.Failed(exception);
            }
        }
    }
}
