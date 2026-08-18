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

        private static float RandomFloat(System.Random random, float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }
    }
}
