using System;
using System.Collections;
using NUnit.Framework;
using RKW.Network;
using UnityEngine;
using UnityEngine.TestTools;

namespace RKW.Tests.PlayMode
{
    public sealed class PhotonRegionLatencyIntegrationTests
    {
        private const float DiscoveryTimeoutSeconds = 60f;

        [UnityTest]
        public IEnumerator BrasiliaDevelopmentNetwork_CapturesRegionalDiscoverySnapshot()
        {
            if (!ShouldRunPhotonLatencyMeasurement())
            {
                Assert.Ignore(
                    "Set RKW_RUN_PHOTON_LATENCY=1 to run the local Photon region snapshot.");
            }

            var probe = new PhotonRegionLatencyProbe();
            var snapshotTask = probe.DiscoverSnapshotAsync();
            var timeoutAt = Time.realtimeSinceStartup + DiscoveryTimeoutSeconds;
            while (!snapshotTask.IsCompleted && Time.realtimeSinceStartup < timeoutAt)
            {
                yield return null;
            }

            Assert.That(snapshotTask.IsCompleted, Is.True, "Photon region discovery timed out.");
            Assert.That(snapshotTask.IsFaulted, Is.False,
                snapshotTask.Exception?.GetBaseException().GetType().Name);
            Assert.That(snapshotTask.IsCanceled, Is.False);
            var snapshot = snapshotTask.Result;
            if (snapshot.Status != PhotonRegionDiscoveryStatus.Succeeded)
            {
                Assert.Inconclusive(snapshot.ToSanitizedLog());
            }

            Assert.That(snapshot.ReturnedRegionCount, Is.GreaterThan(0));
            Assert.That(snapshot.RecommendedRegion.HasValue, Is.True,
                "Discovery succeeded but no valid approved candidate region was returned.");
            Debug.Log(snapshot.ToSanitizedLog());
        }

        private static bool ShouldRunPhotonLatencyMeasurement()
        {
            return string.Equals(
                Environment.GetEnvironmentVariable("RKW_RUN_PHOTON_LATENCY"),
                "1",
                StringComparison.Ordinal);
        }
    }
}
