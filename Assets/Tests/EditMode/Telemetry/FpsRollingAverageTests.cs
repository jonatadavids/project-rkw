using NUnit.Framework;
using RKW.Telemetry;

namespace RKW.Telemetry.Tests.EditMode
{
    /// <summary>
    /// M3-T07: pure logic tests for the rolling FPS accumulator.
    /// Validates: Requirement R12.4 ("coletar FPS a cada frame, rolling average").
    /// </summary>
    public sealed class FpsRollingAverageTests
    {
        [Test]
        public void NoSamples_AverageIsZero()
        {
            var average = new FpsRollingAverage(10);

            Assert.That(average.CurrentAverageFps, Is.EqualTo(0f));
            Assert.That(average.SampleCount, Is.EqualTo(0));
        }

        [Test]
        public void ConstantDeltaTime_AverageMatchesInstantFps()
        {
            var average = new FpsRollingAverage(10);

            // 60 FPS -> 1/60s per frame.
            for (var i = 0; i < 5; i++)
            {
                average.Sample(1f / 60f);
            }

            Assert.That(average.CurrentAverageFps, Is.EqualTo(60f).Within(0.01f));
            Assert.That(average.SampleCount, Is.EqualTo(5));
        }

        [Test]
        public void WindowFull_OldestSampleIsEvicted()
        {
            var average = new FpsRollingAverage(3);

            average.Sample(1f / 30f); // 30 FPS, should be evicted
            average.Sample(1f / 60f);
            average.Sample(1f / 60f);
            average.Sample(1f / 60f); // pushes the 30 FPS sample out

            Assert.That(average.SampleCount, Is.EqualTo(3));
            Assert.That(average.CurrentAverageFps, Is.EqualTo(60f).Within(0.01f));
        }

        [TestCase(0f)]
        [TestCase(-0.5f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void NonPositiveOrInvalidDeltaTime_IsIgnored(float invalidDeltaTime)
        {
            var average = new FpsRollingAverage(10);
            average.Sample(1f / 60f);

            average.Sample(invalidDeltaTime);

            Assert.That(average.SampleCount, Is.EqualTo(1),
                "Invalid delta times must not be recorded as samples.");
            Assert.That(average.CurrentAverageFps, Is.EqualTo(60f).Within(0.01f));
        }

        [Test]
        public void Reset_ClearsAllSamples()
        {
            var average = new FpsRollingAverage(10);
            average.Sample(1f / 60f);
            average.Sample(1f / 60f);

            average.Reset();

            Assert.That(average.SampleCount, Is.EqualTo(0));
            Assert.That(average.CurrentAverageFps, Is.EqualTo(0f));
        }
    }
}
