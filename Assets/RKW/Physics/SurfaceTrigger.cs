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

        private void Reset()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;
        }
    }
}
