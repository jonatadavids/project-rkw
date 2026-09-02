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
        // Etapa 5 (2026-08-31): scales KartCategorySO.CoastingDeceleration
        // (the kart's baseline rolling resistance) per surface. 1.0 = same
        // as before Etapa 5 for every asset/procedural surface -- NOT set
        // by Configure() below, so existing callers (grass, curbs, etc.)
        // keep this at the safe default until a designer deliberately
        // tunes a specific surface's .asset Inspector value.
        [Min(0f)] [SerializeField] private float rollingResistanceMultiplier = 1f;

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
        /// Etapa 5 -- multiplies KartCategorySO.CoastingDeceleration (the
        /// engine-off rolling resistance baseline) while on this surface.
        /// 1.0 = asphalt-like baseline, unchanged from every asset/
        /// procedural surface predating Etapa 5. Grass/gravel should
        /// realistically be HIGHER than 1.0 (rolling resistance -- not
        /// grip -- is usually worse on soft/loose surfaces), left at the
        /// safe default here and not yet tuned per surface; see the Etapa
        /// 5 final report for this as a tracked follow-up.
        /// </summary>
        public float RollingResistanceMultiplier => rollingResistanceMultiplier;

        /// <summary>
        /// Founder playtest feedback, 2026-08-20 (round 8): the fields
        /// above are normally set via the Inspector on a hand-authored
        /// .asset, but KartPhysicsPrototypeBootstrap builds surfaces like
        /// grass procedurally at runtime and had no way to fill them in —
        /// so grass silently did nothing in play. This is that runtime
        /// setter.
        /// </summary>
        public void Configure(
            string id, string display, float grip, float instability, bool offTrack,
            float rollingResistance = 1f)
        {
            // Etapa 5 (2026-08-31): rollingResistance is an OPTIONAL trailing
            // parameter defaulting to 1f (no change), so every existing call
            // site (KartPhysicsPrototypeBootstrap's grass/curb creation)
            // keeps compiling and behaving exactly as before without being
            // touched. New/updated callers can now also configure this at
            // runtime instead of only via the Inspector.
            surfaceId = id;
            displayName = display;
            gripMultiplier = Mathf.Clamp(grip, 0f, 1.5f);
            instabilityFactor = Mathf.Clamp01(instability);
            isOffTrack = offTrack;
            rollingResistanceMultiplier = Mathf.Max(0f, rollingResistance);
        }
    }
}
