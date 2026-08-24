using System.Collections.Generic;
using UnityEngine;

namespace RKW.Physics
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
    public sealed class KartDynamics : MonoBehaviour
    {
        [SerializeField] private KartCategorySO tuning;
        [SerializeField] private Transform visualRoot;

        // Founder playtest feedback, 2026-08-20 (round 8): "não consegui ver
        // o vácuo funcionando" — every active kart registers itself here so
        // any kart can find whoever is closest ahead of it and draft them.
        // KartDynamicsMath.CalculateSlipstreamDragReduction and its M2-T17
        // property test already existed; this list plus UpdateSlipstream
        // below is what was missing to actually wire it into gameplay.
        private static readonly List<KartDynamics> ActiveKarts = new List<KartDynamics>();

        /// <summary>
        /// Founder playtest feedback, 2026-08-20 (round 18): "pilotagem
        /// agressiva, bloquear, tentar se manter na frente" — read-only view
        /// of the same registry <see cref="UpdateSlipstream"/> already uses,
        /// so <see cref="KartBotController"/> can find nearby rivals (ahead
        /// to attack, behind to defend against) without a second, separate
        /// tracking list.
        /// </summary>
        public static IReadOnlyList<KartDynamics> AllActiveKarts => ActiveKarts;

        // cos(32 degrees) — another kart only counts as a draft target if it
        // is roughly in front of you, not off to the side mid-corner.
        private const float SlipstreamForwardConeCosine = 0.85f;

        private Rigidbody _body;
        private float _steeringInput;
        private float _throttleInput;
        private float _brakeInput;
        private float _smoothedThrottle;
        private float _currentGripRatio = 1f;
        private float _maxLateralAcceleration;
        private float _surfaceGripMultiplier = 1f;
        private float _surfaceInstability;
        private SurfaceDataSO _currentSurface;
        private float _timeInSlipstream;
        private float _slipstreamDragReduction;

        public float SpeedKph { get; private set; }
        public float SignedForwardSpeedKph { get; private set; }
        public float SlipAngleDegrees { get; private set; }
        public float GripRatio => _currentGripRatio;
        public float LateralWeightTransferRatio { get; private set; }
        public float InnerRearLift { get; private set; }
        public float BrakeOversteerFactor { get; private set; }
        public float SurfaceGripMultiplier => _surfaceGripMultiplier;
        public SurfaceDataSO CurrentSurface => _currentSurface;
        public KartCategorySO Tuning => tuning;

        /// <summary>
        /// Round 32 (2026-08-24) founder request: a second kart model/tuning
        /// ("18 HP / 80 km/h") to compare against the existing one. See
        /// <see cref="KartPhysicsPrototypeBootstrap.RebuildKartVisual"/> and
        /// <see cref="KartCategoryToggleButton"/> — this exposes the current
        /// visual so that rebuild method can find and destroy it before
        /// instantiating the replacement. Read-only; Configure() is still
        /// the only way to change it.
        /// </summary>
        public Transform VisualRoot => visualRoot;

        /// <summary>
        /// Round 27 (2026-08-24) founder request: "Esterçamento Ativo:
        /// Sincronizar a rotação do volante com a animação de rotação das
        /// rodas dianteiras" — <see cref="KartSteeringVisual"/> needs to
        /// read the live steering input to rotate the front wheels/cockpit
        /// steering wheel prop, but this field was private with no
        /// accessor. Read-only (SetInput is still the only way to change
        /// it), so this doesn't open a second way to drive the physics.
        /// </summary>
        public float SteeringInput => _steeringInput;

        /// <summary>
        /// Round 28 (2026-08-24) founder request: animated brake/throttle
        /// pedals in the cockpit ("pedais animados de aceleração e freio").
        /// Same reasoning as <see cref="SteeringInput"/> above — a new
        /// visual component (<see cref="KartPedalVisual"/>) needs to read
        /// the live pedal inputs to tilt the 3D pedal props, but these
        /// fields were private. Read-only.
        /// </summary>
        public float ThrottleInput => _throttleInput;

        /// <summary>Read-only counterpart to <see cref="ThrottleInput"/> for the brake pedal.</summary>
        public float BrakeInput => _brakeInput;

        /// <summary>
        /// Current maximum lateral acceleration (m/s²) this kart's tires can
        /// actually deliver right now — already folds in grip ratio, weight
        /// transfer, brake-oversteer and surface (see ApplyLateralForces).
        /// Founder playtest feedback, 2026-08-20 (round 14): "os bots
        /// continuam sem competitividade... nao tem graça". Exposed so
        /// KartBotController can target a real physics-grounded cornering
        /// speed (v = sqrt(radius * grip)) instead of an arbitrary throttle
        /// heuristic disconnected from the same traction model the player
        /// is actually racing against.
        /// </summary>
        public float MaxLateralAcceleration => _maxLateralAcceleration;

        /// <summary>Current aerodynamic drag reduction (0..SlipstreamMaxReduction) from drafting a kart ahead. 0 when not drafting.</summary>
        public float SlipstreamDragReduction => _slipstreamDragReduction;

        /// <summary>Smoothed throttle input (0..1), after the ramp in <see cref="UpdateThrottle"/>. Used by engine audio.</summary>
        public float NormalizedThrottle => _smoothedThrottle;

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

        private void OnEnable()
        {
            ActiveKarts.Add(this);
        }

        private void OnDisable()
        {
            ActiveKarts.Remove(this);
        }

        // Founder playtest feedback, 2026-08-20 (round 10): "apertando o
        // freio numa curva o carro da um pulo ou sensacao de pulo... achei
        // estranho e fora da realidade" — every track surface is flat by
        // design (no ramps), so the only legitimate vertical motion is the
        // gentle drop-in settle at spawn and small resting jitter. Anything
        // launching the kart upward faster than this can only be a physics-
        // engine collision artifact — most likely a glancing kart-vs-kart
        // corner clip against the round-9 bounce material (added on purpose
        // to fix rear-end "impacts" reading as a dead stop; see
        // GetKartCollisionMaterial), whose contact normal isn't perfectly
        // horizontal. Rather than removing that bounce (it fixed a
        // confirmed complaint) or freezing Y position outright (the kart
        // needs to fall ~0.5m to settle onto the track at spawn), this just
        // refuses to let any single physics step launch the kart upward
        // faster than a real kart ever would on flat ground.
        private const float MaxUpwardLaunchSpeedMetersPerSecond = 2f;

        private void FixedUpdate()
        {
            if (tuning == null || !tuning.IsValid(out _))
            {
                return;
            }

            ClampUpwardLaunchVelocity();

            var deltaTime = Time.fixedDeltaTime;
            var localVelocity = transform.InverseTransformDirection(_body.linearVelocity);
            var forwardSpeed = localVelocity.z;
            SpeedKph = new Vector2(_body.linearVelocity.x, _body.linearVelocity.z).magnitude * 3.6f;
            SignedForwardSpeedKph = forwardSpeed * 3.6f;

            UpdateThrottle(deltaTime);
            UpdateSlipstream(deltaTime);
            ApplyLongitudinalForces(forwardSpeed);
            ApplyLateralForces(localVelocity, deltaTime);
            ApplySteering(forwardSpeed);
            ApplyInstability(forwardSpeed);
            EnforceSpeedLimit();
        }

        /// <summary>
        /// Finds whoever is closest ahead of this kart (within a forward
        /// cone, so a kart alongside or behind never counts) and feeds the
        /// distance into <see cref="KartDynamicsMath.CalculateSlipstreamDragReduction"/>.
        /// </summary>
        private void UpdateSlipstream(float deltaTime)
        {
            var leaderDistance = FindLeaderDistanceMeters();
            _timeInSlipstream = leaderDistance.HasValue ? _timeInSlipstream + deltaTime : 0f;

            _slipstreamDragReduction = leaderDistance.HasValue
                ? KartDynamicsMath.CalculateSlipstreamDragReduction(
                    leaderDistance.Value,
                    tuning.KartLengthMeters,
                    tuning.SlipstreamMaxActivationLengths,
                    tuning.SlipstreamMaxReduction,
                    tuning.SlipstreamMinimumTimeSeconds,
                    _timeInSlipstream)
                : 0f;
        }

        private float? FindLeaderDistanceMeters()
        {
            var myPosition = transform.position;
            var myForward = transform.forward;
            float? closestDistance = null;

            for (var i = 0; i < ActiveKarts.Count; i++)
            {
                var other = ActiveKarts[i];
                if (other == null || other == this)
                {
                    continue;
                }

                var toOther = other.transform.position - myPosition;
                toOther.y = 0f;
                var distance = toOther.magnitude;
                if (distance < 0.05f)
                {
                    continue;
                }

                if (Vector3.Dot(toOther.normalized, myForward) < SlipstreamForwardConeCosine)
                {
                    continue;
                }

                if (!closestDistance.HasValue || distance < closestDistance.Value)
                {
                    closestDistance = distance;
                }
            }

            return closestDistance;
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
                // Slipstream (vácuo): drafting closely behind another kart
                // cuts aerodynamic drag, so the effective top speed while
                // tucked in behind someone is higher.
                var effectiveDrag = tuning.AerodynamicDrag * (1f - _slipstreamDragReduction);
                _body.AddForce(-planarVelocity.normalized *
                               (planarVelocity.sqrMagnitude * effectiveDrag), ForceMode.Acceleration);
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
                                             Mathf.Max(0.4f, brakeOversteerReduction) *
                                             _surfaceGripMultiplier;
            // Shared with ApplySteering (runs right after this every
            // FixedUpdate) so the yaw controller never asks for more
            // curvature than this same lateral-force budget can deliver —
            // see the round-10 comment on
            // KartDynamicsMath.LimitYawRateToAvailableGrip for why that
            // coupling is what actually fixes the spin-out feel.
            _maxLateralAcceleration = maximumLateralAcceleration;
            var requestedAcceleration = -localVelocity.x * tuning.LateralResponse;
            var lateralAcceleration = Mathf.Clamp(
                requestedAcceleration,
                -maximumLateralAcceleration,
                maximumLateralAcceleration);
            _body.AddForce(transform.right * lateralAcceleration, ForceMode.Acceleration);
        }

        private void ApplySteering(float forwardSpeed)
        {
            // Founder playtest feedback, 2026-08-20 (round 10): "o kart
            // quase nao faz curva... sai deslizando de lado ate bater na
            // parede... gira 180 graus... falta ser mais gostoso a direcao
            // se assemelhar mais um kart". Replaced the old fixed-max-yaw-
            // rate model (which let the nose spin at a set deg/s cap no
            // matter how much grip that implied) with a kart-realistic one:
            // steering angle sets a turn RADIUS (Ackermann geometry), and
            // current speed sets how fast that radius gets swept — then
            // that request is capped to whatever lateral grip is actually
            // available right now (see KartDynamicsMath for the reasoning
            // on both). tuning.MaximumYawRateDegrees stays as a final
            // safety ceiling, not the primary driver of the turn anymore.
            var ackermannYawRate = KartDynamicsMath.CalculateAckermannYawRateDegreesPerSecond(
                _steeringInput, forwardSpeed, tuning.WheelbaseMeters, tuning.MaxSteeringAngleDegrees);
            var tractionLimitedYawRate = KartDynamicsMath.LimitYawRateToAvailableGrip(
                ackermannYawRate, forwardSpeed, _maxLateralAcceleration);
            var targetYawRate = Mathf.Clamp(tractionLimitedYawRate * _currentGripRatio,
                -tuning.MaximumYawRateDegrees, tuning.MaximumYawRateDegrees);

            var currentYawRate = transform.InverseTransformDirection(_body.angularVelocity).y * Mathf.Rad2Deg;
            var yawAcceleration = (targetYawRate - currentYawRate) * tuning.YawResponse -
                                  currentYawRate * tuning.YawDamping;
            _body.AddRelativeTorque(Vector3.up * (yawAcceleration * Mathf.Deg2Rad), ForceMode.Acceleration);
        }

        private void ApplyInstability(float forwardSpeed)
        {
            if (_surfaceInstability <= 0f || Mathf.Abs(forwardSpeed) < 1f)
            {
                return;
            }

            // Instability proportional to speed, surface factor, and a pseudo-random perturbation
            var speedFactor = Mathf.Clamp01(Mathf.Abs(forwardSpeed) / tuning.MaxSpeedMetersPerSecond);
            var perturbation = Mathf.Sin(Time.fixedTime * 37f + transform.position.x * 7f) *
                               _surfaceInstability * speedFactor * 15f;
            _body.AddRelativeTorque(Vector3.up * (perturbation * Mathf.Deg2Rad), ForceMode.Acceleration);
        }

        /// <summary>
        /// See the round-10 comment on <see cref="MaxUpwardLaunchSpeedMetersPerSecond"/>.
        /// Runs first in <see cref="FixedUpdate"/> so it neutralizes any
        /// spike PhysX's collision solver introduced during the previous
        /// physics step, before this step integrates position further.
        /// </summary>
        private void ClampUpwardLaunchVelocity()
        {
            if (_body.linearVelocity.y <= MaxUpwardLaunchSpeedMetersPerSecond)
            {
                return;
            }

            var velocity = _body.linearVelocity;
            velocity.y = MaxUpwardLaunchSpeedMetersPerSecond;
            _body.linearVelocity = velocity;
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

        private void OnTriggerEnter(Collider other)
        {
            var surface = other.GetComponent<SurfaceTrigger>();
            if (surface == null || surface.SurfaceData == null)
            {
                return;
            }

            _currentSurface = surface.SurfaceData;
            _surfaceGripMultiplier = surface.SurfaceData.GripMultiplier;
            _surfaceInstability = surface.SurfaceData.InstabilityFactor;
        }

        private void OnTriggerExit(Collider other)
        {
            var surface = other.GetComponent<SurfaceTrigger>();
            if (surface == null || surface.SurfaceData != _currentSurface)
            {
                return;
            }

            // Return to default asphalt when leaving a non-asphalt surface
            _currentSurface = null;
            _surfaceGripMultiplier = 1f;
            _surfaceInstability = 0f;
        }
    }
}
