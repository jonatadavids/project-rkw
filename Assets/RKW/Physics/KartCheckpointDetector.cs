using UnityEngine;

namespace RKW.Physics
{
    /// <summary>
    /// Placed on the kart. Detects when passing through checkpoint triggers
    /// and reports to TimingManagerLite.
    /// </summary>
    [RequireComponent(typeof(KartDynamics))]
    public sealed class KartCheckpointDetector : MonoBehaviour
    {
        private TimingManagerLite _timing;

        public void Configure(TimingManagerLite timing)
        {
            _timing = timing;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_timing == null)
            {
                return;
            }

            var checkpoint = other.GetComponent<CheckpointTrigger>();
            if (checkpoint == null)
            {
                return;
            }

            _timing.RegisterCheckpointHit(checkpoint.CheckpointIndex, checkpoint.IsStartFinishLine);
        }
    }
}
