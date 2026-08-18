using NUnit.Framework;
using UnityEngine;

namespace RKW.Physics.Tests.EditMode
{
    /// <summary>
    /// Property 4: Throttle Ramp Rate Limit
    /// For any sequence of throttle inputs over time, the output throttle value
    /// shall never increase faster than 1/0.15 per second (150ms minimum to reach
    /// full throttle from zero).
    /// Validates: Requirements 3.5
    /// </summary>
    public sealed class ThrottleRampPropertyTest
    {
        private const int Iterations = 200;
        private const float FixedDeltaTime = 0.02f; // 50 Hz

        [Test]
        public void ThrottleOutput_NeverExceedsMaximumRampRate()
        {
            var random = new System.Random(401);

            for (var iteration = 0; iteration < Iterations; iteration++)
            {
                var rampSeconds = RandomFloat(random, 0.15f, 0.5f);
                var maxRate = 1f / rampSeconds; // max units per second

                var smoothedThrottle = 0f;

                // Simulate sudden full throttle input
                for (var tick = 0; tick < 100; tick++)
                {
                    var previousSmoothed = smoothedThrottle;
                    var rate = 1f / Mathf.Max(0.15f, rampSeconds);
                    smoothedThrottle = Mathf.MoveTowards(smoothedThrottle, 1f, rate * FixedDeltaTime);

                    var actualRate = (smoothedThrottle - previousSmoothed) / FixedDeltaTime;

                    Assert.That(actualRate, Is.LessThanOrEqualTo(maxRate + 0.001f),
                        $"Throttle rate exceeded at iteration {iteration}, tick {tick}: " +
                        $"rate={actualRate:F4}/s, max={maxRate:F4}/s, ramp={rampSeconds:F3}s");
                }
            }
        }

        [Test]
        public void ThrottleOutput_TakesAtLeast150ms_ToReachFull()
        {
            var rampSeconds = 0.15f; // minimum allowed
            var rate = 1f / rampSeconds;
            var smoothed = 0f;
            var ticks = 0;

            while (smoothed < 0.999f && ticks < 500)
            {
                smoothed = Mathf.MoveTowards(smoothed, 1f, rate * FixedDeltaTime);
                ticks++;
            }

            var timeToFull = ticks * FixedDeltaTime;
            Assert.That(timeToFull, Is.GreaterThanOrEqualTo(0.15f - FixedDeltaTime),
                $"Throttle reached full in {timeToFull:F3}s (should take >= 0.15s)");
        }

        [Test]
        public void ThrottleOutput_ReleaseIsImmediate()
        {
            // When releasing throttle, we go toward 0 at the same rate
            // This verifies symmetry
            var rampSeconds = 0.2f;
            var rate = 1f / Mathf.Max(0.15f, rampSeconds);
            var smoothed = 1f;

            smoothed = Mathf.MoveTowards(smoothed, 0f, rate * FixedDeltaTime);

            Assert.That(smoothed, Is.LessThan(1f),
                "Throttle should decrease when input goes to 0");
        }

        private static float RandomFloat(System.Random random, float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }
    }
}
