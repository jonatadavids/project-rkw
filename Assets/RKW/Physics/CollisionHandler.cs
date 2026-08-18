using UnityEngine;

namespace RKW.Physics
{
    /// <summary>
    /// Handles collision events for a kart. Applies continuous speed loss
    /// proportional to relative impact velocity (severity). Never triggers
    /// automatic recovery — recovery is handled by a separate system only
    /// when the kart is stuck/inverted/out-of-bounds.
    /// </summary>
    [RequireComponent(typeof(KartDynamics), typeof(Rigidbody))]
    public sealed class CollisionHandler : MonoBehaviour
    {
        [Header("Collision Parameters")]
        [Min(0f)] [SerializeField] private float speedLossPerSeverity = 0.6f;
        [Min(0f)] [SerializeField] private float minimumImpactSpeed = 0.5f;

        private Rigidbody _body;
        private KartDynamics _dynamics;

        /// <summary>Last collision severity (0 = no collision, scale is continuous).</summary>
        public float LastCollisionSeverity { get; private set; }

        /// <summary>Total collision events this session.</summary>
        public int CollisionCount { get; private set; }

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _dynamics = GetComponent<KartDynamics>();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.relativeVelocity.magnitude < minimumImpactSpeed)
            {
                return;
            }

            var severity = CalculateSeverity(collision);
            LastCollisionSeverity = severity;
            CollisionCount++;

            // Apply proportional speed loss
            var speedLoss = severity * speedLossPerSeverity;
            var currentVelocity = _body.linearVelocity;
            var currentSpeed = currentVelocity.magnitude;

            if (currentSpeed > 0.1f)
            {
                var reducedSpeed = Mathf.Max(0f, currentSpeed - speedLoss);
                _body.linearVelocity = currentVelocity.normalized * reducedSpeed;
            }

            // NOTE: No recovery triggered here. Recovery is ONLY for
            // stuck/inverted/out-of-bounds conditions (separate system).
        }

        /// <summary>
        /// Calculates continuous severity from collision data.
        /// severity = f(relative_velocity, angle, mass_ratio)
        /// Scale: 0 = negligible, ~1 = moderate, >2 = severe
        /// </summary>
        public static float CalculateSeverity(Collision collision)
        {
            var relativeSpeed = collision.relativeVelocity.magnitude;
            var contact = collision.GetContact(0);
            var impactAngle = Vector3.Angle(collision.relativeVelocity.normalized, contact.normal);
            var angleFactor = Mathf.Lerp(0.5f, 1f, impactAngle / 90f);

            return relativeSpeed * angleFactor * 0.1f; // scale to ~0-3 range for kart speeds
        }

        /// <summary>
        /// Pure calculation for testing: given relative speed and angle,
        /// returns severity value.
        /// </summary>
        public static float CalculateSeverityFromParameters(
            float relativeSpeedMps, float impactAngleDegrees)
        {
            var angleFactor = Mathf.Lerp(0.5f, 1f, Mathf.Clamp(impactAngleDegrees, 0f, 90f) / 90f);
            return relativeSpeedMps * angleFactor * 0.1f;
        }
    }
}
