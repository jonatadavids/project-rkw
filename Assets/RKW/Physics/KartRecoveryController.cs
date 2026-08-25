using System.Collections.Generic;
using UnityEngine;

namespace RKW.Physics
{
    public enum KartRecoveryReason
    {
        None = 0,
        Stuck = 1,
        Inverted = 2,
        OutsideRecoverablePerimeter = 3,
        SafetyRisk = 4,
    }

    /// <summary>Pure recovery predicates and track-point selection.</summary>
    public static class KartRecoveryMath
    {
        public static float UpdateStuckDuration(float currentDuration, bool monitoringEnabled,
            float horizontalSpeedMetersPerSecond, float deltaTime, float stoppedSpeedThreshold)
        {
            if (!monitoringEnabled || horizontalSpeedMetersPerSecond > stoppedSpeedThreshold)
            {
                return 0f;
            }

            return Mathf.Max(0f, currentDuration) + Mathf.Max(0f, deltaTime);
        }

        public static KartRecoveryReason EvaluateReason(float stuckDurationSeconds,
            float tiltDegrees, bool outsideRecoverablePerimeter, bool safetyRisk,
            float stuckThresholdSeconds = 4f, float invertedThresholdDegrees = 85f)
        {
            if (safetyRisk)
            {
                return KartRecoveryReason.SafetyRisk;
            }

            if (outsideRecoverablePerimeter)
            {
                return KartRecoveryReason.OutsideRecoverablePerimeter;
            }

            if (tiltDegrees > invertedThresholdDegrees)
            {
                return KartRecoveryReason.Inverted;
            }

            return stuckDurationSeconds > stuckThresholdSeconds
                ? KartRecoveryReason.Stuck
                : KartRecoveryReason.None;
        }

        public static Vector3 FindNearestPoint(IReadOnlyList<Vector3> points, Vector3 position)
        {
            if (points == null || points.Count == 0)
            {
                return position;
            }

            var nearest = points[0];
            var nearestDistanceSquared = HorizontalDistanceSquared(position, nearest);
            for (var i = 1; i < points.Count; i++)
            {
                var distanceSquared = HorizontalDistanceSquared(position, points[i]);
                if (distanceSquared < nearestDistanceSquared)
                {
                    nearest = points[i];
                    nearestDistanceSquared = distanceSquared;
                }
            }

            return nearest;
        }

        public static bool IsOutsideRecoverablePerimeter(Vector3 position,
            IReadOnlyList<Vector3> racingLine, float trackWidthMeters, float perimeterMultiplier = 3f)
        {
            if (racingLine == null || racingLine.Count < 2)
            {
                return false;
            }

            var allowedDistance = Mathf.Max(10f, trackWidthMeters * perimeterMultiplier);
            var allowedDistanceSquared = allowedDistance * allowedDistance;
            for (var i = 0; i < racingLine.Count; i++)
            {
                var start = racingLine[i];
                var end = racingLine[(i + 1) % racingLine.Count];
                if (HorizontalDistanceToSegmentSquared(position, start, end) <= allowedDistanceSquared)
                {
                    return false;
                }
            }

            return true;
        }

        public static Vector3 FindTrackForward(IReadOnlyList<Vector3> racingLine, Vector3 position)
        {
            if (racingLine == null || racingLine.Count < 2)
            {
                return Vector3.forward;
            }

            var nearestDistanceSquared = float.MaxValue;
            var nearestForward = Vector3.forward;
            for (var i = 0; i < racingLine.Count; i++)
            {
                var start = racingLine[i];
                var end = racingLine[(i + 1) % racingLine.Count];
                var distanceSquared = HorizontalDistanceToSegmentSquared(position, start, end);
                if (distanceSquared >= nearestDistanceSquared)
                {
                    continue;
                }

                var forward = end - start;
                forward.y = 0f;
                if (forward.sqrMagnitude > 0.0001f)
                {
                    nearestDistanceSquared = distanceSquared;
                    nearestForward = forward.normalized;
                }
            }

            return nearestForward;
        }

        private static float HorizontalDistanceSquared(Vector3 first, Vector3 second)
        {
            var delta = first - second;
            return delta.x * delta.x + delta.z * delta.z;
        }

        private static float HorizontalDistanceToSegmentSquared(Vector3 position, Vector3 start, Vector3 end)
        {
            var segmentX = end.x - start.x;
            var segmentZ = end.z - start.z;
            var segmentLengthSquared = segmentX * segmentX + segmentZ * segmentZ;
            if (segmentLengthSquared <= 0.0001f)
            {
                return HorizontalDistanceSquared(position, start);
            }

            var offsetX = position.x - start.x;
            var offsetZ = position.z - start.z;
            var t = Mathf.Clamp01((offsetX * segmentX + offsetZ * segmentZ) / segmentLengthSquared);
            var nearestX = start.x + segmentX * t;
            var nearestZ = start.z + segmentZ * t;
            var deltaX = position.x - nearestX;
            var deltaZ = position.z - nearestZ;
            return deltaX * deltaX + deltaZ * deltaZ;
        }
    }

    /// <summary>
    /// Player-only safe recovery. It monitors only after race input is enabled,
    /// never reacts to collision events, and restores the kart at the nearest
    /// configured recovery point aligned with the racing line.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class KartRecoveryController : MonoBehaviour
    {
        private readonly List<Vector3> _recoveryPoints = new List<Vector3>();
        private readonly List<Vector3> _racingLine = new List<Vector3>();
        private readonly List<IgnoredColliderPair> _ignoredColliderPairs = new List<IgnoredColliderPair>();

        private Rigidbody _body;
        private KartPrototypeInput _input;
        private float _trackWidthMeters = 7f;
        private float _stuckThresholdSeconds = 4f;
        private float _stoppedSpeedMetersPerSecond = 0.2f;
        private float _invertedThresholdDegrees = 85f;
        private float _safetyHeightMeters = 2f;
        private float _collisionGraceSeconds = 3f;
        private float _perimeterMultiplier = 3f;
        private float _stuckDuration;
        private float _collisionGraceEndsAt;
        private bool _configured;

        private readonly struct IgnoredColliderPair
        {
            public readonly Collider Own;
            public readonly Collider Other;

            public IgnoredColliderPair(Collider own, Collider other)
            {
                Own = own;
                Other = other;
            }
        }

        public int RecoveryCount { get; private set; }
        public KartRecoveryReason LastRecoveryReason { get; private set; }
        public bool IsCollisionGraceActive => _ignoredColliderPairs.Count > 0;

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
        }

        public void Configure(KartPrototypeInput input, IReadOnlyList<Vector3> recoveryPoints,
            IReadOnlyList<Vector3> racingLine, float trackWidthMeters,
            float stuckThresholdSeconds = 4f, float stoppedSpeedMetersPerSecond = 0.2f,
            float invertedThresholdDegrees = 85f, float safetyHeightMeters = 2f,
            float collisionGraceSeconds = 3f, float perimeterMultiplier = 3f)
        {
            _input = input;
            _recoveryPoints.Clear();
            _racingLine.Clear();

            if (recoveryPoints != null)
            {
                for (var i = 0; i < recoveryPoints.Count; i++)
                {
                    _recoveryPoints.Add(recoveryPoints[i]);
                }
            }

            if (racingLine != null)
            {
                for (var i = 0; i < racingLine.Count; i++)
                {
                    _racingLine.Add(racingLine[i]);
                }
            }

            _trackWidthMeters = Mathf.Max(0.1f, trackWidthMeters);
            _stuckThresholdSeconds = Mathf.Max(0.1f, stuckThresholdSeconds);
            _stoppedSpeedMetersPerSecond = Mathf.Max(0f, stoppedSpeedMetersPerSecond);
            _invertedThresholdDegrees = Mathf.Clamp(invertedThresholdDegrees, 1f, 180f);
            _safetyHeightMeters = Mathf.Max(0.1f, safetyHeightMeters);
            _collisionGraceSeconds = Mathf.Max(0f, collisionGraceSeconds);
            _perimeterMultiplier = Mathf.Max(1f, perimeterMultiplier);
            _stuckDuration = 0f;
            _configured = _recoveryPoints.Count > 0;
            if (!_configured)
            {
                Debug.LogWarning("KartRecoveryController: recovery disabled because no recovery points are configured.");
            }
        }

        private void FixedUpdate()
        {
            if (!_configured || _body == null)
            {
                return;
            }

            UpdateCollisionGrace();

            var monitoringEnabled = _input == null || _input.InputEnabled;
            var horizontalVelocity = new Vector2(_body.linearVelocity.x, _body.linearVelocity.z).magnitude;
            _stuckDuration = KartRecoveryMath.UpdateStuckDuration(_stuckDuration,
                monitoringEnabled, horizontalVelocity, Time.fixedDeltaTime, _stoppedSpeedMetersPerSecond);

            if (!monitoringEnabled)
            {
                return;
            }

            var nearestRecoveryPoint = KartRecoveryMath.FindNearestPoint(_recoveryPoints, _body.position);
            var tiltDegrees = Vector3.Angle(transform.up, Vector3.up);
            var outside = KartRecoveryMath.IsOutsideRecoverablePerimeter(
                _body.position, _racingLine, _trackWidthMeters, _perimeterMultiplier);
            var safetyRisk = _body.position.y - nearestRecoveryPoint.y > _safetyHeightMeters;
            var reason = KartRecoveryMath.EvaluateReason(_stuckDuration, tiltDegrees,
                outside, safetyRisk, _stuckThresholdSeconds, _invertedThresholdDegrees);

            if (reason != KartRecoveryReason.None)
            {
                Recover(nearestRecoveryPoint, reason);
            }
        }

        private void Recover(Vector3 recoveryPoint, KartRecoveryReason reason)
        {
            var forward = KartRecoveryMath.FindTrackForward(_racingLine, recoveryPoint);
            _body.position = recoveryPoint;
            _body.rotation = Quaternion.LookRotation(forward, Vector3.up);
            _body.linearVelocity = Vector3.zero;
            _body.angularVelocity = Vector3.zero;
            _stuckDuration = 0f;
            RecoveryCount++;
            LastRecoveryReason = reason;
            BeginCollisionGrace();
        }

        private void BeginCollisionGrace()
        {
            RestoreIgnoredCollisions();
            var ownColliders = GetComponentsInChildren<Collider>(true);
            var activeKarts = KartDynamics.AllActiveKarts;
            for (var kartIndex = 0; kartIndex < activeKarts.Count; kartIndex++)
            {
                var otherKart = activeKarts[kartIndex];
                if (otherKart == null || otherKart.gameObject == gameObject)
                {
                    continue;
                }

                var otherColliders = otherKart.GetComponentsInChildren<Collider>(true);
                for (var ownIndex = 0; ownIndex < ownColliders.Length; ownIndex++)
                {
                    for (var otherIndex = 0; otherIndex < otherColliders.Length; otherIndex++)
                    {
                        var own = ownColliders[ownIndex];
                        var other = otherColliders[otherIndex];
                        if (own == null || other == null)
                        {
                            continue;
                        }

                        UnityEngine.Physics.IgnoreCollision(own, other, true);
                        _ignoredColliderPairs.Add(new IgnoredColliderPair(own, other));
                    }
                }
            }

            _collisionGraceEndsAt = Time.time + _collisionGraceSeconds;
        }

        private void UpdateCollisionGrace()
        {
            if (_ignoredColliderPairs.Count > 0 && Time.time >= _collisionGraceEndsAt)
            {
                RestoreIgnoredCollisions();
            }
        }

        private void OnDisable()
        {
            RestoreIgnoredCollisions();
        }

        private void OnDestroy()
        {
            RestoreIgnoredCollisions();
        }

        private void RestoreIgnoredCollisions()
        {
            for (var i = 0; i < _ignoredColliderPairs.Count; i++)
            {
                var pair = _ignoredColliderPairs[i];
                if (pair.Own != null && pair.Other != null)
                {
                    UnityEngine.Physics.IgnoreCollision(pair.Own, pair.Other, false);
                }
            }

            _ignoredColliderPairs.Clear();
        }
    }
}
