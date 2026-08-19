using System;
using System.Collections.Generic;
using UnityEngine;

namespace RKW.Track
{
    /// <summary>Per-quality-tier performance ceiling for this preset (Requirement 17.2:
    /// "orçamento de performance por tier"). Tier names match QualityManager's profile
    /// names (Low/Medium/High) by convention, kept as a string here to avoid a runtime
    /// dependency from RKW.Track onto RKW.Core.</summary>
    [Serializable]
    public struct PerformanceBudget
    {
        [SerializeField] private string qualityTier;
        [SerializeField] private int maxDrawCalls;
        [SerializeField] private int targetFps;

        public PerformanceBudget(string qualityTier, int maxDrawCalls, int targetFps)
        {
            this.qualityTier = qualityTier;
            this.maxDrawCalls = maxDrawCalls;
            this.targetFps = targetFps;
        }

        public string QualityTier => qualityTier;
        public int MaxDrawCalls => maxDrawCalls;
        public int TargetFps => targetFps;
    }

    /// <summary>
    /// Requirement 17 (Environment Presets): lighting/skybox/reflections/exposure/
    /// shadows/spotlights/post-processing/crowd/visibility/ambient audio/performance
    /// budget for one environment (morning/late-afternoon/night).
    /// MVP (Requirement 17.6): only the "Day" preset exists.
    /// </summary>
    [CreateAssetMenu(fileName = "NewEnvironmentPreset", menuName = "RKW/Track/Environment Preset")]
    public sealed class EnvironmentPresetSO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string presetId = "";
        [SerializeField] private string displayName = "";

        [Header("Lighting")]
        [SerializeField] private bool usesBakedLighting = true;
        [SerializeField] private Color directionalLightColor = Color.white;
        [Min(0f)] [SerializeField] private float directionalLightIntensity = 1.1f;
        [SerializeField] private Vector3 directionalLightRotationEuler = new Vector3(45f, -35f, 0f);
        [SerializeField] private bool hasSpotlights;

        [Header("Sky and reflections")]
        [SerializeField] private Color ambientSkyColor = new Color(0.5f, 0.6f, 0.7f);
        [Range(0f, 2f)] [SerializeField] private float reflectionIntensity = 1f;
        [Range(-4f, 4f)] [SerializeField] private float exposureCompensation;

        [Header("Shadows and post-processing")]
        [SerializeField] private bool shadowsEnabled = true;
        [SerializeField] private string postProcessingProfileId = "";

        [Header("Atmosphere")]
        [Range(0f, 1f)] [SerializeField] private float crowdDensity;
        [Min(1f)] [SerializeField] private float visibilityDistanceMeters = 500f;
        [SerializeField] private AudioClip ambientAudioClip;

        [Header("Performance")]
        [SerializeField] private PerformanceBudget[] performanceBudgets = Array.Empty<PerformanceBudget>();

        public string PresetId => presetId;
        public string DisplayName => displayName;
        public bool UsesBakedLighting => usesBakedLighting;
        public Color DirectionalLightColor => directionalLightColor;
        public float DirectionalLightIntensity => directionalLightIntensity;
        public Vector3 DirectionalLightRotationEuler => directionalLightRotationEuler;

        /// <summary>Requirement 17.4: rental karts have no headlights, so night racing
        /// depends entirely on venue lighting — this flag exists so a future "Night"
        /// preset can turn on venue spotlights explicitly rather than relying on
        /// player-facing light sources.</summary>
        public bool HasSpotlights => hasSpotlights;

        public Color AmbientSkyColor => ambientSkyColor;
        public float ReflectionIntensity => reflectionIntensity;
        public float ExposureCompensation => exposureCompensation;
        public bool ShadowsEnabled => shadowsEnabled;
        public string PostProcessingProfileId => postProcessingProfileId;
        public float CrowdDensity => crowdDensity;
        public float VisibilityDistanceMeters => visibilityDistanceMeters;
        public AudioClip AmbientAudioClip => ambientAudioClip;
        public IReadOnlyList<PerformanceBudget> PerformanceBudgets => performanceBudgets;

        public bool IsValid(out string reason)
        {
            if (string.IsNullOrWhiteSpace(presetId))
            {
                reason = "Preset ID is required.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                reason = "Display name is required.";
                return false;
            }

            if (directionalLightIntensity < 0f)
            {
                reason = "Directional light intensity cannot be negative.";
                return false;
            }

            if (visibilityDistanceMeters <= 0f)
            {
                reason = "Visibility distance must be positive.";
                return false;
            }

            if (performanceBudgets == null || performanceBudgets.Length == 0)
            {
                reason = "At least one performance budget (per quality tier) is required.";
                return false;
            }

            reason = string.Empty;
            return true;
        }
    }
}
