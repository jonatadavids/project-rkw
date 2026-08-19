using UnityEngine;

namespace RKW.Telemetry
{
    /// <summary>
    /// M3-T07: where a <see cref="PerformanceSample"/> goes once collected.
    /// Deliberately decoupled from any specific analytics backend.
    ///
    /// Requirement R12.4 / the task list call for sending samples "via
    /// Unity Analytics", but that is intentionally NOT wired up here: per
    /// AGENTS.md rule 5 ("Sem Pacote Unity sem ADR"), adding the Unity
    /// Gaming Services Analytics package requires an approved ADR, and
    /// docs/23-ugs-foundation.md explicitly records that Analytics has not
    /// been enabled yet. docs/13-analytics-telemetry.md's own open question
    /// Q-AN-01 ("Usar Unity Analytics ou migrar para Amplitude/Mixpanel?")
    /// is still unresolved, so picking a backend now would be a silent
    /// architecture decision. <see cref="LogTelemetrySink"/> is a
    /// deliberately boring default that makes samples visible in the
    /// Profiler/logcat today; swap in a real sink once Q-AN-01 is decided
    /// and the corresponding package is ADR-approved.
    /// </summary>
    public interface ITelemetrySink
    {
        void Send(PerformanceSample sample);
    }

    /// <summary>
    /// Default sink: writes one structured log line per sample. Cheap,
    /// requires no new package/service, and is enough to eyeball FPS/memory/
    /// thermal trends via `adb logcat` during the M3-T08 real-device test.
    /// </summary>
    public sealed class LogTelemetrySink : ITelemetrySink
    {
        public void Send(PerformanceSample sample)
        {
            Debug.Log("[RKW.Telemetry] " +
                $"t={sample.TimeSeconds:F1}s fps={sample.AverageFps:F1} " +
                $"mem={sample.AllocatedMemoryMegabytes:F1}MB thermal={sample.ThermalStatus}");
        }
    }

    /// <summary>No-op sink, mainly useful for tests that don't care about output.</summary>
    public sealed class NullTelemetrySink : ITelemetrySink
    {
        public void Send(PerformanceSample sample)
        {
        }
    }
}
