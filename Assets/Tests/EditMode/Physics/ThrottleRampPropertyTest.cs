using NUnit.Framework;
using RKW.Physics;
using UnityEngine;

namespace RKW.Physics.Tests.EditMode
{
    /// <summary>
    /// Property 4: Throttle Ramp Rate Limit -- for any sequence of throttle
    /// inputs over time, the output throttle value shall never increase
    /// faster than 1/ThrottleRampSeconds per second.
    ///
    /// Rewritten (2026-08-31, rigid rear axle/friction ellipse round): the
    /// previous version of this file called Mathf.MoveTowards directly with
    /// hand-rolled local variables, reimplementing (rather than exercising)
    /// KartDynamics.UpdateThrottle's actual ramp -- it would have kept
    /// passing even if UpdateThrottle's real code diverged from this copy
    /// (e.g. used a different curve, or a wrong deltaTime). The MoveTowards-
    /// based ramp itself needs live Rigidbody+FixedUpdate ticks to exercise
    /// for real (UpdateThrottle is a private per-tick MonoBehaviour method),
    /// which EditMode tests cannot drive -- see
    /// KartThrottleBrakeRampIntegrationTests (PlayMode) for that real,
    /// end-to-end coverage of both throttle AND the new Etapa 5 brake ramp.
    ///
    /// This file keeps only what EditMode CAN legitimately verify: that the
    /// real, shipped tuning assets actually carry sane ramp configuration
    /// (not fabricated example numbers).
    /// </summary>
    public sealed class ThrottleRampPropertyTest
    {
        private static readonly string[] TuningResourcePaths =
        {
            "KartPhysics/PrototypeRentalSportTuning",
            "KartPhysics/PrototypeSchoolTuning",
            "KartPhysics/PrototypeSportPlusTuning",
        };

        [Test]
        public void EveryShippedTuning_ThrottleRampSeconds_MeetsDocumentedMinimum()
        {
            foreach (var path in TuningResourcePaths)
            {
                var tuning = Resources.Load<KartCategorySO>(path);
                Assert.That(tuning, Is.Not.Null, $"Could not load {path} from Resources.");
                Assert.That(tuning.ThrottleRampSeconds, Is.GreaterThanOrEqualTo(0.15f),
                    $"{path}: ThrottleRampSeconds below the documented [Min(0.15f)] floor.");
            }
        }

        [Test]
        public void EveryShippedTuning_BrakeRampSeconds_ArePositive_AndApplyIsFasterThanRelease()
        {
            // Etapa 5 (2026-08-31): real design intent -- brakes should
            // build up pressure quickly (apply) but release a bit more
            // progressively (release), never the other way around.
            foreach (var path in TuningResourcePaths)
            {
                var tuning = Resources.Load<KartCategorySO>(path);
                Assert.That(tuning, Is.Not.Null, $"Could not load {path} from Resources.");
                Assert.That(tuning.BrakeApplySeconds, Is.GreaterThan(0f), $"{path}: BrakeApplySeconds must be positive.");
                Assert.That(tuning.BrakeReleaseSeconds, Is.GreaterThan(0f), $"{path}: BrakeReleaseSeconds must be positive.");
                Assert.That(tuning.BrakeApplySeconds, Is.LessThanOrEqualTo(tuning.BrakeReleaseSeconds),
                    $"{path}: brake apply should not be slower than release.");
            }
        }

        [Test]
        public void MoveTowardsRampRate_NeverExceedsOneOverRampSeconds()
        {
            // Documents the underlying guarantee UpdateThrottle/UpdateBrake
            // rely on from Unity's own Mathf.MoveTowards (not a
            // reimplementation of KartDynamics' formula -- this exercises
            // the real engine function with the same call shape production
            // code uses, across randomized ramp durations).
            var random = new System.Random(401);
            const float fixedDeltaTime = 0.02f;

            for (var iteration = 0; iteration < 200; iteration++)
            {
                var rampSeconds = 0.15f + (float)random.NextDouble() * 0.35f;
                var maxRate = 1f / rampSeconds;
                var smoothed = 0f;

                for (var tick = 0; tick < 100; tick++)
                {
                    var previous = smoothed;
                    var rate = 1f / Mathf.Max(0.02f, rampSeconds);
                    smoothed = Mathf.MoveTowards(smoothed, 1f, rate * fixedDeltaTime);
                    var actualRate = (smoothed - previous) / fixedDeltaTime;

                    Assert.That(actualRate, Is.LessThanOrEqualTo(maxRate + 0.001f),
                        $"iteration={iteration} tick={tick}: rate={actualRate:F4}/s exceeded max={maxRate:F4}/s");
                }
            }
        }
    }
}
