using NUnit.Framework;

namespace RKW.Physics.Tests.EditMode
{
    /// <summary>
    /// Property 10: Slipstream Drag Reduction Monotonicity
    /// For any two distances d1 less than d2 both within slipstream activation range,
    /// the drag reduction at d1 SHALL be greater than or equal to at d2.
    /// Validates: Requirements 4.8
    /// </summary>
    public sealed class SlipstreamPropertyTest
    {
        private const int Iterations = 200;
        private const int DistanceSamples = 30;

        [Test]
        public void DragReduction_IsMonotonicallyDecreasingWithDistance()
        {
            var random = new System.Random(1601);

            for (var iteration = 0; iteration < Iterations; iteration++)
            {
                var kartLength = RandomFloat(random, 1.5f, 2.0f);
                var maxActivation = RandomFloat(random, 1.2f, 2.0f); // in kart lengths
                var maxReduction = RandomFloat(random, 0.05f, 0.12f);
                var minTime = RandomFloat(random, 0.5f, 1.5f);
                var timeIn = minTime + 1f; // always above minimum

                var previousReduction = float.MaxValue;

                for (var step = 1; step <= DistanceSamples; step++)
                {
                    var distance = (float)step / DistanceSamples * maxActivation * kartLength;

                    var reduction = KartDynamicsMath.CalculateSlipstreamDragReduction(
                        distance, kartLength, maxActivation, maxReduction, minTime, timeIn);

                    Assert.That(reduction, Is.LessThanOrEqualTo(previousReduction),
                        $"Monotonicity violated at iteration {iteration}: " +
                        $"dist={distance:F2}m, reduction={reduction:F6} > previous={previousReduction:F6}");

                    previousReduction = reduction;
                }
            }
        }

        [Test]
        public void DragReduction_IsZero_WhenTimeInSlipstreamBelowMinimum()
        {
            var random = new System.Random(1602);

            for (var iteration = 0; iteration < Iterations; iteration++)
            {
                var kartLength = RandomFloat(random, 1.5f, 2.0f);
                var maxActivation = 1.5f;
                var maxReduction = 0.08f;
                var minTime = RandomFloat(random, 0.5f, 2f);
                var timeIn = minTime * RandomFloat(random, 0f, 0.99f); // below minimum
                var distance = RandomFloat(random, 0.5f, maxActivation * kartLength);

                var reduction = KartDynamicsMath.CalculateSlipstreamDragReduction(
                    distance, kartLength, maxActivation, maxReduction, minTime, timeIn);

                Assert.That(reduction, Is.EqualTo(0f),
                    $"Should be 0 when time below minimum at iteration {iteration}");
            }
        }

        [Test]
        public void DragReduction_IsZero_WhenOutsideActivationRange()
        {
            var random = new System.Random(1603);

            for (var iteration = 0; iteration < Iterations; iteration++)
            {
                var kartLength = RandomFloat(random, 1.5f, 2.0f);
                var maxActivation = 1.5f;
                var maxReduction = 0.08f;
                var minTime = 1f;
                var timeIn = 2f;
                var distance = maxActivation * kartLength + RandomFloat(random, 0.1f, 5f);

                var reduction = KartDynamicsMath.CalculateSlipstreamDragReduction(
                    distance, kartLength, maxActivation, maxReduction, minTime, timeIn);

                Assert.That(reduction, Is.EqualTo(0f),
                    $"Should be 0 outside range at iteration {iteration}, dist={distance:F2}");
            }
        }

        [Test]
        public void DragReduction_NeverExceedsMaxReduction()
        {
            var random = new System.Random(1604);

            for (var iteration = 0; iteration < Iterations; iteration++)
            {
                var kartLength = RandomFloat(random, 1.5f, 2.0f);
                var maxActivation = RandomFloat(random, 1.2f, 2.0f);
                var maxReduction = RandomFloat(random, 0.01f, 0.15f);
                var minTime = 1f;
                var timeIn = 5f;
                var distance = RandomFloat(random, 0.01f, maxActivation * kartLength);

                var reduction = KartDynamicsMath.CalculateSlipstreamDragReduction(
                    distance, kartLength, maxActivation, maxReduction, minTime, timeIn);

                Assert.That(reduction, Is.LessThanOrEqualTo(maxReduction + 0.0001f),
                    $"Exceeded max at iteration {iteration}: reduction={reduction:F6}, max={maxReduction:F6}");
            }
        }

        private static float RandomFloat(System.Random random, float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }
    }
}
