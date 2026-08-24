using UnityEngine;

namespace RKW.Physics
{
    [CreateAssetMenu(fileName = "NewSurface", menuName = "RKW/Physics/Surface Data")]
    public sealed class SurfaceDataSO : ScriptableObject
    {
        [SerializeField] private string surfaceId = "asphalt_dry";
        [SerializeField] private string displayName = "Asphalt (Dry)";
        [Range(0f, 1.5f)] [SerializeField] private float gripMultiplier = 1f;
        [Range(0f, 1f)] [SerializeField] private float instabilityFactor;
        [SerializeField] private bool isOffTrack;

        public string SurfaceId => surfaceId;
        public string DisplayName => displayName;

        /// <summary>
        /// Multiplier applied to lateral and longitudinal grip.
        /// 1.0 = dry asphalt baseline. Grass/dirt should be ≤ 0.6 (≥40% reduction).
        /// </summary>
        public float GripMultiplier => gripMultiplier;

        /// <summary>
        /// Additional instability (0 = stable, 1 = maximum destabilization).
        /// Used for curbs/zebras proportional to speed and angle.
        /// </summary>
        public float InstabilityFactor => instabilityFactor;

        /// <summary>Whether this surface counts as off-track for lap validation.</summary>
        public bool IsOffTrack => isOffTrack;

        /// <summary>
        /// Founder playtest feedback, 2026-08-20 (round 8): the fields
        /// above are normally set via the Inspector on a hand-authored
        /// .asset, but KartPhysicsPrototypeBootstrap builds surfaces like
        /// grass procedurally at runtime and had no way to fill them in —
        /// so grass silently did nothing in play. This is that runtime
        /// setter.
        /// </summary>
        public void Configure(string id, string display, float grip, float instability, bool offTrack)
        {
            surfaceId = id;
            displayName = display;
            gripMultiplier = Mathf.Clamp(grip, 0f, 1.5f);
            instabilityFactor = Mathf.Clamp01(instability);
            isOffTrack = offTrack;
        }
    }
}
