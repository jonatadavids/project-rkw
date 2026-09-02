using UnityEngine;

namespace RKW.Physics
{
    /// <summary>
    /// Etapa 8 (2026-08-31) driving assists: OFF / STANDARD / BEGINNER.
    /// Sits between raw driver input (KartPrototypeInput) and
    /// KartDynamics.SetInput, reshaping input through the pure functions in
    /// KartAssistMath -- it never reads or writes any KartDynamics tuning
    /// value, only its already-public, read-only runtime state (speed,
    /// rear slip angle, rear combined grip usage, brake lock ratio), so an
    /// assist can only ever soften/smooth/redirect what the player already
    /// asked for, never grant more grip or power than that same raw input
    /// would already produce with assists Off.
    ///
    /// NOT used by <see cref="KartBotController"/> -- bots always call
    /// KartDynamics.SetInput directly (see that class), so every bot always
    /// races with identical, fully unassisted physics regardless of what
    /// assist level any human player has selected. This is the Etapa 8
    /// spec's explicit "bots devem usar física idêntica aos jogadores, sem
    /// vantagens ocultas de IA" requirement.
    ///
    /// A kart with no KartAssistController attached at all behaves exactly
    /// as before Etapa 8 (KartPrototypeInput falls back to calling
    /// KartDynamics.SetInput directly when it can't find one) -- adding
    /// this component is what opts a kart into the assists layer, not a
    /// tuning value.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(KartDynamics))]
    public sealed class KartAssistController : MonoBehaviour
    {
        public enum Level
        {
            Off = 0,
            Standard = 1,
            Beginner = 2,
        }

        [SerializeField] private Level assistLevel = Level.Off;

        // Etapa 8: CounterSteerAssist's catch-direction sign is reasoned
        // through the codebase's existing slip-angle/steering conventions
        // (see KartAssistMath.ApplyCounterSteerAssist's doc comment for the
        // full reasoning) but has NOT been visually confirmed against a
        // real spinning kart in Unity -- a wrong sign here would make a
        // beginner's spin WORSE, which would violate the whole point of an
        // assist. Left OFF even at Beginner level until that confirmation
        // happens; a founder/tester can flip this on in the Inspector (or
        // this default can change) once verified. See the Etapa 8 report's
        // "UNITY VALIDATION PENDING" section.
        [SerializeField] private bool enableCounterSteerAssistUnverified;

        // Standard is intentionally much gentler than Beginner on every
        // shared assist -- it should feel close to unassisted, just
        // slightly less twitchy, while Beginner is a real safety net.
        private const float SteeringAssistMaxReductionAtTopSpeedStandard = 0.12f;
        private const float SteeringAssistMaxReductionAtTopSpeedBeginner = 0.35f;
        private const float StabilityAssistMaxRatePerSecondStandard = 7f;
        private const float StabilityAssistMaxRatePerSecondBeginner = 4f;
        private const float ThrottleAssistEaseStartUsage = 0.75f;
        private const float CounterSteerSevereSlipThresholdDegrees = 20f;
        private const float CounterSteerMaxAssistSteering = 0.25f;

        private KartDynamics _dynamics;
        private float _smoothedSteeringForStability;

        public Level AssistLevel
        {
            get => assistLevel;
            set => assistLevel = value;
        }

        private void Awake()
        {
            _dynamics = GetComponent<KartDynamics>();
        }

        /// <summary>
        /// Reshapes raw -1..1/0..1 driver input per the current assist
        /// level, then forwards the result to KartDynamics.SetInput. Call
        /// this INSTEAD OF calling KartDynamics.SetInput directly (see
        /// KartPrototypeInput.Update).
        /// </summary>
        public void ApplyInput(float rawSteering, float rawThrottle, float rawBrake, float deltaTime)
        {
            if (_dynamics == null)
            {
                return;
            }

            if (assistLevel == Level.Off || _dynamics.Tuning == null)
            {
                _smoothedSteeringForStability = rawSteering;
                _dynamics.SetInput(rawSteering, rawThrottle, rawBrake);
                return;
            }

            var speedRatio = Mathf.Clamp01(_dynamics.SpeedKph / Mathf.Max(1f, _dynamics.Tuning.MaxSpeedKph));
            var isBeginner = assistLevel == Level.Beginner;
            var steeringReduction = isBeginner
                ? SteeringAssistMaxReductionAtTopSpeedBeginner
                : SteeringAssistMaxReductionAtTopSpeedStandard;
            var stabilityRate = isBeginner
                ? StabilityAssistMaxRatePerSecondBeginner
                : StabilityAssistMaxRatePerSecondStandard;

            var steering = KartAssistMath.ApplySteeringAssist(rawSteering, speedRatio, steeringReduction);

            _smoothedSteeringForStability = KartAssistMath.ApplyStabilityAssistSmoothing(
                _smoothedSteeringForStability, steering, stabilityRate, deltaTime);
            steering = _smoothedSteeringForStability;

            if (enableCounterSteerAssistUnverified && isBeginner)
            {
                steering = KartAssistMath.ApplyCounterSteerAssist(
                    steering, _dynamics.RearSlipAngleDegrees,
                    CounterSteerSevereSlipThresholdDegrees, CounterSteerMaxAssistSteering);
            }

            // BrakeAssist runs at both Standard and Beginner (a gentle
            // anti-lock ease-off is safe/uncontroversial at any level);
            // ThrottleAssist is Beginner-only, per the spec's own
            // progressive OFF/STANDARD/BEGINNER framing.
            var brake = KartAssistMath.ApplyBrakeAssist(rawBrake, _dynamics.BrakeLockRatio);
            var throttle = isBeginner
                ? KartAssistMath.ApplyThrottleAssist(rawThrottle, _dynamics.RearCombinedGripUsage, ThrottleAssistEaseStartUsage)
                : rawThrottle;

            _dynamics.SetInput(steering, throttle, brake);
        }
    }
}
