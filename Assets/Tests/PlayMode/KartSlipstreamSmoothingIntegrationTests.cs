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
    /// Etapa 12 (2026-08-31) integration tests for the slipstream drag
    /// reduction SMOOTHING added in KartDynamics.UpdateSlipstream. The pure
    /// KartDynamicsMath.CalculateSlipstreamDragReduction formula itself is
    /// unchanged and already covered by SlipstreamPropertyTest (EditMode) --
    /// these tests instead exercise the real per-tick MonoBehaviour wiring
    /// to prove two previously-instant transitions are now a smooth ramp:
    /// (1) the minimumTimeInSlipstream gate "snap-in" the instant the time
    /// threshold is crossed, and (2) the forward-cone "snap-out" the
    /// instant a leader kart exits the cone (e.g. a lateral overtake).
    /// </summary>
    public sealed class KartSlipstreamSmoothingIntegrationTests
    {
        private static KartDynamics SpawnKart(string name, Vector3 position)
        {
            var root = new GameObject(name);
            root.transform.position = position;
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

        /// <summary>Same tuning field values every SpawnKart'd kart above uses -- read once for the max-per-tick-step math below.</summary>
        private static KartCategorySO LoadTuning()
        {
            return Resources.Load<KartCategorySO>("KartPhysics/PrototypeRentalSportTuning");
        }

        [UnityTest]
        public IEnumerator ActivationGate_DoesNotSnapToTarget_RampsInOverTransitionSeconds()
        {
            var scene = SceneManager.CreateScene("SlipstreamGateSmoothingTest");
            SceneManager.SetActiveScene(scene);

            var tuning = LoadTuning();
            // 2.5m ahead: inside [kartLengthMeters, maxActivationLengths*kartLengthMeters]
            // = [1.8, 3.24] for PrototypeRentalSportTuning, and dead ahead so it is
            // always inside the forward cone.
            var follower = SpawnKart("SlipstreamFollower", Vector3.zero);
            var leader = SpawnKart("SlipstreamLeader", new Vector3(0f, 0f, 2.5f));
            yield return null;

            var maxStepPerTick = Time.fixedDeltaTime * (tuning.SlipstreamMaxReduction / tuning.SlipstreamTransitionSeconds);

            var previous = follower.SlipstreamDragReduction;
            Assert.That(previous, Is.EqualTo(0f), "Should start at 0 before any time has accumulated in the slipstream.");

            var sawNonZero = false;
            var sawGateCrossing = false;
            const int totalTicks = 120;
            for (var i = 0; i < totalTicks; i++)
            {
                yield return new WaitForFixedUpdate();
                var current = follower.SlipstreamDragReduction;

                Assert.That(float.IsFinite(current), Is.True, $"tick {i}: non-finite drag reduction");
                Assert.That(current, Is.GreaterThanOrEqualTo(0f), $"tick {i}: drag reduction went negative");

                // Core smoothing guarantee: whatever the underlying target does
                // (including jumping instantly the moment the minimumTime gate
                // is crossed), the SMOOTHED value the rest of the game reads
                // can only move by maxStepPerTick in either direction per tick.
                Assert.That(Mathf.Abs(current - previous), Is.LessThanOrEqualTo(maxStepPerTick + 0.0001f),
                    $"tick {i}: drag reduction moved by {Mathf.Abs(current - previous):F5} in one tick, " +
                    $"more than the allowed ramp step {maxStepPerTick:F5} -- this is the snap-in bug the fix targets.");

                if (previous == 0f && current > 0f)
                {
                    sawGateCrossing = true;
                }
                if (current > 0f)
                {
                    sawNonZero = true;
                }

                previous = current;
            }

            Assert.That(sawGateCrossing, Is.True,
                "Test setup: expected the minimumTimeInSlipstream gate to be crossed at least once within 120 ticks.");
            Assert.That(sawNonZero, Is.True);

            // After the gate is crossed, given enough ticks the smoothed value
            // should have caught up close to the real steady-state target
            // (computed via the actual production formula, not reimplemented).
            var steadyTarget = KartDynamicsMath.CalculateSlipstreamDragReduction(
                2.5f, tuning.KartLengthMeters, tuning.SlipstreamMaxActivationLengths,
                tuning.SlipstreamMaxReduction, tuning.SlipstreamMinimumTimeSeconds,
                timeInSlipstream: 10f);
            Assert.That(follower.SlipstreamDragReduction, Is.EqualTo(steadyTarget).Within(0.01f),
                "After the full transition window has elapsed, the smoothed value should have caught up to the steady-state target.");

            yield return SceneManager.UnloadSceneAsync(scene);
        }

        [UnityTest]
        public IEnumerator ForwardConeExit_DoesNotSnapToZero_RampsOutOverTransitionSeconds()
        {
            var scene = SceneManager.CreateScene("SlipstreamExitSmoothingTest");
            SceneManager.SetActiveScene(scene);

            var tuning = LoadTuning();
            var follower = SpawnKart("SlipstreamFollower2", Vector3.zero);
            // 1.9m (closer than the gate-test's 2.5m) so the steady-state
            // reduction is comfortably larger than a couple of maxStepPerTick
            // increments -- otherwise a *correctly* ramping-down value can
            // finish within just a few ticks simply because there was not
            // much distance to cover, which would look identical to a snap
            // and make the "not immediately zero" check below flaky.
            var leader = SpawnKart("SlipstreamLeader2", new Vector3(0f, 0f, 1.9f));
            yield return null;

            // Run long enough to reach a steady non-trivial drafting value
            // before triggering the exit.
            for (var i = 0; i < 60; i++)
            {
                yield return new WaitForFixedUpdate();
            }
            var steadyValue = follower.SlipstreamDragReduction;
            Assert.That(steadyValue, Is.GreaterThan(0.05f),
                "Test setup: follower should be meaningfully drafting before the exit is triggered.");

            // Move the leader out to the side -- same rough distance, but now
            // Dot(toOther.normalized, myForward) drops well below the
            // SlipstreamForwardConeCosine threshold, so FindLeaderDistanceMeters
            // stops finding it at all (this is the exact "lateral overtake"
            // scenario the fix targets: leaderDistance goes from a value straight
            // to null, so the TARGET drops to 0 instantly).
            leader.transform.position = new Vector3(10f, 0f, 0f);

            var maxStepPerTick = Time.fixedDeltaTime * (tuning.SlipstreamMaxReduction / tuning.SlipstreamTransitionSeconds);
            var previous = follower.SlipstreamDragReduction;
            Assert.That(previous, Is.EqualTo(steadyValue).Within(0.0001f));

            // Bug found via the first real PlayMode run (2026-08-31, post-
            // build_deploy_verify.sh round): checking "no tick in a fixed
            // 5-tick window is exactly 0" is too strict in general -- a
            // small enough steadyValue can legitimately finish ramping down
            // to 0 within a handful of ticks (that is still a correct,
            // bounded-rate ramp, not a snap). The actual claim this test
            // makes is specifically about the FIRST tick right after the
            // exit -- check exactly that, plus the per-tick rate bound
            // (already enforced above) across a window sized from the real
            // steadyValue/maxStepPerTick ratio so it never overruns into the
            // tick where a legitimate ramp finishes at 0.
            yield return new WaitForFixedUpdate();
            var firstTickAfterExit = follower.SlipstreamDragReduction;
            Assert.That(Mathf.Abs(firstTickAfterExit - previous), Is.LessThanOrEqualTo(maxStepPerTick + 0.0001f),
                $"tick 0 after exit: dropped by {Mathf.Abs(firstTickAfterExit - previous):F5} in one tick, " +
                $"more than the allowed ramp step {maxStepPerTick:F5} -- this is the snap-out bug the fix targets.");
            Assert.That(firstTickAfterExit, Is.Not.EqualTo(0f),
                "Drag reduction must not drop straight to 0 on the tick right after a lateral cone exit.");
            previous = firstTickAfterExit;

            var safeWindowTicks = Mathf.Max(0, Mathf.FloorToInt(steadyValue / maxStepPerTick) - 2);
            for (var i = 0; i < safeWindowTicks; i++)
            {
                yield return new WaitForFixedUpdate();
                var current = follower.SlipstreamDragReduction;
                Assert.That(Mathf.Abs(current - previous), Is.LessThanOrEqualTo(maxStepPerTick + 0.0001f),
                    $"tick {i + 1} after exit: dropped by {Mathf.Abs(current - previous):F5} in one tick, " +
                    $"more than the allowed ramp step {maxStepPerTick:F5} -- this is the snap-out bug the fix targets.");
                Assert.That(current, Is.LessThanOrEqualTo(previous + 0.0001f),
                    $"tick {i + 1} after exit: drag reduction increased while ramping down -- should be monotonically decreasing.");
                previous = current;
            }

            // Eventually (steadyValue / maxStepPerTick ticks, plus margin) it
            // should reach (or get very close to) 0.
            var ticksNeeded = Mathf.CeilToInt(steadyValue / maxStepPerTick) + 5;
            for (var i = 0; i < ticksNeeded; i++)
            {
                yield return new WaitForFixedUpdate();
            }
            Assert.That(follower.SlipstreamDragReduction, Is.EqualTo(0f).Within(0.005f),
                "After enough ticks past the exit, the smoothed value should have ramped all the way down to 0.");

            yield return SceneManager.UnloadSceneAsync(scene);
        }
    }
}
