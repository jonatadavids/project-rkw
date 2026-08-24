using UnityEngine;

namespace RKW.Audio
{
    /// <summary>
    /// Pure logic for mapping kart driving state (speed, throttle, grip) to
    /// engine/skid audio parameters. Deliberately has no dependency on
    /// AudioSource/AudioClip so it is EditMode testable without the audio
    /// device or an AudioListener.
    /// </summary>
    public static class KartAudioMath
    {
        /// <summary>
        /// Engine pitch rises with both road speed and throttle input, so
        /// the engine still "revs" when accelerating from a stop (high
        /// throttle, low speed) instead of staying silent until the kart is
        /// already moving.
        /// </summary>
        public static float CalculateEnginePitch(float speedRatio01, float throttleRatio01, float minPitch, float maxPitch)
        {
            var blended = Mathf.Clamp01(Mathf.Max(Mathf.Clamp01(speedRatio01), Mathf.Clamp01(throttleRatio01) * 0.6f));
            return Mathf.Lerp(minPitch, maxPitch, blended);
        }

        /// <summary>Engine volume tracks throttle: idle hum at rest, louder under load.</summary>
        public static float CalculateEngineVolume(float throttleRatio01, float idleVolume, float maxVolume)
        {
            return Mathf.Lerp(idleVolume, maxVolume, Mathf.Clamp01(throttleRatio01));
        }

        // Founder playtest feedback, 2026-08-20 (round 8): "o som ambiente
        // meio que ofusca o barulho do motor... derrapada não parece" —
        // this used to start ramping from the very first hint of grip
        // loss, which happens during completely ordinary cornering (the
        // grip curve starts falling past a fairly small slip angle), so
        // the "skid" layer was audible almost continuously and read as a
        // constant background drone rather than an occasional "you're
        // sliding" cue. It now stays silent for the first
        // SkidActivationThreshold01 share of the grip-loss range and only
        // ramps up for genuinely significant loss.
        private const float SkidActivationThreshold01 = 0.35f;

        /// <summary>
        /// Skid/tire-scrub intensity: silent below a minimum speed (no
        /// audible scrub while nearly stopped) or below
        /// <see cref="SkidActivationThreshold01"/> of the grip-loss range
        /// (ordinary cornering shouldn't sound like a skid), then scales up
        /// toward the tuning's minimum grip ratio.
        /// </summary>
        public static float CalculateSkidIntensity(
            float gripRatio, float minimumGripRatio, float speedKph, float speedThresholdKph)
        {
            if (speedKph < speedThresholdKph)
            {
                return 0f;
            }

            var range = Mathf.Max(0.0001f, 1f - Mathf.Clamp01(minimumGripRatio));
            var loss = 1f - Mathf.Clamp01(gripRatio);
            var loss01 = Mathf.Clamp01(loss / range);
            return Mathf.InverseLerp(SkidActivationThreshold01, 1f, loss01);
        }
    }
}
