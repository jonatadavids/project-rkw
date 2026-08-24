using UnityEngine;

namespace RKW.Physics
{
    /// <summary>
    /// Placed on trigger colliders representing different track surfaces.
    /// When a kart enters, it reports the surface to KartDynamics.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class SurfaceTrigger : MonoBehaviour
    {
        [SerializeField] private SurfaceDataSO surfaceData;

        public SurfaceDataSO SurfaceData => surfaceData;

        /// <summary>Founder playtest feedback, 2026-08-20 (round 8): lets a procedurally-created trigger (see KartPhysicsPrototypeBootstrap.CreateSurface) assign its data at runtime instead of only via the Inspector.</summary>
        public void Configure(SurfaceDataSO data)
        {
            surfaceData = data;
        }

        private void Reset()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;
        }
    }
}
