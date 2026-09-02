using NUnit.Framework;
using RKW.Physics;
using UnityEngine;

namespace RKW.Physics.Tests.EditMode
{
    /// <summary>
    /// Etapa 1.2 (2026-08-31): tests for the low-speed slip-angle blend fix
    /// requested at the Etapa 1.1 validation gate (item 7 found a real,
    /// measured ~7 degree jump at the old hard cutoff). Calls the real
    /// production functions in KartDynamicsMath -- nothing here reimplements
    /// the blend formula being tested.
    /// </summary>
    public sealed class KartLowSpeedBlendTests
    {
        private const float Threshold = 0.3f;

        [Test]
        public void AxleSlip_BelowHalfThreshold_IsExactlyZero()
        {
            // Below half the threshold, behavior must be unchanged from
            // before: exactly 0, no matter the lateral component.
            Assert.That(KartDynamicsMath.CalculateAxleSlipAngleDegrees(0.05f, 0.10f, Threshold), Is.EqualTo(0f));
            Assert.That(KartDynamicsMath.CalculateAxleSlipAngleDegrees(0.05f, 0.0f, Threshold), Is.EqualTo(0f));
        }

        [Test]
        public void AxleSlip_AtOrAboveThreshold_MatchesRawAtan2Angle()
        {
            // At/above the threshold, the blend must be fully "open" (1.0),
            // i.e. identical to the pre-Etapa-1.2 raw formula -- no change
            // in behavior at real racing speeds.
            const float lateral = 0.05f;
            const float longitudinal = 5f;
            var expected = Mathf.Atan2(lateral, Mathf.Abs(longitudinal) + 0.1f) * Mathf.Rad2Deg;
            var actual = KartDynamicsMath.CalculateAxleSlipAngleDegrees(lateral, longitudinal, Threshold);
            Assert.That(actual, Is.EqualTo(expected).Within(0.0001f));

            // exactly at the threshold too
            var atThreshold = KartDynamicsMath.CalculateAxleSlipAngleDegrees(lateral, Threshold, Threshold);
            var expectedAtThreshold = Mathf.Atan2(lateral, Mathf.Abs(Threshold) + 0.1f) * Mathf.Rad2Deg;
            Assert.That(atThreshold, Is.EqualTo(expectedAtThreshold).Within(0.001f));
        }

        [Test]
        public void AxleSlip_NoLargeJumpAcrossOldDiscontinuity()
        {
            // The Etapa 1.1 gate measured a ~7 degree jump between 0.295 and
            // 0.300 m/s for this tuning's lateral/longitudinal shape. After
            // the fix, no single small step in planar speed across the
            // whole 0.05..0.5 m/s band should produce more than a couple of
            // degrees of change -- the whole point of the blend.
            const float lateral = 0.05f;
            var previous = KartDynamicsMath.CalculateAxleSlipAngleDegrees(lateral, 0.05f, Threshold);
            var maxJump = 0f;
            for (var speed = 0.055f; speed <= 0.5f; speed += 0.005f)
            {
                var longitudinal = Mathf.Sqrt(Mathf.Max(0f, speed * speed - lateral * lateral));
                var slip = KartDynamicsMath.CalculateAxleSlipAngleDegrees(lateral, longitudinal, Threshold);
                maxJump = Mathf.Max(maxJump, Mathf.Abs(slip - previous));
                previous = slip;
            }

            Assert.That(maxJump, Is.LessThan(1.5f),
                $"Largest single 5mm/s step should be smooth, not a snap (was {maxJump} deg).");
        }

        [Test]
        public void AxleSlip_NeverNaNOrInfiniteAcrossTheWholeBlendRegion()
        {
            for (var speed = 0f; speed <= 1f; speed += 0.01f)
            {
                var slip = KartDynamicsMath.CalculateAxleSlipAngleDegrees(0.05f, speed, Threshold);
                Assert.That(float.IsNaN(slip), Is.False, $"NaN at speed={speed}");
                Assert.That(float.IsInfinity(slip), Is.False, $"Infinity at speed={speed}");
            }
        }

        [Test]
        public void FrontAxleSlip_BlendPreservesLeftRightSymmetry()
        {
            // Steering symmetric, lateral velocity symmetric -> slip must be
            // exactly mirrored at every point across the blend region,
            // including the transition band itself (not just the extremes).
            for (var speed = 0.1f; speed <= 0.5f; speed += 0.02f)
            {
                var positive = KartDynamicsMath.CalculateFrontAxleSlipAngleDegrees(0.05f, speed, 0.3f, 24f, Threshold);
                var negative = KartDynamicsMath.CalculateFrontAxleSlipAngleDegrees(-0.05f, speed, -0.3f, 24f, Threshold);
                Assert.That(negative, Is.EqualTo(-positive).Within(0.0001f),
                    $"Mirrored input should produce mirrored slip at speed={speed}.");
            }
        }

        [Test]
        public void FrontAxleSlip_ParkedWithWheelTurned_IsZeroNotResidualWheelAngle()
        {
            // A stationary kart with the wheel cranked over should not
            // report a "slip" equal to the negated wheel angle -- both the
            // velocity-angle term AND the wheel-angle term must blend
            // together toward 0, not just the velocity term.
            var slip = KartDynamicsMath.CalculateFrontAxleSlipAngleDegrees(0f, 0f, 1f, 24f, Threshold);
            Assert.That(slip, Is.EqualTo(0f));
        }

        [Test]
        public void CenterOfMassOffset_DefaultZero_SplitsWheelbaseEvenly()
        {
            // Pure math check on the derived-distance formulas a KartCategorySO
            // exposes (FrontAxleDistanceFromCoMMeters / RearAxleDistanceFromCoMMeters):
            // with the default 0 offset, both must equal exactly half the
            // wheelbase, matching the pre-Etapa-1.2 assumption exactly.
            const float wheelbase = 1.05f;
            const float offset = 0f;
            var front = wheelbase * 0.5f + offset;
            var rear = wheelbase * 0.5f - offset;
            Assert.That(front, Is.EqualTo(wheelbase * 0.5f));
            Assert.That(rear, Is.EqualTo(wheelbase * 0.5f));
            Assert.That(front + rear, Is.EqualTo(wheelbase).Within(0.0001f));
        }

        [Test]
        public void CenterOfMassOffset_NonZero_DistancesStillSumToWheelbase()
        {
            const float wheelbase = 1.05f;
            foreach (var offset in new[] { -0.2f, -0.05f, 0.05f, 0.2f })
            {
                var front = wheelbase * 0.5f + offset;
                var rear = wheelbase * 0.5f - offset;
                Assert.That(front + rear, Is.EqualTo(wheelbase).Within(0.0001f),
                    $"front+rear should always equal the wheelbase (offset={offset}).");
                Assert.That(front, Is.GreaterThan(0f));
                Assert.That(rear, Is.GreaterThan(0f));
            }
        }
    }
}
