using UnityEngine;

namespace RKW.Telemetry
{
    /// <summary>
    /// M3-T07: MonoBehaviour wrapper that drives a
    /// <see cref="PerformanceTelemetryCollector"/> from Unity's Update loop
    /// with production defaults (real Profiler memory, platform-appropriate
    /// thermal provider, log-based sink — see <see cref="ITelemetrySink"/>
    /// for why no analytics backend is wired in yet). All actual decision
    /// logic lives in the plain-C# collector so it stays EditMode testable;
    /// this class exists only to supply Time.unscaledDeltaTime/unscaledTime.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PerformanceTelemetryRunner : MonoBehaviour
    {
        [Tooltip("Seconds between telemetry samples sent to the sink. FPS is still averaged every frame in between.")]
        [SerializeField] private float sampleIntervalSeconds = 5f;

        [Tooltip("Number of frames the FPS rolling average is computed over.")]
        [SerializeField] private int fpsWindowSampleCount = 60;

        private PerformanceTelemetryCollector _collector;

        public PerformanceSample? LastSample { get; private set; }

        private void Awake()
        {
            IThermalStatusProvider thermalStatusProvider =
#if UNITY_ANDROID
                new AndroidThermalStatusProvider();
#else
                new UnsupportedThermalStatusProvider();
#endif

            _collector = new PerformanceTelemetryCollector(
                new ProfilerMemorySampler(),
                thermalStatusProvider,
                new LogTelemetrySink(),
                sampleIntervalSeconds,
                fpsWindowSampleCount);
        }

        private void Update()
        {
            var sample = _collector.Tick(Time.unscaledDeltaTime, Time.unscaledTime);
            if (sample.HasValue)
            {
                LastSample = sample;
            }
        }
    }
}
