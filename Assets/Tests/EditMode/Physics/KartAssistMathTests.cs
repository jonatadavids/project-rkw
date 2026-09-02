using NUnit.Framework;
using RKW.Physics;
using UnityEngine;

namespace RKW.Physics.Tests.EditMode
{
    /// <summary>
    /// Etapa 8 (2026-08-31) pure-math coverage for the driving assists
    /// layer. Every test here checks the "never faster/more grip than
    /// unassisted" safety property explicitly, not just the headline
    /// behavior -- see KartAssistController (PlayMode:
    /// KartAssistControllerIntegrationTests) for the level-gating wiring.
    /// </summary>
    public sealed class KartAssistMathTests
    {
        [Test]
        public void SteeringAssist_ZeroReduction_IsIdentity()
        {
            Assert.That(KartAssistMath.ApplySteeringAssist(0.7f, 1f, 0f), Is.EqualTo(0.7f).Within(0.0001f));
        }

        [Test]
        public void SteeringAssist_NeverIncreasesMagnitude()
        {
            foreach (var speedRatio in new[] { 0f, 0.3f, 0.7f, 1f })
            {
                var input = 0.6f;
                var assisted = KartAssistMath.ApplySteeringAssist(input, speedRatio, 0.35f);
                Assert.That(Mathf.Abs(assisted), Is.LessThanOrEqualTo(Mathf.Abs(input)),
                    $"speedRatio={speedRatio}: assisted steering must never exceed raw input magnitude.");
            }
        }

        [Test]
        public void SteeringAssist_ReducesMoreAtHigherSpeed()
        {
            var atLowSpeed = KartAssistMath.ApplySteeringAssist(0.6f, 0.1f, 0.35f);
            var atHighSpeed = KartAssistMath.ApplySteeringAssist(0.6f, 1f, 0.35f);
            Assert.That(Mathf.Abs(atHighSpeed), Is.LessThan(Mathf.Abs(atLowSpeed)));
        }

        [Test]
        public void SteeringAssist_AtZeroSpeed_IsUnaffected()
        {
            Assert.That(KartAssistMath.ApplySteeringAssist(0.6f, 0f, 0.35f), Is.EqualTo(0.6f).Within(0.0001f));
        }

        [Test]
        public void ThrottleAssist_BelowEaseStart_IsIdentity()
        {
            Assert.That(KartAssistMath.ApplyThrottleAssist(1f, 0.5f, 0.75f), Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void ThrottleAssist_NeverIncreasesRequest()
        {
            foreach (var usage in new[] { 0f, 0.5f, 0.8f, 1f })
            {
                var assisted = KartAssistMath.ApplyThrottleAssist(0.9f, usage, 0.75f);
                Assert.That(assisted, Is.LessThanOrEqualTo(0.9f),
                    $"usage={usage}: throttle assist must never request MORE throttle than the driver asked for.");
            }
        }

        [Test]
        public void ThrottleAssist_ReducesMoreAsUsageApproachesOne()
        {
            var atStart = KartAssistMath.ApplyThrottleAssist(1f, 0.75f, 0.75f);
            var midway = KartAssistMath.ApplyThrottleAssist(1f, 0.875f, 0.75f);
            var atLimit = KartAssistMath.ApplyThrottleAssist(1f, 1f, 0.75f);

            Assert.That(atStart, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(midway, Is.LessThan(atStart));
            Assert.That(atLimit, Is.LessThan(midway));
            Assert.That(atLimit, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void BrakeAssist_NeverIncreasesRequest_AndNeverFullyRemovesIt()
        {
            foreach (var lockRatio in new[] { 0f, 0.3f, 0.7f, 1f })
            {
                var assisted = KartAssistMath.ApplyBrakeAssist(1f, lockRatio);
                Assert.That(assisted, Is.LessThanOrEqualTo(1f));
                Assert.That(assisted, Is.GreaterThanOrEqualTo(0.5f),
                    $"lockRatio={lockRatio}: brake assist should ease off, not fully cut braking.");
            }
        }

        [Test]
        public void BrakeAssist_ZeroLock_IsIdentity()
        {
            Assert.That(KartAssistMath.ApplyBrakeAssist(0.6f, 0f), Is.EqualTo(0.6f).Within(0.0001f));
        }

        [Test]
        public void StabilityAssistSmoothing_LimitsRateOfChange()
        {
            const float maxRate = 4f;
            const float deltaTime = 0.02f;
            var smoothed = KartAssistMath.ApplyStabilityAssistSmoothing(0f, 1f, maxRate, deltaTime);
            Assert.That(Mathf.Abs(smoothed), Is.LessThanOrEqualTo(maxRate * deltaTime + 0.0001f));
        }

        [Test]
        public void StabilityAssistSmoothing_ConvergesToTargetOverManySteps()
        {
            var smoothed = 0f;
            for (var i = 0; i < 200; i++)
            {
                smoothed = KartAssistMath.ApplyStabilityAssistSmoothing(smoothed, 0.8f, 4f, 0.02f);
            }
            Assert.That(smoothed, Is.EqualTo(0.8f).Within(0.001f));
        }

        [Test]
        public void CounterSteerAssist_BelowThreshold_IsIdentity()
        {
            Assert.That(KartAssistMath.ApplyCounterSteerAssist(0.2f, 10f, 20f, 0.25f), Is.EqualTo(0.2f).Within(0.0001f));
        }

        [Test]
        public void CounterSteerAssist_AboveThreshold_NudgesTowardRearSlipSign()
        {
            // Rear sliding to local +X (positive) with the driver holding
            // neutral steering -- the assist should nudge steering positive
            // (see KartAssistMath's doc comment for the full sign reasoning).
            var assisted = KartAssistMath.ApplyCounterSteerAssist(0f, 30f, 20f, 0.25f);
            Assert.That(assisted, Is.GreaterThan(0f));

            var assistedNegative = KartAssistMath.ApplyCounterSteerAssist(0f, -30f, 20f, 0.25f);
            Assert.That(assistedNegative, Is.LessThan(0f));
        }

        [Test]
        public void CounterSteerAssist_AlreadyCatching_DoesNotAddMore()
        {
            // Driver already steering firmly in the catch direction (same
            // sign as rear slip) -- assist should not add anything more.
            var input = 0.5f;
            var assisted = KartAssistMath.ApplyCounterSteerAssist(input, 30f, 20f, 0.25f);
            Assert.That(assisted, Is.EqualTo(input).Within(0.0001f));
        }

        [Test]
        public void CounterSteerAssist_NeverExceedsClampedRange()
        {
            var assisted = KartAssistMath.ApplyCounterSteerAssist(0.9f, -60f, 20f, 0.9f);
            Assert.That(assisted, Is.InRange(-1f, 1f));
        }

        [Test]
        public void CounterSteerAssist_RampsInGraduallyPastThreshold()
        {
            var justPast = KartAssistMath.ApplyCounterSteerAssist(0f, 21f, 20f, 0.25f);
            var wellPast = KartAssistMath.ApplyCounterSteerAssist(0f, 40f, 20f, 0.25f);
            Assert.That(Mathf.Abs(justPast), Is.LessThan(Mathf.Abs(wellPast)));
        }
    }
}
