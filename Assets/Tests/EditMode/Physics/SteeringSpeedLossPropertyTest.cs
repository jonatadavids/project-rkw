using NUnit.Framework;

namespace RKW.Physics.Tests.EditMode
{
    /// <summary>
    /// Property 6: Steering Speed Loss
    /// For any kart at elevated speed, increasing steering angle SHALL increase
    /// speed loss proportionally — speed loss is monotonically increasing with
    /// steer angle magnitude.
    /// Validates: Requirements 4.3
    /// </summary>
    public sealed class SteeringSpeedLossPropertyTest
    {
        private const int Iterations = 200;
        private const int SteeringSamples = 50;

        private const float MinSpeed = 3f;
        private const float MaxSpeed = 28f;
        private const float MinMaxSpeed = 15f;
        private const float MaxMaxSpeed = 28f;
        private const float MinLossAccel = 1.0f;
        private const float MaxLossAccel = 5.0f;

        [Test]
        public void SteeringSpeedLoss_IsMonotonicallyIncreasingWithSteeringMagnitude()
        {
            var random = new System.Random(101);

            for (var iteration = 0; iteration < Iterations; iteration++)
            {
                var maxSpeedMps = RandomFloat(random, MinMaxSpeed, MaxMaxSpeed);
                var speedMps = RandomFloat(random, MinSpeed, maxSpeedMps);
                var maxLoss = RandomFloat(random, MinLossAccel, MaxLossAccel);

                var previousLoss = 0f;

                for (var step = 0; step <= SteeringSamples; step++)
                {
                    var steering = (float)step / SteeringSamples;

                    var loss = KartDynamicsMath.CalculateSteeringSpeedLoss(
                        steering, speedMps, maxSpeedMps, maxLoss);

                    Assert.That(loss, Is.GreaterThanOrEqualTo(previousLoss),
                        $"Monotonicity violated at iteration {iteration}: " +
                        $"steering={steering:F3}, speed={speedMps:F1}m/s, " +
                        $"loss={loss:F6} < previous={previousLoss:F6}");

                    previousLoss = loss;
                }
            }
        }

        [Test]
        public void SteeringSpeedLoss_IsZeroAtZeroSteering()
        {
            var random = new System.Random(202);

            for (var iteration = 0; iteration < Iterations; iteration++)
            {
                var maxSpeedMps = RandomFloat(random, MinMaxSpeed, MaxMaxSpeed);
                var speedMps = RandomFloat(random, 0f, maxSpeedMps);
                var maxLoss = RandomFloat(random, MinLossAccel, MaxLossAccel);

                var loss = KartDynamicsMath.CalculateSteeringSpeedLoss(
                    0f, speedMps, maxSpeedMps, maxLoss);

                Assert.That(loss, Is.EqualTo(0f).Within(0.0001f),
                    $"Loss should be zero with no steering at iteration {iteration}");
            }
        }

        [Test]
        public void SteeringSpeedLoss_IncreasesWithSpeed_AtFixedSteering()
        {
            var random = new System.Random(303);

            for (var iteration = 0; iteration < Iterations; iteration++)
            {
                var maxSpeedMps = RandomFloat(random, MinMaxSpeed, MaxMaxSpeed);
                var steering = RandomFloat(random, 0.2f, 1f);
                var maxLoss = RandomFloat(random, MinLossAccel, MaxLossAccel);

                var previousLoss = 0f;

                for (var step = 0; step <= SteeringSamples; step++)
                {
                    var speed = (float)step / SteeringSamples * maxSpeedMps;

                    var loss = KartDynamicsMath.CalculateSteeringSpeedLoss(
                        steering, speed, maxSpeedMps, maxLoss);

                    Assert.That(loss, Is.GreaterThanOrEqualTo(previousLoss),
                        $"Speed monotonicity violated at iteration {iteration}: " +
                        $"speed={speed:F1}m/s, steer={steering:F3}, " +
                        $"loss={loss:F6} < previous={previousLoss:F6}");

                    previousLoss = loss;
                }
            }
        }

        private static float RandomFloat(System.Random random, float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }
    }
}
