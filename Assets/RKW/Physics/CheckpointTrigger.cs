using UnityEngine;

namespace RKW.Physics
{
    /// <summary>
    /// Placed on trigger colliders at checkpoint positions on the track.
    /// Used by TimingManagerLite to detect lap progress and validate laps.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class CheckpointTrigger : MonoBehaviour
    {
        [SerializeField] private int checkpointIndex;
        [SerializeField] private bool isStartFinishLine;

        public int CheckpointIndex => checkpointIndex;
        public bool IsStartFinishLine => isStartFinishLine;

        public void Configure(int index, bool startFinish)
        {
            checkpointIndex = index;
            isStartFinishLine = startFinish;
        }

        private void Reset()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;
        }
    }
}
