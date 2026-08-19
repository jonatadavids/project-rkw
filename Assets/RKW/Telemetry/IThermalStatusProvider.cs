using UnityEngine;

namespace RKW.Telemetry
{
    /// <summary>
    /// M3-T07: abstraction over the platform thermal API (Requirement R12.4 —
    /// "Integrar Thermal Status API quando disponível"). An interface so the
    /// collector is testable without a real device.
    /// </summary>
    public interface IThermalStatusProvider
    {
        ThermalStatus GetThermalStatus();
    }

    /// <summary>
    /// Fallback for platforms/editor without a thermal API: always reports
    /// <see cref="ThermalStatus.Unknown"/> rather than guessing.
    /// </summary>
    public sealed class UnsupportedThermalStatusProvider : IThermalStatusProvider
    {
        public ThermalStatus GetThermalStatus()
        {
            return ThermalStatus.Unknown;
        }
    }

    /// <summary>
    /// Wraps Android's PowerManager.getCurrentThermalStatus() (API 29+,
    /// added in Android 10 / Q). Older devices, or any unexpected JNI
    /// failure, degrade to <see cref="ThermalStatus.Unknown"/> instead of
    /// throwing — thermal telemetry is best-effort and must never crash the
    /// game. The actual JNI calls only compile into Android player builds
    /// (guarded below); everywhere else, including the Editor — which is
    /// what runs EditMode tests — this falls back to Unknown so the class
    /// still exists and <see cref="MapAndroidThermalStatus"/> stays
    /// unit-testable regardless of build target.
    /// </summary>
    public sealed class AndroidThermalStatusProvider : IThermalStatusProvider
    {
        /// <summary>
        /// Maps android.os.PowerManager.THERMAL_STATUS_* constants (0=NONE,
        /// 1=LIGHT, 2=MODERATE, 3=SEVERE, 4=CRITICAL, 5=EMERGENCY,
        /// 6=SHUTDOWN) onto our 5-category enum. EMERGENCY/SHUTDOWN both
        /// collapse into Critical — by that point the OS is about to force a
        /// shutdown and the distinction is not actionable in-game. Kept as a
        /// pure, unconditionally-compiled function so it is EditMode
        /// testable even though the JNI calls that feed it are Android-only.
        /// </summary>
        public static ThermalStatus MapAndroidThermalStatus(int rawStatus)
        {
            switch (rawStatus)
            {
                case 0: return ThermalStatus.Nominal;
                case 1: return ThermalStatus.Light;
                case 2: return ThermalStatus.Moderate;
                case 3: return ThermalStatus.Severe;
                case 4:
                case 5:
                case 6:
                    return ThermalStatus.Critical;
                default:
                    return ThermalStatus.Unknown;
            }
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaObject _powerManager;
        private bool _unavailable;

        public ThermalStatus GetThermalStatus()
        {
            if (_unavailable)
            {
                return ThermalStatus.Unknown;
            }

            try
            {
                if (_powerManager == null)
                {
                    using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                    using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                    _powerManager = activity.Call<AndroidJavaObject>("getSystemService", "power");
                }

                var rawStatus = _powerManager.Call<int>("getCurrentThermalStatus");
                return MapAndroidThermalStatus(rawStatus);
            }
            catch (System.Exception exception)
            {
                // getCurrentThermalStatus is API 29+; older devices (or any
                // other JNI hiccup) land here. Stop retrying for the rest of
                // this session rather than paying the JNI exception cost on
                // every sample.
                _unavailable = true;
                Debug.LogWarning("AndroidThermalStatusProvider: thermal status unavailable, " +
                    $"falling back to Unknown. Reason: {exception.Message}");
                return ThermalStatus.Unknown;
            }
        }
#else
        public ThermalStatus GetThermalStatus()
        {
            return ThermalStatus.Unknown;
        }
#endif
    }
}
