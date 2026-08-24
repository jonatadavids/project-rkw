using UnityEngine;

namespace RKW.Audio
{
    /// <summary>
    /// Lightweight gameplay engine/skid audio for the kart prototype.
    /// Reuses the same synthetic-clip generator and safe volume/pitch
    /// constants already validated on-device by <see cref="AudioValidationHarness"/>
    /// (M1-T13), but is a separate, minimal component: the validation
    /// harness's debug OnGUI panel (mixer toggles, sliders) is a technical
    /// fixture and must never appear during actual gameplay. This class also
    /// does not require an AudioMixer asset — it drives AudioSource volume/
    /// pitch directly, keeping it dependency-free for the prototype.
    /// </summary>
    public sealed class KartEngineAudioController : MonoBehaviour
    {
        // Founder playtest feedback, 2026-08-19 and 2026-08-20 (round 8):
        // "o barulho do ambiente esta maior que o do motor" persisted even
        // after the first pass — reusing AudioValidationConfiguration's
        // Engine/Road volumes 1:1 made the tire/road layer's ceiling
        // (0.12) close to the engine's idle volume and not far under its
        // max, so under any grip loss the road layer could get close to or
        // louder than the engine. Round 8 also added a skid-activation
        // deadzone (see KartAudioMath.CalculateSkidIntensity) so this layer
        // now only plays during genuinely significant grip loss instead of
        // almost continuously — combined with pushing the gap between
        // these two constants further apart here, the engine should now
        // stay clearly dominant both in loudness and in how often the
        // skid layer is even active. These gameplay-only constants
        // deliberately diverge from the shared validation config (which
        // stays untouched for M1-T13).
        private const float GameplayEngineIdleVolume = 0.28f;
        private const float GameplayEngineMaxVolume = 0.48f;
        private const float GameplaySkidMaxVolume = 0.09f;

        // Founder playtest feedback, 2026-08-20 (round 8): "derrapada não
        // parece" — the skid layer used the same fixed pitch as any other
        // looping clip, so even when it played it did not read as a tire
        // screech. Letting pitch rise with skid intensity gives it a
        // distinct, escalating character instead of a flat drone.
        private const float SkidMinimumPitch = 0.9f;
        private const float SkidMaximumPitch = 1.6f;

        private AudioSource _engineSource;
        private AudioSource _skidSource;
        private AudioClip _engineClip;
        private AudioClip _skidClip;

        private float _targetEngineVolume;
        private float _targetEnginePitch = AudioValidationConfiguration.MinimumEnginePitch;
        private float _targetSkidVolume;
        private float _targetSkidPitch = SkidMinimumPitch;

        private void Awake()
        {
            _engineSource = CreateSource("Engine Audio");
            _engineClip = ProceduralAudioFactory.Create(AudioValidationLayer.Engine);
            _engineSource.clip = _engineClip;
            _engineSource.volume = 0f;
            _engineSource.pitch = AudioValidationConfiguration.MinimumEnginePitch;
            _engineSource.Play();

            _skidSource = CreateSource("Skid Audio");
            _skidClip = ProceduralAudioFactory.Create(AudioValidationLayer.Road);
            _skidSource.clip = _skidClip;
            _skidSource.volume = 0f;
            _skidSource.pitch = SkidMinimumPitch;
            _skidSource.Play();
        }

        private AudioSource CreateSource(string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(transform, false);
            var source = child.AddComponent<AudioSource>();
            source.loop = true;
            source.playOnAwake = false;
            // 2D: this is the local player/bot's own kart sound, always
            // audible at full presence regardless of camera framing — not
            // meant to be positionally attenuated like the M1-T13 harness's
            // orbiting validation sources.
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            return source;
        }

        /// <param name="speedRatio01">Current speed / max speed, clamped 0..1.</param>
        /// <param name="throttleRatio01">Current smoothed throttle input, 0..1.</param>
        /// <param name="skidIntensity01">0 = full grip, 1 = at/under minimum grip ratio.</param>
        public void SetDrivingState(float speedRatio01, float throttleRatio01, float skidIntensity01)
        {
            _targetEnginePitch = KartAudioMath.CalculateEnginePitch(
                speedRatio01, throttleRatio01,
                AudioValidationConfiguration.MinimumEnginePitch,
                AudioValidationConfiguration.MaximumEnginePitch);
            _targetEngineVolume = KartAudioMath.CalculateEngineVolume(
                throttleRatio01,
                GameplayEngineIdleVolume,
                GameplayEngineMaxVolume);
            var clampedSkidIntensity = Mathf.Clamp01(skidIntensity01);
            _targetSkidVolume = clampedSkidIntensity * GameplaySkidMaxVolume;
            _targetSkidPitch = Mathf.Lerp(SkidMinimumPitch, SkidMaximumPitch, clampedSkidIntensity);
        }

        private void Update()
        {
            var deltaTime = Time.unscaledDeltaTime;
            if (deltaTime <= 0f)
            {
                return;
            }

            _engineSource.volume = Mathf.MoveTowards(
                _engineSource.volume, _targetEngineVolume,
                AudioValidationConfiguration.EngineVolumeChangePerSecond * deltaTime);
            _engineSource.pitch = Mathf.MoveTowards(
                _engineSource.pitch, _targetEnginePitch,
                AudioValidationConfiguration.EnginePitchChangePerSecond * deltaTime);
            _skidSource.volume = Mathf.MoveTowards(
                _skidSource.volume, _targetSkidVolume,
                AudioValidationConfiguration.EngineVolumeChangePerSecond * deltaTime);
            _skidSource.pitch = Mathf.MoveTowards(
                _skidSource.pitch, _targetSkidPitch,
                AudioValidationConfiguration.EnginePitchChangePerSecond * deltaTime);
        }

        private void OnDestroy()
        {
            if (_engineSource != null)
            {
                Destroy(_engineSource.gameObject);
            }

            if (_skidSource != null)
            {
                Destroy(_skidSource.gameObject);
            }

            if (_engineClip != null)
            {
                Destroy(_engineClip);
            }

            if (_skidClip != null)
            {
                Destroy(_skidClip);
            }
        }
    }
}
