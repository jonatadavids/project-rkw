using System.Collections;
using NUnit.Framework;
using RKW.Physics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RKW.Tests.PlayMode
{
    /// <summary>
    /// Real end-to-end coverage for KartDynamics' throttle ramp
    /// (UpdateThrottle, pre-existing) and Etapa 5's (2026-08-31) new brake
    /// ramp (UpdateBrake), replacing the old EditMode ThrottleRampPropertyTest
    /// fake test that only reimplemented Mathf.MoveTowards locally without
    /// ever touching a real KartDynamics component. Both ramps are private,
    /// per-FixedUpdate-tick behavior, so they can only be exercised for real
    /// with a live Rigidbody ticking through WaitForFixedUpdate -- see
    /// ThrottleRampPropertyTest (EditMode) for the shallow, non-ticking
    /// config-sanity checks that stayed there.
    /// </summary>
    public sealed class KartThrottleBrakeRampIntegrationTests
    {
        private static KartDynamics SpawnKart()
        {
            var root = new GameObject("RampTestKart");
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

        [UnityTest]
        public IEnumerator Throttle_SnapToFull_NeverJumpsInstantly_ReachesFullWithinConfiguredRamp()
        {
            var scene = SceneManager.CreateScene("ThrottleRampTest");
            SceneManager.SetActiveScene(scene);
            var kart = SpawnKart();
            var rampSeconds = kart.Tuning.ThrottleRampSeconds;
            yield return null;

            kart.SetInput(0f, 1f, 0f);
            yield return new WaitForFixedUpdate();
            Assert.That(kart.NormalizedThrottle, Is.LessThan(1f),
                "One tick after a full-throttle snap input, throttle should not have jumped straight to 1 -- the ramp must take real time.");

            var ticksToReachFull = 0;
            const int maxTicks = 500;
            while (kart.NormalizedThrottle < 0.999f && ticksToReachFull < maxTicks)
            {
                yield return new WaitForFixedUpdate();
                ticksToReachFull++;
            }

            Assert.That(ticksToReachFull, Is.LessThan(maxTicks), "Throttle never reached full within a generous tick budget.");
            var secondsToReachFull = ticksToReachFull * Time.fixedDeltaTime;
            Assert.That(secondsToReachFull, Is.GreaterThanOrEqualTo(rampSeconds - Time.fixedDeltaTime * 2f),
                $"Throttle reached full in {secondsToReachFull:F3}s, faster than the configured ramp ({rampSeconds:F3}s) allows.");

            yield return SceneManager.UnloadSceneAsync(scene);
        }

        [UnityTest]
        public IEnumerator Brake_SnapToFull_NeverJumpsInstantly_ReachesFullWithinConfiguredApplyRamp()
        {
            var scene = SceneManager.CreateScene("BrakeRampApplyTest");
            SceneManager.SetActiveScene(scene);
            var kart = SpawnKart();
            var applySeconds = kart.Tuning.BrakeApplySeconds;
            yield return null;

            kart.SetInput(0f, 1f, 0f);
            for (var i = 0; i < 100; i++) yield return new WaitForFixedUpdate();

            kart.SetInput(0f, 0f, 1f);
            yield return new WaitForFixedUpdate();
            Assert.That(kart.SmoothedBrakeInput, Is.LessThan(1f),
                "One tick after a full-brake snap input, the smoothed brake should not have jumped straight to 1.");

            var ticksToReachFull = 0;
            const int maxTicks = 200;
            while (kart.SmoothedBrakeInput < 0.999f && ticksToReachFull < maxTicks)
            {
                yield return new WaitForFixedUpdate();
                ticksToReachFull++;
            }

            Assert.That(ticksToReachFull, Is.LessThan(maxTicks), "Smoothed brake never reached full within a generous tick budget.");
            var secondsToReachFull = ticksToReachFull * Time.fixedDeltaTime;
            Assert.That(secondsToReachFull, Is.GreaterThanOrEqualTo(applySeconds - Time.fixedDeltaTime * 2f),
                $"Brake reached full in {secondsToReachFull:F3}s, faster than the configured apply ramp ({applySeconds:F3}s) allows.");

            yield return SceneManager.UnloadSceneAsync(scene);
        }

        [UnityTest]
        public IEnumerator Brake_Release_IsSlowerThanApply_MatchingConfiguredAsymmetry()
        {
            var scene = SceneManager.CreateScene("BrakeRampReleaseTest");
            SceneManager.SetActiveScene(scene);
            var kart = SpawnKart();
            yield return null;

            kart.SetInput(0f, 1f, 0f);
            for (var i = 0; i < 100; i++) yield return new WaitForFixedUpdate();

            kart.SetInput(0f, 0f, 1f);
            var applyTicks = 0;
            while (kart.SmoothedBrakeInput < 0.999f && applyTicks < 200)
            {
                yield return new WaitForFixedUpdate();
                applyTicks++;
            }

            kart.SetInput(0f, 0f, 0f);
            var releaseTicks = 0;
            while (kart.SmoothedBrakeInput > 0.001f && releaseTicks < 200)
            {
                yield return new WaitForFixedUpdate();
                releaseTicks++;
            }

            Assert.That(releaseTicks, Is.GreaterThan(applyTicks),
                $"With BrakeApplySeconds ({kart.Tuning.BrakeApplySeconds}) < BrakeReleaseSeconds ({kart.Tuning.BrakeReleaseSeconds}), " +
                $"releasing the brake should take more ticks than applying it (apply={applyTicks}, release={releaseTicks}).");

            yield return SceneManager.UnloadSceneAsync(scene);
        }
    }
}
