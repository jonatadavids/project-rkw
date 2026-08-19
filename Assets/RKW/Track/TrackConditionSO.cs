using UnityEngine;

namespace RKW.Track
{
    /// <summary>
    /// Requirement 18 (Track Conditions): calibration hypotheses, stored as
    /// ScriptableObjects (Requirement 18.3), for how a track condition (dry/damp/
    /// light rain/heavy rain) alters longitudinal/lateral grip, braking distance,
    /// traction, curb grip, grass grip, rubber line bonus, puddles, spray,
    /// visibility, particles, audio and haptics.
    /// MVP (Requirement 18.5): only "Dry" is implemented, with every multiplier at
    /// its 1.0 (neutral) baseline.
    /// </summary>
    [CreateAssetMenu(fileName = "NewTrackCondition", menuName = "RKW/Track/Track Condition")]
    public sealed class TrackConditionSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string conditionId = "";
        [SerializeField] private string displayName = "";

        [Header("Grip and braking")]
        [Min(0f)] [SerializeField] private float longitudinalGripMultiplier = 1f;
        [Min(0f)] [SerializeField] private float lateralGripMultiplier = 1f;
        [Min(0f)] [SerializeField] private float brakingDistanceMultiplier = 1f;
        [Min(0f)] [SerializeField] private float tractionMultiplier = 1f;
        [Min(0f)] [SerializeField] private float curbGripMultiplier = 1f;
        [Min(0f)] [SerializeField] private float grassGripMultiplier = 1f;
        [SerializeField] private float rubberLineGripBonus;

        [Header("Visual, audio and haptics")]
        [SerializeField] private bool hasPuddles;
        [SerializeField] private bool hasSpray;
        [Range(0f, 1f)] [SerializeField] private float visibilityMultiplier = 1f;
        [SerializeField] private bool hasWeatherParticles;
        [SerializeField] private string ambientAudioProfileId = "";
        [SerializeField] private bool hasHapticFeedback;

        public string ConditionId => conditionId;
        public string DisplayName => displayName;
        public float LongitudinalGripMultiplier => longitudinalGripMultiplier;
        public float LateralGripMultiplier => lateralGripMultiplier;
        public float BrakingDistanceMultiplier => brakingDistanceMultiplier;
        public float TractionMultiplier => tractionMultiplier;
        public float CurbGripMultiplier => curbGripMultiplier;
        public float GrassGripMultiplier => grassGripMultiplier;
        public float RubberLineGripBonus => rubberLineGripBonus;
        public bool HasPuddles => hasPuddles;
        public bool HasSpray => hasSpray;
        public float VisibilityMultiplier => visibilityMultiplier;
        public bool HasWeatherParticles => hasWeatherParticles;
        public string AmbientAudioProfileId => ambientAudioProfileId;
        public bool HasHapticFeedback => hasHapticFeedback;

        public bool IsValid(out string reason)
        {
            if (string.IsNullOrWhiteSpace(conditionId))
            {
                reason = "Condition ID is required.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                reason = "Display name is required.";
                return false;
            }

            if (visibilityMultiplier < 0f || visibilityMultiplier > 1f)
            {
                reason = "Visibility multiplier must be in [0, 1].";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        /// <summary>Requirement 18.5 (MVP): "Dry" must be a true neutral baseline —
        /// every grip/traction/braking multiplier at 1.0, no puddles/spray/particles.
        /// Used directly by TrackConditionSOTests, and safe to call from any other
        /// system that needs to assert a condition is a no-op.</summary>
        public bool IsNeutralBaseline()
        {
            return Mathf.Approximately(longitudinalGripMultiplier, 1f)
                && Mathf.Approximately(lateralGripMultiplier, 1f)
                && Mathf.Approximately(brakingDistanceMultiplier, 1f)
                && Mathf.Approximately(tractionMultiplier, 1f)
                && Mathf.Approximately(curbGripMultiplier, 1f)
                && Mathf.Approximately(grassGripMultiplier, 1f)
                && Mathf.Approximately(rubberLineGripBonus, 0f)
                && !hasPuddles
                && !hasSpray
                && Mathf.Approximately(visibilityMultiplier, 1f)
                && !hasWeatherParticles;
        }
    }
}
