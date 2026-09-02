using NUnit.Framework;

namespace RKW.Physics.Tests.EditMode
{
    /// <summary>
    /// Property 7: Straight Braking Superiority
    /// For any initial speed above minimum threshold, the effective braking
    /// deceleration when braking in a straight line SHALL be greater than
    /// when braking with any non-zero steering angle applied (therefore
    /// stopping distance in straight line is shorter).
    /// Validates: Requirements 4.4
    ///
    /// Property 8: Brake-Steer Oversteer
    /// For any speed and non-zero steer angle, adding braking force SHALL
    /// increase oversteer factor compared to the same steer angle without braking.
    /// Validates: Requirements 4.5
    /// </summary>
    public sealed class BrakingPropertyTests
    {
        private const int Iterations = 200;

        private const float MinSpeed = 3f;
        private const float MaxSpeed = 28f;
        private const float MinBrakeDecel = 6f;
        private const float MaxBrakeDecel = 14f;
        private const float MinRearDist = 0.5f;
        private const float MaxRearDist = 0.95f;
        private const float MinGrip = 0.5f;
        private const float MaxGrip = 1.0f;
        private const float MinLateralG = 0.8f;
        private const float MaxLateralG = 1.4f;
        private const float MinOversteerGain = 0.5f;
        private const float MaxOversteerGain = 2.5f;

        [Test]
        public void StraightBraking_HasHigherDeceleration_ThanBrakingWithSteering()
        {
            var random = new System.Random(501);

            for (var iteration = 0; iteration < Iterations; iteration++)
            {
                var speed = RandomFloat(random, MinSpeed, MaxSpeed);
                var brakeInput = RandomFloat(random, 0.3f, 1f);
                var brakeDecel = RandomFloat(random, MinBrakeDecel, MaxBrakeDecel);
                var rearDist = RandomFloat(random, MinRearDist, MaxRearDist);
                var grip = RandomFloat(random, MinGrip, MaxGrip);
                var lateralG = RandomFloat(random, MinLateralG, MaxLateralG);
                var oversteerGain = RandomFloat(random, MinOversteerGain, MaxOversteerGain);
                var steering = RandomFloat(random, 0.15f, 1f);

                KartDynamicsMath.CalculateBrakingWithSteering(
                    brakeInput, 0f, speed, brakeDecel, rearDist, grip, lateralG, oversteerGain,
                    out var straightDecel, out _);

                KartDynamicsMath.CalculateBrakingWithSteering(
                    brakeInput, steering, speed, brakeDecel, rearDist, grip, lateralG, oversteerGain,
                    out var steerDecel, out _);

                Assert.That(straightDecel, Is.GreaterThanOrEqualTo(steerDecel),
                    $"Straight braking should be >= braking with steering at iteration {iteration}: " +
                    $"straight={straightDecel:F4}, withSteer={steerDecel:F4}, steer={steering:F3}");
            }
        }

        [Test]
        public void BrakeSteerOversteer_IsGreaterThanZero_WhenBrakingAndSteering()
        {
            var random = new System.Random(601);

            for (var iteration = 0; iteration < Iterations; iteration++)
            {
                var speed = RandomFloat(random, MinSpeed, MaxSpeed);
                var brakeInput = RandomFloat(random, 0.2f, 1f);
                var brakeDecel = RandomFloat(random, MinBrakeDecel, MaxBrakeDecel);
                var rearDist = RandomFloat(random, MinRearDist, MaxRearDist);
                var grip = RandomFloat(random, MinGrip, MaxGrip);
                var lateralG = RandomFloat(random, MinLateralG, MaxLateralG);
                var oversteerGain = RandomFloat(random, MinOversteerGain, MaxOversteerGain);
                var steering = RandomFloat(random, 0.1f, 1f);

                KartDynamicsMath.CalculateBrakingWithSteering(
                    brakeInput, steering, speed, brakeDecel, rearDist, grip, lateralG, oversteerGain,
                    out _, out var oversteerFactor);

                Assert.That(oversteerFactor, Is.GreaterThan(0f),
                    $"Oversteer should be > 0 when braking with steering at iteration {iteration}: " +
                    $"brake={brakeInput:F3}, steer={steering:F3}, oversteer={oversteerFactor:F4}");
            }
        }

        [Test]
        public void BrakeSteerOversteer_IsZero_WhenNoSteering()
        {
            var random = new System.Random(701);

            for (var iteration = 0; iteration < Iterations; iteration++)
            {
                var speed = RandomFloat(random, MinSpeed, MaxSpeed);
                var brakeInput = RandomFloat(random, 0.1f, 1f);
                var brakeDecel = RandomFloat(random, MinBrakeDecel, MaxBrakeDecel);
                var rearDist = RandomFloat(random, MinRearDist, MaxRearDist);
                var grip = RandomFloat(random, MinGrip, MaxGrip);
                var lateralG = RandomFloat(random, MinLateralG, MaxLateralG);
                var oversteerGain = RandomFloat(random, MinOversteerGain, MaxOversteerGain);

                KartDynamicsMath.CalculateBrakingWithSteering(
                    brakeInput, 0f, speed, brakeDecel, rearDist, grip, lateralG, oversteerGain,
                    out _, out var oversteerFactor);

                Assert.That(oversteerFactor, Is.EqualTo(0f).Within(0.0001f),
                    $"Oversteer should be 0 with no steering at iteration {iteration}");
            }
        }

        [Test]
        public void BrakeSteerOversteer_IncreasesWithBrakeInput()
        {
            var random = new System.Random(801);

            for (var iteration = 0; iteration < Iterations; iteration++)
            {
                var speed = RandomFloat(random, MinSpeed, MaxSpeed);
                var brakeDecel = RandomFloat(random, MinBrakeDecel, MaxBrakeDecel);
                var rearDist = RandomFloat(random, MinRearDist, MaxRearDist);
                var grip = RandomFloat(random, MinGrip, MaxGrip);
                var lateralG = RandomFloat(random, MinLateralG, MaxLateralG);
                var oversteerGain = RandomFloat(random, MinOversteerGain, MaxOversteerGain);
                var steering = RandomFloat(random, 0.2f, 1f);

                var previousOversteer = 0f;
                for (var step = 0; step <= 10; step++)
                {
                    var brake = (float)step / 10f;

                    KartDynamicsMath.CalculateBrakingWithSteering(
                        brake, steering, speed, brakeDecel, rearDist, grip, lateralG, oversteerGain,
                        out _, out var oversteer);

                    Assert.That(oversteer, Is.GreaterThanOrEqualTo(previousOversteer),
                        $"Oversteer should increase with brake at iteration {iteration}: " +
                        $"brake={brake:F2}, oversteer={oversteer:F4} < previous={previousOversteer:F4}");

                    previousOversteer = oversteer;
                }
            }
        }

        [Test]
        public void EffectiveDeceleration_IsReduced_WhenRequestedBrakingExceedsAvailableGrip()
        {
            // Etapa 5 (2026-08-31): wheel-lock modeling already existed
            // inside CalculateBrakingWithSteering (the lockRatio > 1
            // branch) but had no direct test coverage exercising that
            // specific branch -- BrakingPropertyTests above only ever used
            // brake/grip combinations that stay within grip. Force an
            // overshoot here (very high requested brake decel, low
            // available grip) and confirm the REAL function actually
            // reduces its output below the naive "decel = maxBrake*input"
            // request, rather than letting a locked wheel keep decelerating
            // at full requested force.
            const float brakeInput = 1f;
            const float maxBrakeDeceleration = 20f; // deliberately high
            const float lowGrip = 0.3f; // deliberately low -> low available grip
            const float lateralGripG = 1f;

            KartDynamicsMath.CalculateBrakingWithSteering(
                brakeInput, 0f, 15f, maxBrakeDeceleration, 0.7f, lowGrip, lateralGripG, 1.2f,
                out var effectiveDeceleration, out _);

            var requestedDeceleration = maxBrakeDeceleration * brakeInput;
            Assert.That(effectiveDeceleration, Is.LessThan(requestedDeceleration),
                $"Requested {requestedDeceleration:F2} m/s2 exceeds available grip -- effective ({effectiveDeceleration:F2}) " +
                "should be reduced by the wheel-lock branch, not equal the raw request.");
        }

        [Test]
        public void EffectiveDeceleration_MatchesRequest_WhenWellWithinAvailableGrip()
        {
            const float brakeInput = 0.3f;
            const float maxBrakeDeceleration = 8f;
            const float highGrip = 1f;
            const float lateralGripG = 1.4f; // plenty of available grip

            KartDynamicsMath.CalculateBrakingWithSteering(
                brakeInput, 0f, 15f, maxBrakeDeceleration, 0.7f, highGrip, lateralGripG, 1.2f,
                out var effectiveDeceleration, out _);

            var requestedDeceleration = maxBrakeDeceleration * brakeInput;
            Assert.That(effectiveDeceleration, Is.EqualTo(requestedDeceleration).Within(0.0001f),
                "Well within available grip, effective deceleration should exactly match the requested value (no lock-up reduction).");
        }

        [Test]
        public void WheelLockRatio_IsZero_WhenRequestedIsWithinAvailableGrip()
        {
            var ratio = KartDynamicsMath.CalculateWheelLockRatio(5f, 10f);
            Assert.That(ratio, Is.EqualTo(0f));
        }

        [Test]
        public void WheelLockRatio_RampsToOne_AsRequestedOvershootsAvailableGripBy50Percent()
        {
            var atThreshold = KartDynamicsMath.CalculateWheelLockRatio(10f, 10f);
            var atHalfway = KartDynamicsMath.CalculateWheelLockRatio(12.5f, 10f);
            var atFullLock = KartDynamicsMath.CalculateWheelLockRatio(15f, 10f);
            var beyondFullLock = KartDynamicsMath.CalculateWheelLockRatio(30f, 10f);

            Assert.That(atThreshold, Is.EqualTo(0f));
            Assert.That(atHalfway, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(atFullLock, Is.EqualTo(1f).Within(0.001f));
            Assert.That(beyondFullLock, Is.EqualTo(1f).Within(0.001f), "Should clamp at 1, never exceed it.");
        }

        private static float RandomFloat(System.Random random, float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }
    }
}
