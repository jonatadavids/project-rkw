namespace RKW.Telemetry
{
    /// <summary>
    /// M3-T07: one periodic performance measurement (Requirement R12.4).
    /// Immutable — collectors build a new instance per sample instead of
    /// mutating shared state, so consumers/sinks can hold a reference safely.
    /// </summary>
    public readonly struct PerformanceSample
    {
        public PerformanceSample(float timeSeconds, float averageFps, long allocatedMemoryBytes, ThermalStatus thermalStatus)
        {
            TimeSeconds = timeSeconds;
            AverageFps = averageFps;
            AllocatedMemoryBytes = allocatedMemoryBytes;
            ThermalStatus = thermalStatus;
        }

        /// <summary>Unscaled time (seconds) at which this sample was taken.</summary>
        public float TimeSeconds { get; }

        /// <summary>Rolling average FPS over the collector's sampling window.</summary>
        public float AverageFps { get; }

        /// <summary>Total allocated managed memory, in bytes, at sample time.</summary>
        public long AllocatedMemoryBytes { get; }

        /// <summary>Coarse thermal category at sample time.</summary>
        public ThermalStatus ThermalStatus { get; }

        public double AllocatedMemoryMegabytes => AllocatedMemoryBytes / (1024.0 * 1024.0);
    }
}
