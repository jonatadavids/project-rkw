using RKW.Audio;
using UnityEngine;

namespace RKW.Physics
{
    /// <summary>
    /// Glue between <see cref="KartDynamics"/> (speed/grip/throttle state)
    /// and <see cref="RKW.Audio.KartEngineAudioController"/> (engine/skid
    /// sound). Lives in RKW.Physics rather than RKW.Audio so RKW.Audio stays
    /// decoupled from kart physics — it only receives plain floats.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(KartDynamics))]
    public sealed class KartAudioBridge : MonoBehaviour
    {
        private const float SkidAudibleSpeedThresholdKph = 8f;

        private KartDynamics _dynamics;
        private KartEngineAudioController _audio;

        private void Awake()
        {
            _dynamics = GetComponent<KartDynamics>();
            var audioObject = new GameObject("Kart Audio");
            audioObject.transform.SetParent(transform, false);
            _audio = audioObject.AddComponent<KartEngineAudioController>();
        }

        private void Update()
        {
            if (_dynamics == null || _dynamics.Tuning == null || _audio == null)
            {
                return;
            }

            var maxSpeedKph = Mathf.Max(1f, _dynamics.Tuning.MaxSpeedKph);
            var speedRatio = Mathf.Clamp01(_dynamics.SpeedKph / maxSpeedKph);
            var throttleRatio = Mathf.Clamp01(_dynamics.NormalizedThrottle);
            var skidIntensity = KartAudioMath.CalculateSkidIntensity(
                _dynamics.GripRatio, _dynamics.Tuning.MinimumGripRatio,
                _dynamics.SpeedKph, SkidAudibleSpeedThresholdKph);

            _audio.SetDrivingState(speedRatio, throttleRatio, skidIntensity);
        }
    }
}
