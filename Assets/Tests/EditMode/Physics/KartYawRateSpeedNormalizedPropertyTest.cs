using NUnit.Framework;
using UnityEngine;

namespace RKW.Physics.Tests.EditMode
{
    /// <summary>
    /// RECOVERY tuning round 4 (2026-08-31) -- the real cause behind three
    /// straight rounds of "18 HP ainda pesada, ainda mais lenta que a 13
    /// HP" founder feedback, each of which fixed a real but secondary issue
    /// (front slip-angle geometry mismatch, brake oversteer, engine
    /// braking, friction ellipse bias) without touching the actual
    /// dominant factor.
    ///
    /// The production yaw formula (see
    /// KartDynamicsMath.CalculateAckermannYawRateDegreesPerSecond and
    /// LimitYawRateToAvailableGrip) is, once grip-limited -- which happens
    /// well before reaching max steering angle at any real driving speed --
    /// mathematically equivalent to:
    ///
    ///     yawRateDegPerSec = (LateralGripG * 9.81) / forwardSpeedMetersPerSecond
    ///
    /// This means yaw rate (how fast the kart visibly rotates, in
    /// degrees/second -- what a driver reads as "how sharply it turns") is
    /// INVERSELY proportional to speed for a fixed grip budget. A category
    /// tuned to go faster will therefore always look like it "turns less"
    /// unless its LateralGripG is boosted by AT LEAST the same ratio as its
    /// top speed increase relative to the category it is compared against.
    /// A diagnostic PlayMode test run on the founder's own build on
    /// 2026-08-31 (KartCategoryComparisonDiagnosticTest, search
    /// rkw_playmode_tests.log for "[DIAG]") measured this directly: with
    /// LateralGripG at 1.7 (school=1.25, ratio 1.36) against a top-speed
    /// ratio of 1.42 (85/60 kph), the 18 HP's actual yaw rate came out
    /// LOWER than the 13 HP's whenever it carried proportionally more of
    /// its own top speed into the corner -- exactly matching "parece mais
    /// pesada, mais lenta em curva".
    ///
    /// This test locks in the fix (LateralGripG raised to 2.0, giving a
    /// 1.6 ratio, comfortably above the 1.42 speed ratio) as a permanent
    /// property: at any given FRACTION of each category's own top speed,
    /// with the same steering input and no throttle/brake demand (so the
    /// friction ellipse and axle-grip-ratio terms are both neutral at
    /// 1.0), the faster category's yaw rate must be at least as high as
    /// the slower category's. This is the actual playability requirement
    /// behind the founder's repeated feedback -- not merely "front slip
    /// curves match" (round 3, section 10) or "individual numbers look more
    /// generous" (round 2, section 9), both of which passed while this
    /// property was still failing.
    /// </summary>
    public sealed class KartYawRateSpeedNormalizedPropertyTest
    {
        private const float SteeringInput = 0.6f;

        private static float YawRateAtSpeedFraction(KartCategorySO category, float speedFraction)
        {
            var speedMetersPerSecond = category.MaxSpeedKph / 3.6f * speedFraction;
            var requested = KartDynamicsMath.CalculateAckermannYawRateDegreesPerSecond(
                SteeringInput, speedMetersPerSecond, category.WheelbaseMeters, category.MaxSteeringAngleDegrees);
            var maxLateralAcceleration = category.LateralGripG * KartDynamicsMath.Gravity;
            return KartDynamicsMath.LimitYawRateToAvailableGrip(requested, speedMetersPerSecond, maxLateralAcceleration);
        }

        [TestCase(0.4f)]
        [TestCase(0.5f)]
        [TestCase(0.6f)]
        [TestCase(0.7f)]
        [TestCase(0.8f)]
        [TestCase(0.9f)]
        [TestCase(1.0f)]
        public void RentalSport_YawRateAtMatchedSpeedFraction_IsNotLowerThanSchool(float speedFraction)
        {
            var school = Resources.Load<KartCategorySO>("KartPhysics/PrototypeSchoolTuning");
            var rentalSport = Resources.Load<KartCategorySO>("KartPhysics/PrototypeRentalSportTuning");

            Assert.That(school, Is.Not.Null, "School tuning asset must exist");
            Assert.That(rentalSport, Is.Not.Null, "Rental Sport tuning asset must exist");

            var schoolYawRate = YawRateAtSpeedFraction(school, speedFraction);
            var rentalYawRate = YawRateAtSpeedFraction(rentalSport, speedFraction);

            // Small tolerance (2%) for floating point noise -- this is not
            // meant to allow a real regression back toward the "feels
            // heavier" complaint, just to avoid failing on rounding.
            Assert.That(rentalYawRate, Is.GreaterThanOrEqualTo(schoolYawRate * 0.98f),
                $"At {speedFraction:P0} of each category's own top speed, RentalSport (18 HP) yaw rate " +
                $"({rentalYawRate:F1} deg/s) should not be lower than School (13 HP)'s ({schoolYawRate:F1} deg/s) -- " +
                "otherwise the faster category will feel like it turns worse than the slower one, which is exactly " +
                "the founder feedback this round exists to fix.");
        }
    }
}
