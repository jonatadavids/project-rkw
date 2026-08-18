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
    }
}
