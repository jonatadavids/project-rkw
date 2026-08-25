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
        [SerializeField] private Vector3 requiredCrossingDirection;

        public int CheckpointIndex => checkpointIndex;
        public bool IsStartFinishLine => isStartFinishLine;

        public void Configure(int index, bool startFinish, Vector3 crossingDirection)
        {
            checkpointIndex = index;
            isStartFinishLine = startFinish;
            crossingDirection.y = 0f;
            requiredCrossingDirection = crossingDirection.sqrMagnitude > 0.0001f
                ? crossingDirection.normalized
                : Vector3.zero;
        }

        public bool IsCrossingForward(Vector3 velocity, Vector3 fallbackForward)
        {
            if (!isStartFinishLine || requiredCrossingDirection.sqrMagnitude <= 0.0001f)
            {
                return true;
            }

            velocity.y = 0f;
            fallbackForward.y = 0f;
            var movementDirection = velocity.sqrMagnitude > 0.0001f ? velocity.normalized : fallbackForward.normalized;
            return movementDirection.sqrMagnitude > 0.0001f
                && Vector3.Dot(movementDirection, requiredCrossingDirection) > 0f;
        }

        private void Reset()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;
        }
    }
}
