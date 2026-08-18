using UnityEngine;

namespace RKW.Physics
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class KartPrototypeCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 localOffset = new(0f, 4.6f, -7.2f);
        [SerializeField] private float positionSharpness = 7f;
        [SerializeField] private float rotationSharpness = 9f;

        public void Configure(Transform followTarget)
        {
            target = followTarget;
            Snap();
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            var desiredPosition = target.TransformPoint(localOffset);
            transform.position = Vector3.Lerp(transform.position, desiredPosition,
                1f - Mathf.Exp(-positionSharpness * Time.deltaTime));
            var lookTarget = target.position + target.forward * 3f + Vector3.up * 0.8f;
            var desiredRotation = Quaternion.LookRotation(lookTarget - transform.position, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation,
                1f - Mathf.Exp(-rotationSharpness * Time.deltaTime));
        }

        private void Snap()
        {
            if (target == null)
            {
                return;
            }

            transform.position = target.TransformPoint(localOffset);
            transform.LookAt(target.position + target.forward * 3f + Vector3.up * 0.8f);
        }
    }
}
