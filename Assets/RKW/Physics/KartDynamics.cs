using UnityEngine;

namespace RKW.Physics
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
    public sealed class KartDynamics : MonoBehaviour
    {
        [SerializeField] private KartCategorySO tuning;
        [SerializeField] private Transform visualRoot;

        private Rigidbody _body;
        private float _steeringInput;
        private float _throttleInput;
        private float _brakeInput;
        private float _smoothedThrottle;
        private float _currentGripRatio = 1f;

        public float SpeedKph { get; private set; }
        public float SignedForwardSpeedKph { get; private set; }
        public float SlipAngleDegrees { get; private set; }
        public float GripRatio => _currentGripRatio;
        public float LateralWeightTransferRatio { get; private set; }
        public float InnerRearLift { get; private set; }
        public float BrakeOversteerFactor { get; private set; }
        public KartCategorySO Tuning => tuning;

        public void Configure(KartCategorySO category, Transform kartVisual = null)
        {
            tuning = category;
            visualRoot = kartVisual;
            ApplyBodyConfiguration();
        }

        public void SetInput(float steering, float throttle, float brake)
        {
            _steeringInput = Mathf.Clamp(steering, -1f, 1f);
            _throttleInput = Mathf.Clamp01(throttle);
            _brakeInput = Mathf.Clamp01(brake);
        }

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            ApplyBodyConfiguration();
        }

        private void FixedUpdate()
        {
            if (tuning == null || !tuning.IsValid(out _))
            {
                return;
            }

            var deltaTime = Time.fixedDeltaTime;
            var localVelocity = transform.InverseTransformDirection(_body.linearVelocity);
            var forwardSpeed = localVelocity.z;
            SpeedKph = new Vector2(_body.linearVelocity.x, _body.linearVelocity.z).magnitude * 3.6f;
            SignedForwardSpeedKph = forwardSpeed * 3.6f;

            UpdateThrottle(deltaTime);
            ApplyLongitudinalForces(forwardSpeed);
            ApplyLateralForces(localVelocity, deltaTime);
            ApplySteering(forwardSpeed);
            EnforceSpeedLimit();
        }

        private void LateUpdate()
        {
            if (visualRoot == null || tuning == null)
            {
                return;
            }

            var targetRoll = -Mathf.Sign(_steeringInput) * LateralWeightTransferRatio *
                             tuning.VisualWeightTransferDegrees;
            var target = Quaternion.Euler(0f, 0f, targetRoll);
            visualRoot.localRotation = Quaternion.Slerp(
                visualRoot.localRotation,
                target,
                1f - Mathf.Exp(-8f * Time.deltaTime));
        }

        private void UpdateThrottle(float deltaTime)
        {
            var rate = 1f / Mathf.Max(0.15f, tuning.ThrottleRampSeconds);
            _smoothedThrottle = Mathf.MoveTowards(_smoothedThrottle, _throttleInput, rate * deltaTime);
        }

        private void ApplyLongitudinalForces(float forwardSpeed)
        {
            var direction = forwardSpeed >= 0f ? 1f : -1f;
            var acceleration = KartDynamicsMath.CalculateAccelerationMetersPerSecondSquared(
                forwardSpeed,
                tuning.MaxSpeedMetersPerSecond,
                tuning.ZeroToMaxSeconds);

            if (_smoothedThrottle > 0f && forwardSpeed < tuning.MaxSpeedMetersPerSecond)
            {
                _body.AddForce(transform.forward * (acceleration * _smoothedThrottle), ForceMode.Acceleration);
            }

            if (_brakeInput > 0f && forwardSpeed > 0.25f)
            {
                KartDynamicsMath.CalculateBrakingWithSteering(
                    _brakeInput,
                    _steeringInput,
                    forwardSpeed,
                    tuning.BrakeDeceleration,
                    tuning.RearBrakeDistribution,
                    _currentGripRatio,
                    tuning.LateralGripG,
                    tuning.BrakeOversteerGain,
                    out var effectiveBrake,
                    out var oversteer);
                BrakeOversteerFactor = oversteer;
                _body.AddForce(-transform.forward * effectiveBrake, ForceMode.Acceleration);
            }
            else
            {
                BrakeOversteerFactor = 0f;
            }

            if (_brakeInput > 0f && forwardSpeed <= 0.25f && forwardSpeed > -tuning.ReverseMaxSpeedMetersPerSecond)
            {
                _body.AddForce(-transform.forward * (tuning.ReverseAcceleration * _brakeInput),
                    ForceMode.Acceleration);
            }
            else if (_smoothedThrottle <= 0.01f && Mathf.Abs(forwardSpeed) > 0.05f)
            {
                _body.AddForce(-transform.forward * (direction * tuning.CoastingDeceleration),
                    ForceMode.Acceleration);
            }

            var planarVelocity = new Vector3(_body.linearVelocity.x, 0f, _body.linearVelocity.z);
            if (planarVelocity.sqrMagnitude > 0.01f)
            {
                _body.AddForce(-planarVelocity.normalized *
                               (planarVelocity.sqrMagnitude * tuning.AerodynamicDrag), ForceMode.Acceleration);
            }

            var steeringLoss = KartDynamicsMath.CalculateSteeringSpeedLoss(
                _steeringInput,
                forwardSpeed,
                tuning.MaxSpeedMetersPerSecond,
                tuning.SteeringLossAcceleration);
            _body.AddForce(-transform.forward * (direction * steeringLoss), ForceMode.Acceleration);
        }

        private void ApplyLateralForces(Vector3 localVelocity, float deltaTime)
        {
            SlipAngleDegrees = Mathf.Atan2(localVelocity.x, Mathf.Abs(localVelocity.z) + 0.1f) * Mathf.Rad2Deg;
            var targetGrip = Mathf.Abs(SlipAngleDegrees) <= tuning.PeakSlipAngleDegrees
                ? 1f
                : KartDynamicsMath.EvaluateGripCurve(
                    SlipAngleDegrees,
                    tuning.PeakSlipAngleDegrees,
                    tuning.FullLossSlipAngleDegrees,
                    tuning.MinimumGripRatio);

            var gripRate = targetGrip < _currentGripRatio ? tuning.GripLossRate : tuning.GripRecoveryRate;
            _currentGripRatio = Mathf.MoveTowards(_currentGripRatio, targetGrip, gripRate * deltaTime);

            LateralWeightTransferRatio = KartDynamicsMath.CalculateLateralWeightTransferRatio(
                localVelocity.z,
                _steeringInput,
                tuning.MaxSpeedMetersPerSecond,
                tuning.CenterOfMassHeightMeters,
                tuning.RearTrackWidthMeters,
                tuning.WeightTransferGain);
            InnerRearLift = KartDynamicsMath.CalculateInnerRearLift(
                LateralWeightTransferRatio,
                tuning.InnerRearLiftThreshold);

            var rigidAxleRelease = Mathf.Lerp(1f - tuning.RigidAxleGripInfluence, 1f, InnerRearLift);
            var brakeOversteerReduction = 1f - BrakeOversteerFactor * 0.3f; // braking with steering reduces grip
            var maximumLateralAcceleration = tuning.LateralGripG * KartDynamicsMath.Gravity *
                                             _currentGripRatio * rigidAxleRelease *
                                             Mathf.Max(0.4f, brakeOversteerReduction);
            var requestedAcceleration = -localVelocity.x * tuning.LateralResponse;
            var lateralAcceleration = Mathf.Clamp(
                requestedAcceleration,
                -maximumLateralAcceleration,
                maximumLateralAcceleration);
            _body.AddForce(transform.right * lateralAcceleration, ForceMode.Acceleration);
        }

        private void ApplySteering(float forwardSpeed)
        {
            var speedRatio = Mathf.Clamp01(Mathf.Abs(forwardSpeed) / 3f);
            var direction = forwardSpeed >= -0.05f ? 1f : -1f;
            var targetYawRate = _steeringInput * tuning.MaximumYawRateDegrees * speedRatio *
                                _currentGripRatio * direction;
            var currentYawRate = transform.InverseTransformDirection(_body.angularVelocity).y * Mathf.Rad2Deg;
            var yawAcceleration = (targetYawRate - currentYawRate) * tuning.YawResponse -
                                  currentYawRate * tuning.YawDamping;
            _body.AddRelativeTorque(Vector3.up * (yawAcceleration * Mathf.Deg2Rad), ForceMode.Acceleration);
        }

        private void EnforceSpeedLimit()
        {
            var localVelocity = transform.InverseTransformDirection(_body.linearVelocity);
            if (localVelocity.z <= tuning.MaxSpeedMetersPerSecond)
            {
                return;
            }

            localVelocity.z = tuning.MaxSpeedMetersPerSecond;
            _body.linearVelocity = transform.TransformDirection(localVelocity);
        }

        private void ApplyBodyConfiguration()
        {
            if (_body == null)
            {
                _body = GetComponent<Rigidbody>();
            }

            if (_body == null || tuning == null)
            {
                return;
            }

            _body.mass = tuning.MassKilograms;
            _body.centerOfMass = new Vector3(0f, -tuning.CenterOfMassHeightMeters, 0f);
            _body.interpolation = RigidbodyInterpolation.Interpolate;
            _body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
    }
}
