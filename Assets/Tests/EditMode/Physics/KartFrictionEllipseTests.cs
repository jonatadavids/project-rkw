using NUnit.Framework;
using RKW.Physics;
using UnityEngine;

namespace RKW.Physics.Tests.EditMode
{
    /// <summary>
    /// Etapa 2 (2026-08-31): tests for the friction-ellipse helpers in
    /// KartDynamicsMath (CalculateEllipseRemainingCapacityRatio,
    /// CalculateCombinedGripUsage). These are the pure functions that
    /// KartDynamics wires into ApplyLongitudinalForces/ApplyLateralForces --
    /// see KartFrictionEllipseIntegrationTests (PlayMode) for the end-to-end
    /// throttle-in-corner / brake-in-corner behavior these enable.
    /// </summary>
    public sealed class KartFrictionEllipseTests
    {
        [Test]
        public void EllipseCapacity_NoUsage_IsFullCapacity()
        {
            Assert.That(KartDynamicsMath.CalculateEllipseRemainingCapacityRatio(0f, 1f), Is.EqualTo(1f).Within(0.0001f));
            Assert.That(KartDynamicsMath.CalculateEllipseRemainingCapacityRatio(0f, 0.5f), Is.EqualTo(1f).Within(0.0001f));
            Assert.That(KartDynamicsMath.CalculateEllipseRemainingCapacityRatio(0f, 2f), Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void EllipseCapacity_FullUsageAtBiasOne_IsZero()
        {
            Assert.That(KartDynamicsMath.CalculateEllipseRemainingCapacityRatio(1f, 1f), Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void EllipseCapacity_MatchesUnitCircleFormula_AtBiasOne()
        {
            // (usage)^2 + (remaining)^2 == 1  ==>  remaining = sqrt(1 - usage^2)
            foreach (var usage in new[] { 0.2f, 0.4f, 0.6f, 0.8f })
            {
                var expected = Mathf.Sqrt(1f - usage * usage);
                var actual = KartDynamicsMath.CalculateEllipseRemainingCapacityRatio(usage, 1f);
                Assert.That(actual, Is.EqualTo(expected).Within(0.001f), $"usage={usage}");
            }
        }

        [Test]
        public void EllipseCapacity_HigherBias_ToleratesMoreUsageBeforeShrinking()
        {
            // Same usage, wider bias -> strictly more remaining capacity.
            const float usage = 0.7f;
            var narrow = KartDynamicsMath.CalculateEllipseRemainingCapacityRatio(usage, 1f);
            var wide = KartDynamicsMath.CalculateEllipseRemainingCapacityRatio(usage, 2f);
            Assert.That(wide, Is.GreaterThan(narrow));
        }

        [Test]
        public void EllipseCapacity_UsageBeyondOne_ClampsRatherThanGoingNegativeOrNaN()
        {
            for (var usage = 1f; usage <= 3f; usage += 0.25f)
            {
                var capacity = KartDynamicsMath.CalculateEllipseRemainingCapacityRatio(usage, 1f);
                Assert.That(capacity, Is.EqualTo(0f).Within(0.0001f));
                Assert.That(float.IsNaN(capacity), Is.False);
            }
        }

        [Test]
        public void EllipseCapacity_NeverNaNOrNegativeAcrossWideSweep()
        {
            for (var usage = -1f; usage <= 3f; usage += 0.1f)
            {
                for (var bias = 0.2f; bias <= 5f; bias += 0.3f)
                {
                    var capacity = KartDynamicsMath.CalculateEllipseRemainingCapacityRatio(usage, bias);
                    Assert.That(float.IsNaN(capacity), Is.False, $"NaN at usage={usage} bias={bias}");
                    Assert.That(float.IsInfinity(capacity), Is.False, $"Infinity at usage={usage} bias={bias}");
                    Assert.That(capacity, Is.GreaterThanOrEqualTo(0f).And.LessThanOrEqualTo(1f));
                }
            }
        }

        [Test]
        public void CombinedGripUsage_ZeroBoth_IsZero()
        {
            Assert.That(KartDynamicsMath.CalculateCombinedGripUsage(0f, 0f), Is.EqualTo(0f));
        }

        [Test]
        public void CombinedGripUsage_MatchesPythagoreanMagnitude_ClampedToOne()
        {
            Assert.That(KartDynamicsMath.CalculateCombinedGripUsage(0.6f, 0.8f), Is.EqualTo(1f).Within(0.001f));
            Assert.That(KartDynamicsMath.CalculateCombinedGripUsage(0.3f, 0.3f),
                Is.EqualTo(Mathf.Sqrt(0.18f)).Within(0.001f));
        }

        [Test]
        public void CombinedGripUsage_NeverExceedsOneEvenWithOverdrivenInputs()
        {
            var usage = KartDynamicsMath.CalculateCombinedGripUsage(5f, 5f);
            Assert.That(usage, Is.EqualTo(1f));
        }
    }
}
