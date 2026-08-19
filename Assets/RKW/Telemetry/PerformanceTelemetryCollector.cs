using System;

namespace RKW.Telemetry
{
    /// <summary>
    /// M3-T07: ties together FPS rolling average, memory, and thermal status
    /// into periodic <see cref="PerformanceSample"/>s sent to a sink
    /// (Requirement R12.4). Deliberately plain C# — no MonoBehaviour, no
    /// direct Time.* calls — so the sampling-interval decision and the
    /// sample contents are fully EditMode testable. See
    /// <see cref="PerformanceTelemetryRunner"/> for the MonoBehaviour that
    /// drives this from Unity's Update loop.
    /// </summary>
    public sealed class PerformanceTelemetryCollector
    {
        private readonly FpsRollingAverage _fpsAverage;
        private readonly IMemorySampler _memorySampler;
        private readonly IThermalStatusProvider _thermalStatusProvider;
        private readonly ITelemetrySink _sink;
        private readonly float _sampleIntervalSeconds;
        private float _lastSampleTime = float.NegativeInfinity;

        public PerformanceTelemetryCollector(
            IMemorySampler memorySampler,
            IThermalStatusProvider thermalStatusProvider,
            ITelemetrySink sink,
            float sampleIntervalSeconds = 5f,
            int fpsWindowSampleCount = 60)
        {
            if (sampleIntervalSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(sampleIntervalSeconds), "Sample interval must be positive.");
            }

            _memorySampler = memorySampler ?? throw new ArgumentNullException(nameof(memorySampler));
            _thermalStatusProvider = thermalStatusProvider ?? throw new ArgumentNullException(nameof(thermalStatusProvider));
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
            _sampleIntervalSeconds = sampleIntervalSeconds;
            _fpsAverage = new FpsRollingAverage(fpsWindowSampleCount);
        }

        /// <summary>Rolling-average FPS as of the last <see cref="Tick"/> call.</summary>
        public float CurrentAverageFps => _fpsAverage.CurrentAverageFps;

        /// <summary>
        /// Call once per frame with the frame's delta time and the current
        /// unscaled time. Always feeds the FPS rolling average; only every
        /// <c>sampleIntervalSeconds</c> does it actually build a sample and
        /// push it to the sink (per-frame sends would spam the sink /
        /// eventually a real analytics backend — see Q-AN-03 in
        /// docs/13-analytics-telemetry.md, still open on exact cadence).
        /// Returns the emitted sample, or null on ticks that don't sample.
        /// </summary>
        public PerformanceSample? Tick(float deltaTimeSeconds, float timeSeconds)
        {
            _fpsAverage.Sample(deltaTimeSeconds);

            if (!ShouldSample(_lastSampleTime, timeSeconds, _sampleIntervalSeconds))
            {
                return null;
            }

            _lastSampleTime = timeSeconds;

            var sample = new PerformanceSample(
                timeSeconds,
                _fpsAverage.CurrentAverageFps,
                _memorySampler.GetAllocatedMemoryBytes(),
                _thermalStatusProvider.GetThermalStatus());

            _sink.Send(sample);
            return sample;
        }

        /// <summary>
        /// Pure decision of whether enough time has elapsed since the last
        /// sample. Exposed statically so the interval logic itself — not
        /// just the end-to-end Tick behavior — has a direct, fast EditMode
        /// test.
        /// </summary>
        public static bool ShouldSample(float lastSampleTime, float currentTime, float intervalSeconds)
        {
            if (float.IsNegativeInfinity(lastSampleTime))
            {
                return true;
            }

            return currentTime - lastSampleTime >= intervalSeconds;
        }
    }
}
