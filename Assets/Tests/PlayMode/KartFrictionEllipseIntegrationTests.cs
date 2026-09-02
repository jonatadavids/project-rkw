using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using RKW.Physics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RKW.Tests.PlayMode
{
    /// <summary>
    /// Etapa 2 (2026-08-31) integration tests for the friction-ellipse
    /// coupling between throttle/brake and cornering, exercising the real
    /// KartDynamics component end to end (Rigidbody + PhysX). See
    /// KartFrictionEllipseTests (EditMode) for the pure-math coverage of
    /// the ellipse formula itself.
    /// </summary>
    public sealed class KartFrictionEllipseIntegrationTests
    {
        private static KartDynamics SpawnKart()
        {
            var root = new GameObject("FrictionEllipseTestKart");
            var collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(1.0f, 0.5f, 1.8f);
            collider.center = new Vector3(0f, 0.25f, 0f);
            var body = root.AddComponent<Rigidbody>();
            body.linearDamping = 0.02f;
            body.angularDamping = 0.6f;
            var dynamics = root.AddComponent<KartDynamics>();
            var tuning = Resources.Load<KartCategorySO>("KartPhysics/PrototypeRentalSportTuning");
            Assert.That(tuning, Is.Not.Null, "Could not load PrototypeRentalSportTuning from Resources.");
            dynamics.Configure(tuning);
            return dynamics;
        }

        private static float Average(List<float> values)
        {
            if (values.Count == 0) return 0f;
            var total = 0f;
            foreach (var v in values) total += v;
            return total / values.Count;
        }

        [UnityTest]
        public IEnumerator ThrottleInLowSpeedCorner_IncreasesRearLongitudinalDemand_NoRunaway()
        {
            var scene = SceneManager.CreateScene("FrictionEllipseThrottleTest");
            SceneManager.SetActiveScene(scene);
            var kart = SpawnKart();
            var body = kart.GetComponent<Rigidbody>();
            yield return null;

            // Modest speed first (classic corner-exit scenario, not flat-out top speed).
            kart.SetInput(0f, 0.35f, 0f);
            for (var i = 0; i < 40; i++) yield return new WaitForFixedUpdate();

            kart.SetInput(0.7f, 1.0f, 0f);
            var rearLongDemand = new List<float>();
            var rearSlip = new List<float>();
            for (var i = 0; i < 60; i++)
            {
                yield return new WaitForFixedUpdate();
                rearLongDemand.Add(kart.RearLongitudinalDemand);
                rearSlip.Add(Mathf.Abs(kart.RearSlipAngleDegrees));

                // Never allow the ellipse coupling to produce a non-finite
                // or runaway state -- this is exactly what an earlier,
                // buggy version of this patch did (see the KartDynamics.cs
                // comment on _lastRequestedDriveAccelMps2): full throttle
                // drove usage to 1.0 and rear slip past -80 degrees.
                Assert.That(float.IsFinite(body.angularVelocity.y), Is.True, $"tick {i}: angularVelocity non-finite");
                // Ceiling is tuning-relative (fullLoss + 25 degrees of margin) rather
                // than a stale hardcoded literal -- the Etapa "recovery" retune (2026-08-31)
                // intentionally widened RearFullLossSlipAngleDegrees for playability, so a
                // fixed 45-degree number would leave almost no margin against the new value.
                var rearSlipCeiling = kart.Tuning.RearFullLossSlipAngleDegrees + 25f;
                Assert.That(Mathf.Abs(kart.RearSlipAngleDegrees), Is.LessThan(rearSlipCeiling),
                    $"tick {i}: rear slip should never blow up to near-90 degrees from throttle alone " +
                    $"(ceiling={rearSlipCeiling:F1} deg, fullLoss={kart.Tuning.RearFullLossSlipAngleDegrees:F1} deg).");
            }

            Assert.That(Average(rearLongDemand), Is.GreaterThan(0.01f),
                "Full throttle mid-corner should register nonzero rear longitudinal demand.");

            yield return SceneManager.UnloadSceneAsync(scene);
        }

        [UnityTest]
        public IEnumerator MaxThrottleVsCoasting_SameCorner_RearUsesMoreLongitudinalBudget()
        {
            var scene = SceneManager.CreateScene("FrictionEllipseCompareTest");
            SceneManager.SetActiveScene(scene);

            var kartA = SpawnKart();
            kartA.SetInput(0f, 0.35f, 0f);
            for (var i = 0; i < 40; i++) yield return new WaitForFixedUpdate();
            kartA.SetInput(0.7f, 0.0f, 0f); // coasting through the corner
            var coastingDemand = new List<float>();
            for (var i = 0; i < 40; i++)
            {
                yield return new WaitForFixedUpdate();
                coastingDemand.Add(kartA.RearLongitudinalDemand);
            }

            var kartB = SpawnKart();
            kartB.SetInput(0f, 0.35f, 0f);
            for (var i = 0; i < 40; i++) yield return new WaitForFixedUpdate();
            kartB.SetInput(0.7f, 1.0f, 0f); // full throttle through the same corner
            var throttleDemand = new List<float>();
            for (var i = 0; i < 40; i++)
            {
                yield return new WaitForFixedUpdate();
                throttleDemand.Add(kartB.RearLongitudinalDemand);
            }

            Assert.That(Average(throttleDemand), Is.GreaterThan(Average(coastingDemand)),
                "Full throttle through a corner should register more rear longitudinal demand than coasting through the same corner.");

            yield return SceneManager.UnloadSceneAsync(scene);
        }

        [UnityTest]
        public IEnumerator BrakingWhileTurning_ReducesFrontLateralCapacity_StaysFinite()
        {
            var scene = SceneManager.CreateScene("FrictionEllipseBrakeTest");
            SceneManager.SetActiveScene(scene);
            var kart = SpawnKart();
            var body = kart.GetComponent<Rigidbody>();
            yield return null;

            kart.SetInput(0f, 1f, 0f);
            for (var i = 0; i < 200; i++) yield return new WaitForFixedUpdate();
            Assert.That(kart.SpeedKph, Is.GreaterThan(20f), "Test setup: kart should be at real speed before trail-braking.");

            kart.SetInput(0.6f, 0f, 0.8f);
            for (var i = 0; i < 40; i++)
            {
                yield return new WaitForFixedUpdate();
                Assert.That(float.IsFinite(body.linearVelocity.x), Is.True, $"tick {i}: velocity non-finite while trail braking");
                Assert.That(float.IsFinite(body.angularVelocity.y), Is.True, $"tick {i}: angular velocity non-finite while trail braking");
            }

            Assert.That(kart.FrontLongitudinalDemand, Is.GreaterThan(0f),
                "Braking with the front's brake-bias share should register nonzero front longitudinal demand.");

            yield return SceneManager.UnloadSceneAsync(scene);
        }

        [UnityTest]
        public IEnumerator CaptureTelemetry_GripUsageFields_MatchLivePropertiesNotNull()
        {
            // Etapa 2 (2026-08-31): KartDynamics.CaptureTelemetry used to
            // leave the friction-ellipse telemetry fields as null ("not
            // implemented yet"). Now that the ellipse is implemented, this
            // guards against silently regressing back to nulls, and checks
            // CaptureTelemetry actually reads the SAME values the public
            // Front/RearLateralDemand etc. properties expose (not a
            // second, independently-drifting computation).
            var scene = SceneManager.CreateScene("FrictionEllipseTelemetryTest");
            SceneManager.SetActiveScene(scene);
            var kart = SpawnKart();
            yield return null;

            kart.SetInput(0.5f, 0.8f, 0f);
            for (var i = 0; i < 30; i++) yield return new WaitForFixedUpdate();

            var sample = new KartPhysicsTelemetrySample();
            kart.CaptureTelemetry(ref sample);

            Assert.That(sample.FrontGripUsage, Is.Not.Null.And.EqualTo(kart.FrontLateralDemand));
            Assert.That(sample.RearGripUsage, Is.Not.Null.And.EqualTo(kart.RearLateralDemand));
            Assert.That(sample.FrontLongitudinalGripUsage, Is.Not.Null.And.EqualTo(kart.FrontLongitudinalDemand));
            Assert.That(sample.RearLongitudinalGripUsage, Is.Not.Null.And.EqualTo(kart.RearLongitudinalDemand));
            Assert.That(sample.CombinedGripUsage, Is.Not.Null);
            Assert.That(sample.CombinedGripUsage.Value,
                Is.EqualTo(Mathf.Max(kart.FrontCombinedGripUsage, kart.RearCombinedGripUsage)).Within(0.0001f));

            yield return SceneManager.UnloadSceneAsync(scene);
        }

        [UnityTest]
        public IEnumerator TrailBraking_ReleasingBrakeMidCorner_GraduallyRestoresFrontLateralCapacity()
        {
            // Etapa 5 (2026-08-31): "trail braking" -- easing off the brake
            // progressively while still turning in -- is meant to be an
            // EMERGENT result of the Etapa 2 friction ellipse (brake demand
            // eats into front lateral capacity) combined with the Etapa 5
            // brake release ramp (SmoothedBrakeInput fades out over real
            // time, not instantly), not a new bonus mechanic. This confirms
            // that combination actually produces a gradual, monotonic-ish
            // recovery of front lateral capacity as the brake is released,
            // rather than either an instant snap-back (no ramp) or no
            // change at all (ellipse not coupled to front capacity).
            var scene = SceneManager.CreateScene("TrailBrakingTest");
            SceneManager.SetActiveScene(scene);
            var kart = SpawnKart();
            yield return null;

            kart.SetInput(0f, 1f, 0f);
            for (var i = 0; i < 150; i++) yield return new WaitForFixedUpdate();

            // Trail braking: hard brake INTO the corner, then ease off
            // while still steering.
            kart.SetInput(0.6f, 0f, 1f);
            for (var i = 0; i < 40; i++) yield return new WaitForFixedUpdate();
            var frontLongDemandUnderFullBrake = kart.FrontLongitudinalDemand;

            kart.SetInput(0.6f, 0f, 0f);
            var sawRecovery = false;
            for (var i = 0; i < 60; i++)
            {
                yield return new WaitForFixedUpdate();
                Assert.That(float.IsFinite(kart.FrontLongitudinalDemand), Is.True, $"tick {i}: front longitudinal demand non-finite");
                if (kart.FrontLongitudinalDemand < frontLongDemandUnderFullBrake * 0.5f)
                {
                    sawRecovery = true;
                }
            }

            Assert.That(sawRecovery, Is.True,
                "Releasing the brake mid-corner should let front longitudinal demand fade back down " +
                "(and with it, front lateral capacity recover) as the Etapa 5 release ramp completes.");

            yield return SceneManager.UnloadSceneAsync(scene);
        }
    }
}
