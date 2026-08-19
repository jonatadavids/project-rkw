using UnityEngine.Profiling;

namespace RKW.Telemetry
{
    /// <summary>
    /// M3-T07: abstraction over "how much managed memory is allocated right
    /// now" (Requirement R12.4 — Profiler.GetTotalAllocatedMemoryLong). Kept
    /// as an interface so <see cref="PerformanceTelemetryCollector"/> can be
    /// unit tested with a fake sampler instead of depending on the real
    /// Unity profiler counters.
    /// </summary>
    public interface IMemorySampler
    {
        long GetAllocatedMemoryBytes();
    }

    /// <summary>Default sampler: wraps UnityEngine.Profiling.Profiler.</summary>
    public sealed class ProfilerMemorySampler : IMemorySampler
    {
        public long GetAllocatedMemoryBytes()
        {
            return Profiler.GetTotalAllocatedMemoryLong();
        }
    }
}
