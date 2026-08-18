using NUnit.Framework;

namespace RKW.Physics.Tests.EditMode
{
    /// <summary>
    /// Property 5: Weight Transfer Monotonicity
    /// For any kart at speed above threshold, increasing steering angle SHALL
    /// increase weight on the outer rear wheel (LateralWeightTransferRatio)
    /// monotonically.
    /// Validates: Requirements 4.2
    /// </summary>
    public sealed class WeightTransferMonotonicityPropertyTest
    {
        private const int Iterations = 200;
        private const int SteeringSamples = 50;

        // Realistic parameter ranges for property generation
        private const float MinSpeed = 3f;        // m/s (~11 km/h) - threshold
        private const float MaxSpeed = 28f;       // m/s (~100 km/h)
        private const float MinMaxSpeed = 15f;    // m/s category max speed
        private const float MaxMaxSpeed = 28f;
        private const float MinCogHeight = 0.15f; // meters
        private const float MaxCogHeight = 0.35f;
        private const float MinTrackWidth = 0.85f;
        private const float MaxTrackWidth = 1.20f;
        private const float MinGain = 1.5f;
        private const float MaxGain = 5.0f;

        [Test]
        public void WeightTransfer_IsMonotonicallyIncreasingWithSteering_ForAllValidInputs()
        {
            var random = new System.Random(42); // deterministic seed

            for (var iteration = 0; iteration < Iterations; iteration++)
            {
                // Generate random but valid physics parameters
                var maxSpeedMps = RandomFloat(random, MinMaxSpeed, MaxMaxSpeed);
                var speedMps = RandomFloat(random, MinSpeed, maxSpeedMps);
                var cogHeight = RandomFloat(random, MinCogHeight, MaxCogHeight);
                var trackWidth = RandomFloat(random, MinTrackWidth, MaxTrackWidth);
                var gain = RandomFloat(random, MinGain, MaxGain);

                var previousTransfer = 0f;

                for (var step = 0; step <= SteeringSamples; step++)
                {
                    var steering = (float)step / SteeringSamples; // 0.0 to 1.0

                    var transfer = KartDynamicsMath.CalculateLateralWeightTransferRatio(
                        speedMps,
                        steering,
                        maxSpeedMps,
                        cogHeight,
                        trackWidth,
                        gain);

                    Assert.That(transfer, Is.GreaterThanOrEqualTo(previousTransfer),
                        $"Monotonicity violated at iteration {iteration}: " +
                        $"steering={steering:F3}, speed={speedMps:F1}m/s, " +
                        $"transfer={transfer:F6} < previous={previousTransfer:F6}");

                    previousTransfer = transfer;
                }
            }
        }

        [Test]
        public void WeightTransfer_IsZeroOrPositive_ForAllValidInputs()
        {
            var random = new System.Random(7);

            for (var iteration = 0; iteration < Iterations; iteration++)
            {
                var maxSpeedMps = RandomFloat(random, MinMaxSpeed, MaxMaxSpeed);
                var speedMps = RandomFloat(random, 0f, maxSpeedMps);
                var steering = RandomFloat(random, -1f, 1f);
                var cogHeight = RandomFloat(random, MinCogHeight, MaxCogHeight);
                var trackWidth = RandomFloat(random, MinTrackWidth, MaxTrackWidth);
                var gain = RandomFloat(random, MinGain, MaxGain);

                var transfer = KartDynamicsMath.CalculateLateralWeightTransferRatio(
                    speedMps, steering, maxSpeedMps, cogHeight, trackWidth, gain);

                Assert.That(transfer, Is.InRange(0f, 1f),
                    $"Transfer out of [0,1] at iteration {iteration}: " +
                    $"speed={speedMps:F1}, steer={steering:F3}, result={transfer:F6}");
            }
        }

        [Test]
        public void WeightTransfer_IncreasesWithSpeed_AtFixedSteering()
        {
            var random = new System.Random(99);

            for (var iteration = 0; iteration < Iterations; iteration++)
            {
                var maxSpeedMps = RandomFloat(random, MinMaxSpeed, MaxMaxSpeed);
                var steering = RandomFloat(random, 0.2f, 1f); // meaningful steering
                var cogHeight = RandomFloat(random, MinCogHeight, MaxCogHeight);
                var trackWidth = RandomFloat(random, MinTrackWidth, MaxTrackWidth);
                var gain = RandomFloat(random, MinGain, MaxGain);

                var previousTransfer = 0f;

                for (var step = 0; step <= SteeringSamples; step++)
                {
                    var speed = (float)step / SteeringSamples * maxSpeedMps;

                    var transfer = KartDynamicsMath.CalculateLateralWeightTransferRatio(
                        speed, steering, maxSpeedMps, cogHeight, trackWidth, gain);

                    Assert.That(transfer, Is.GreaterThanOrEqualTo(previousTransfer),
                        $"Speed monotonicity violated at iteration {iteration}: " +
                        $"speed={speed:F1}m/s, steer={steering:F3}, " +
                        $"transfer={transfer:F6} < previous={previousTransfer:F6}");

                    previousTransfer = transfer;
                }
            }
        }

        private static float RandomFloat(System.Random random, float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }
    }
}
