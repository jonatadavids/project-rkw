using NUnit.Framework;

namespace RKW.Telemetry.Tests.EditMode
{
    /// <summary>
    /// M3-T07: end-to-end collector tests using fake providers/sink, so no
    /// real device, Profiler, or Android JNI call is needed.
    /// Validates: Requirement R12.4 (FPS/memory/thermal sampling + sending).
    /// </summary>
    public sealed class PerformanceTelemetryCollectorTests
    {
        private sealed class FakeMemorySampler : IMemorySampler
        {
            public long BytesToReturn = 123_456_789L;
            public long GetAllocatedMemoryBytes() => BytesToReturn;
        }

        private sealed class FakeThermalStatusProvider : IThermalStatusProvider
        {
            public ThermalStatus StatusToReturn = ThermalStatus.Nominal;
            public ThermalStatus GetThermalStatus() => StatusToReturn;
        }

        private sealed class FakeTelemetrySink : ITelemetrySink
        {
            public readonly System.Collections.Generic.List<PerformanceSample> Received = new();
            public void Send(PerformanceSample sample) => Received.Add(sample);
        }

        [Test]
        public void FirstTick_AlwaysProducesASample()
        {
            var sink = new FakeTelemetrySink();
            var collector = new PerformanceTelemetryCollector(
                new FakeMemorySampler(), new FakeThermalStatusProvider(), sink, sampleIntervalSeconds: 5f);

            var result = collector.Tick(1f / 60f, timeSeconds: 0f);

            Assert.That(result, Is.Not.Null);
            Assert.That(sink.Received.Count, Is.EqualTo(1));
        }

        [Test]
        public void TicksBeforeInterval_DoNotProduceSamples()
        {
            var sink = new FakeTelemetrySink();
            var collector = new PerformanceTelemetryCollector(
                new FakeMemorySampler(), new FakeThermalStatusProvider(), sink, sampleIntervalSeconds: 5f);

            collector.Tick(1f / 60f, 0f);
            var second = collector.Tick(1f / 60f, 1f);
            var third = collector.Tick(1f / 60f, 4.9f);

            Assert.That(second, Is.Null);
            Assert.That(third, Is.Null);
            Assert.That(sink.Received.Count, Is.EqualTo(1));
        }

        [Test]
        public void TickAtOrAfterInterval_ProducesAnotherSample()
        {
            var sink = new FakeTelemetrySink();
            var collector = new PerformanceTelemetryCollector(
                new FakeMemorySampler(), new FakeThermalStatusProvider(), sink, sampleIntervalSeconds: 5f);

            collector.Tick(1f / 60f, 0f);
            var result = collector.Tick(1f / 60f, 5f);

            Assert.That(result, Is.Not.Null);
            Assert.That(sink.Received.Count, Is.EqualTo(2));
        }

        [Test]
        public void Sample_CarriesFpsMemoryAndThermalFromProviders()
        {
            var memorySampler = new FakeMemorySampler { BytesToReturn = 50_000_000L };
            var thermalProvider = new FakeThermalStatusProvider { StatusToReturn = ThermalStatus.Severe };
            var sink = new FakeTelemetrySink();
            var collector = new PerformanceTelemetryCollector(memorySampler, thermalProvider, sink, sampleIntervalSeconds: 5f);

            // Constant 60 FPS for a few frames before the sample is taken.
            collector.Tick(1f / 60f, 0f);
            var result = collector.Tick(1f / 60f, 5f);

            Assert.That(result, Is.Not.Null);
            var sample = result.Value;
            Assert.That(sample.AverageFps, Is.EqualTo(60f).Within(0.01f));
            Assert.That(sample.AllocatedMemoryBytes, Is.EqualTo(50_000_000L));
            Assert.That(sample.ThermalStatus, Is.EqualTo(ThermalStatus.Severe));
        }

        [TestCase(float.NegativeInfinity, 0f, 5f, ExpectedResult = true, TestName = "NoPriorSample_AlwaysSamples")]
        [TestCase(0f, 4.99f, 5f, ExpectedResult = false, TestName = "BeforeInterval_DoesNotSample")]
        [TestCase(0f, 5f, 5f, ExpectedResult = true, TestName = "ExactlyAtInterval_Samples")]
        [TestCase(0f, 10f, 5f, ExpectedResult = true, TestName = "PastInterval_Samples")]
        public bool ShouldSample_PureDecision(float lastSampleTime, float currentTime, float intervalSeconds)
        {
            return PerformanceTelemetryCollector.ShouldSample(lastSampleTime, currentTime, intervalSeconds);
        }

        [Test]
        public void Constructor_RejectsNonPositiveInterval()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                new PerformanceTelemetryCollector(new FakeMemorySampler(), new FakeThermalStatusProvider(), new FakeTelemetrySink(), sampleIntervalSeconds: 0f));
        }
    }
}
