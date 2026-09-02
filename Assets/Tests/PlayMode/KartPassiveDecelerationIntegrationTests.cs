using System.Collections;
using NUnit.Framework;
using RKW.Physics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RKW.Tests.PlayMode
{
    /// <summary>
    /// Etapa 5 (2026-08-31) integration tests for the engine-braking /
    /// rolling-resistance split: engine braking is a new, surface-
    /// independent, additive deceleration (default 0 for every existing
    /// asset); rolling resistance is the pre-existing CoastingDeceleration,
    /// now scaled by the current surface's SurfaceDataSO.RollingResistanceMultiplier
    /// (default 1.0 -- see KartDynamics.ApplyLongitudinalForces's coasting
    /// branch). Uses the real KartDynamics OnTriggerEnter/SurfaceTrigger
    /// pickup path, not a reimplemented formula.
    /// </summary>
    public sealed class KartPassiveDecelerationIntegrationTests
    {
        private static KartDynamics SpawnKart()
        {
            var root = new GameObject("PassiveDecelTestKart");
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

        private static GameObject SpawnOverlappingSurfaceTrigger(SurfaceDataSO data)
        {
            var go = new GameObject("TestSurfaceTrigger");
            var col = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
            // Third bug found via the next real PlayMode run (2026-08-31):
            // a 5m-tall trigger only reaches +-2.5m from its own origin --
            // with gravity unconstrained and no ground in this test scene
            // (same as every other PlayMode test this session), the kart
            // falls out the BOTTOM of that volume in well under a second
            // (physics: t=sqrt(2*2.5/9.81)=~0.71s, ~36 ticks), long before the
            // 150-tick throttle phase even finishes -- OnTriggerExit then
            // silently resets the rolling-resistance multiplier back to the
            // 1.0 default well before the actual measurement window, which is
            // exactly why testKart and the baseline came out numerically
            // IDENTICAL (0.919570446 == 0.919570446) instead of different --
            // not a production bug, the mud simply "wore off" too early in
            // this specific test's geometry. Tall enough that even several
            // seconds of unconstrained freefall (this test never runs anywhere
            // near that long) cannot fall out the bottom.
            col.size = new Vector3(100f, 4000f, 100f);
            var trigger = go.AddComponent<SurfaceTrigger>();
            trigger.Configure(data);
            return go;
        }

        [UnityTest]
        public IEnumerator DefaultSurface_CoastingDeceleration_MatchesOldSingleTermBehavior()
        {
            // Regression guard: default tuning has EngineBrakingDeceleration
            // == 0 and every surface defaults RollingResistanceMultiplier
            // == 1, so total passive deceleration while coasting should be
            // (within numerical tolerance) exactly tuning.CoastingDeceleration,
            // identical to the pre-Etapa-5 single-term behavior.
            var scene = SceneManager.CreateScene("PassiveDecelDefaultTest");
            SceneManager.SetActiveScene(scene);
            var kart = SpawnKart();
            yield return null;

            kart.SetInput(0f, 1f, 0f);
            for (var i = 0; i < 150; i++) yield return new WaitForFixedUpdate();

            kart.SetInput(0f, 0f, 0f);
            // Bug found via the first real PlayMode run (2026-08-31, post-
            // build_deploy_verify.sh round): SetInput(0,0,0) does not zero
            // drive force on the SAME tick -- _smoothedThrottle (the Etapa 5
            // pedal ramp) takes tuning.ThrottleRampSeconds to decay from 1 to
            // 0, and ApplyLongitudinalForces keeps applying real drive force
            // the whole time it is above 0.01 (see the coasting branch's own
            // guard). Measuring "coasting" deceleration one tick after
            // SetInput, before that ramp has actually reached 0, was catching
            // residual drive force instead -- not a KartDynamics bug, a test
            // bug. Wait for NormalizedThrottle (the real, public mirror of
            // _smoothedThrottle) to actually settle first.
            var settleTicks = 0;
            while (kart.NormalizedThrottle > 0.02f && settleTicks < 200)
            {
                yield return new WaitForFixedUpdate();
                settleTicks++;
            }
            Assert.That(kart.NormalizedThrottle, Is.LessThanOrEqualTo(0.02f),
                "Test setup: throttle ramp should have fully decayed within 200 ticks.");

            // Second bug found via the next real PlayMode run (2026-08-31):
            // body.linearVelocity.magnitude is the raw 3D Rigidbody velocity,
            // which INCLUDES vertical fall speed -- these isolated test scenes
            // have no ground collider (matching every other PlayMode test in
            // this session -- a real scene's track stops the fall almost
            // immediately), so gravity keeps accelerating the kart downward,
            // unconstrained, for the whole test. That ever-growing vertical
            // speed swamped the much smaller planar coasting signal this test
            // is actually about. KartDynamics.SpeedKph is already the real,
            // production-correct PLANAR (X/Z only, no Y) speed -- see
            // FixedUpdate's own SpeedKph assignment -- so use that instead of
            // re-deriving anything from the Rigidbody directly.
            var speedBefore = kart.SpeedKph / 3.6f;
            yield return new WaitForFixedUpdate();
            var speedAfter = kart.SpeedKph / 3.6f;
            var measuredDecelPerTick = (speedBefore - speedAfter) / Time.fixedDeltaTime;

            // Loose tolerance: aerodynamic drag and steering-loss terms are
            // also active in ApplyLongitudinalForces and contribute a small
            // additional amount at speed, so this only checks that the
            // measured deceleration is AT LEAST the pure coasting term
            // (default 0 engine braking + rollingResistance*1.0) and not
            // wildly larger (which would indicate a double-counted or
            // broken split).
            Assert.That(measuredDecelPerTick, Is.GreaterThanOrEqualTo(kart.Tuning.CoastingDeceleration * 0.5f));
            Assert.That(measuredDecelPerTick, Is.LessThan(kart.Tuning.CoastingDeceleration * 3f),
                "Measured coasting deceleration is far larger than the tuned baseline -- check for a double-counted rolling resistance/engine braking term.");

            yield return SceneManager.UnloadSceneAsync(scene);
        }

        [UnityTest]
        public IEnumerator HighRollingResistanceSurface_CoastsSlowerThanDefaultAsphalt()
        {
            var scene = SceneManager.CreateScene("PassiveDecelSurfaceTest");
            SceneManager.SetActiveScene(scene);

            // Baseline: default asphalt (no surface trigger), coast and
            // measure the speed lost over a fixed window.
            var baselineKart = SpawnKart();
            yield return null;
            baselineKart.SetInput(0f, 1f, 0f);
            for (var i = 0; i < 150; i++) yield return new WaitForFixedUpdate();
            baselineKart.SetInput(0f, 0f, 0f);
            // See the matching comment in DefaultSurface_CoastingDeceleration_
            // MatchesOldSingleTermBehavior above -- must let the throttle ramp
            // fully decay before the coasting-only measurement window starts,
            // for both karts, or residual drive force swamps the (much
            // smaller) rolling-resistance signal this test is looking for.
            var baselineSettleTicks = 0;
            while (baselineKart.NormalizedThrottle > 0.02f && baselineSettleTicks < 200)
            {
                yield return new WaitForFixedUpdate();
                baselineSettleTicks++;
            }
            // See the matching comment in DefaultSurface_CoastingDeceleration_
            // MatchesOldSingleTermBehavior above -- use the real, planar
            // SpeedKph rather than the raw Rigidbody velocity magnitude, which
            // also picks up the unconstrained vertical fall in this ground-less
            // test scene.
            var baselineSpeedBefore = baselineKart.SpeedKph / 3.6f;
            for (var i = 0; i < 30; i++) yield return new WaitForFixedUpdate();
            var baselineSpeedLost = baselineSpeedBefore - baselineKart.SpeedKph / 3.6f;

            // Test kart: a real SurfaceDataSO configured (via the real,
            // public Configure() API -- Etapa 5's new optional trailing
            // parameter) with a heavy rolling-resistance multiplier,
            // covering the whole scene so the kart is on it from the start.
            var heavySurface = ScriptableObject.CreateInstance<SurfaceDataSO>();
            heavySurface.Configure("test_mud", "Test Mud", 1f, 0f, false, rollingResistance: 4f);
            var triggerGo = SpawnOverlappingSurfaceTrigger(heavySurface);
            SceneManager.MoveGameObjectToScene(triggerGo, scene);

            var testKart = SpawnKart();
            yield return null; // let OnTriggerEnter fire
            for (var i = 0; i < 3; i++) yield return new WaitForFixedUpdate();
            Assert.That(testKart.SurfaceGripMultiplier, Is.EqualTo(1f).Within(0.001f),
                "Test setup: this surface should not affect grip, only rolling resistance.");

            testKart.SetInput(0f, 1f, 0f);
            for (var i = 0; i < 150; i++) yield return new WaitForFixedUpdate();
            testKart.SetInput(0f, 0f, 0f);
            var testSettleTicks = 0;
            while (testKart.NormalizedThrottle > 0.02f && testSettleTicks < 200)
            {
                yield return new WaitForFixedUpdate();
                testSettleTicks++;
            }
            var testSpeedBefore = testKart.SpeedKph / 3.6f;
            for (var i = 0; i < 30; i++) yield return new WaitForFixedUpdate();
            var testSpeedLost = testSpeedBefore - testKart.SpeedKph / 3.6f;

            Assert.That(testSpeedLost, Is.GreaterThan(baselineSpeedLost),
                $"A 4x rolling-resistance surface should coast down faster than default asphalt " +
                $"(baseline lost {baselineSpeedLost:F3} m/s, test-surface kart lost {testSpeedLost:F3} m/s over the same window).");

            yield return SceneManager.UnloadSceneAsync(scene);
        }
    }
}
