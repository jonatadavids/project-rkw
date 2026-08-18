using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using RKW.Network;

namespace RKW.Network.Tests.EditMode
{
    public sealed class PhotonRegionLatencyStatisticsTests
    {
        [Test]
        public void HeartbeatSummary_CalculatesMetricsFromIndependentChronologicalSamples()
        {
            var summary = PhotonHeartbeatLatencyStatistics.Summarize(
                "sa",
                new List<int> { 30, 10, 20, 40, 50 },
                1);

            Assert.That(summary.ValidSamples, Is.EqualTo(5));
            Assert.That(summary.FailedHeartbeats, Is.EqualTo(1));
            Assert.That(summary.MinimumMilliseconds, Is.EqualTo(10));
            Assert.That(summary.AverageMilliseconds, Is.EqualTo(30d));
            Assert.That(summary.MedianMilliseconds, Is.EqualTo(30d));
            Assert.That(summary.P95Milliseconds, Is.EqualTo(50));
            Assert.That(summary.MaximumMilliseconds, Is.EqualTo(50));
            Assert.That(summary.MeanAbsoluteVariationMilliseconds, Is.EqualTo(15d));
        }

        [Test]
        public void HeartbeatRecommendation_UsesMedianThenP95ThenFailuresWithoutAssumingSa()
        {
            var recommendation = PhotonHeartbeatLatencyStatistics.Recommend(new[]
            {
                PhotonHeartbeatLatencyStatistics.Summarize("sa", new List<int> { 30, 31, 32 }, 0),
                PhotonHeartbeatLatencyStatistics.Summarize("ussc", new List<int> { 29, 31, 33 }, 0),
                PhotonHeartbeatLatencyStatistics.Summarize("us", new List<int> { 30, 31, 31 }, 1)
            });

            Assert.That(recommendation.HasValue, Is.True);
            Assert.That(recommendation.Value.RegionCode, Is.EqualTo("us"));
        }

        [Test]
        public void HeartbeatRecommendation_ReturnsNothingWhenEveryCandidateFailed()
        {
            var recommendation = PhotonHeartbeatLatencyStatistics.Recommend(new[]
            {
                PhotonHeartbeatLatencyStatistics.Summarize("sa", new List<int>(), 30),
                PhotonHeartbeatLatencyStatistics.Summarize("ussc", new List<int>(), 30)
            });

            Assert.That(recommendation.HasValue, Is.False);
        }

        [Test]
        public async Task DiscoverSnapshotAsync_UsesExactlyOneCallForGlobalEmptyResult()
        {
            var calls = 0;
            var probe = new PhotonRegionLatencyProbe(
                _ =>
                {
                    calls++;
                    return Task.FromResult(PhotonRegionDiscoveryResult.NoRegionsReturned());
                },
                new PhotonRegionDiscoveryConfiguration(true, true, false, "Udp"));

            var snapshot = await probe.DiscoverSnapshotAsync(CancellationToken.None);

            Assert.That(calls, Is.EqualTo(1));
            Assert.That(snapshot.Status, Is.EqualTo(PhotonRegionDiscoveryStatus.NoRegionsReturned));
            Assert.That(snapshot.ReturnedRegionCount, Is.EqualTo(0));
            Assert.That(snapshot.RecommendedRegion.HasValue, Is.False);
        }

        [Test]
        public void DiscoverySnapshot_RecommendsLowestValidCandidateWithoutTimeSeriesStatistics()
        {
            var snapshot = new PhotonRegionDiscoverySnapshot(
                System.DateTimeOffset.UtcNow,
                PhotonRegionDiscoveryResult.Succeeded(new[]
                {
                    new PhotonRegionPing("sa", 21),
                    new PhotonRegionPing("ussc", -1),
                    new PhotonRegionPing("us", 172),
                    new PhotonRegionPing("eu", 10)
                }),
                new PhotonRegionDiscoveryConfiguration(true, true, false, "Udp"));

            Assert.That(snapshot.RecommendedRegion.HasValue, Is.True);
            Assert.That(snapshot.RecommendedRegion.Value.RegionCode, Is.EqualTo("sa"));
            Assert.That(snapshot.FindRegion("ussc").Value.IsSuccess, Is.False);
        }
    }
}
