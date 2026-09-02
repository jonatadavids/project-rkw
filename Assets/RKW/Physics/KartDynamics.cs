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
        // Etapa 5 (2026-08-31): rolling-resistance multiplier for the
        // CURRENT surface, mirroring _surfaceGripMultiplier's pattern.
        // Default 1f (asphalt-like baseline) -- see SurfaceDataSO.RollingResistanceMultiplier.
        private float _surfaceRollingResistanceMultiplier = 1f;
        private SurfaceDataSO _currentSurface;
        private float _timeInSlipstream;
        private float _slipstreamDragReduction;
        // Etapa 12 (2026-08-31): the raw, un-smoothed value the pure
        // CalculateSlipstreamDragReduction formula returns this tick --
        // used only as the MoveTowards target inside UpdateSlipstream, so
        // the gate/cone snaps become a smooth ramp instead of an instant
        // jump. _slipstreamDragReduction (above) stays the one and only
        // value every other system (ApplyLongitudinalForces, telemetry)
        // reads -- unchanged from their point of view.
        private float _targetSlipstreamDragReduction;

        // Etapa 1 (2026-08-31): smoothed front/rear axle grip ratios,
        // parallel to the legacy _currentGripRatio above (which is left
        // untouched -- see ApplyLateralForces).
        private float _currentFrontGripRatio = 1f;
        private float _currentRearGripRatio = 1f;
        private float _frontMaxLateralAcceleration;
        private float _rearGripAvailabilityRatio = 1f;

        // Etapa 2 (2026-08-31): friction ellipse usage ratios (0..1), one
        // pair per axis per axle. The *LateralUsageRatio fields are written
        // at the end of ApplyLateralForces (from THIS tick's slip angle)
        // and read at the START of the NEXT tick's ApplyLongitudinalForces
        // -- a deliberate one-tick lag that avoids a same-tick circular
        // dependency between the two methods (see KartDynamicsMath's Etapa
        // 2 section comment for the full reasoning). The *LongitudinalUsageRatio
        // fields have no such lag -- they are read from this tick's own
        // throttle/brake input, which ApplyLateralForces can safely read
        // same-tick since ApplyLongitudinalForces already ran earlier in
        // FixedUpdate.
        private float _frontLateralUsageRatio;
        private float _rearLateralUsageRatio;
        private float _frontLongitudinalUsageRatio;
        private float _rearLongitudinalUsageRatio;

        // Etapa 0 (2026-08-31): dev-only instrumentation state. Only
        // updated while KartPhysicsTelemetry.Enabled is true -- see
        // UpdateTelemetryKinematics, ApplySteering and
        // ApplyLongitudinalForces for the (gated) writers, and
        // CaptureTelemetry for the reader.
        private Vector3 _previousLocalVelocityForTelemetry;
        private float _lateralAccelerationMps2;
        private float _longitudinalAccelerationMps2;
        private float _lastRequestedYawRateDegPerSec;
        private float _lastActualYawRateDegPerSec;
        // RECOVERY tuning round (2026-08-31): full steering->yaw PIPELINE
        // trace, always live (not gated behind KartPhysicsTelemetry.Enabled --
        // these are a handful of extra float writes per tick, not worth
        // gating) so a PlayMode test or the debug overlay can see exactly how
        // much commanded rotation is being lost at each stage: input -> curve
        // -> steering angle -> requested (Ackermann) yaw -> after rigid-axle
        // scrub -> after the front-grip traction limit -> final (after the
        // MaximumYawRateDegrees safety clamp and the legacy whole-kart grip
        // multiply). See ApplySteering for where each is assigned, in this
        // same pipeline order.
        private float _rawSteeringInput;
        private float _requestedYawRateDegPerSec;
        private float _scrubLimitedYawRateDegPerSec;
        private float _gripLimitedYawRateDegPerSec;
        private float _finalYawRateDegPerSec;
        private float _lastDragAccelerationMps2;
        // Etapa 2 (2026-08-31): the RAW requested drive/brake acceleration
        // this tick, BEFORE any friction-ellipse scaling is applied -- used
        // in ApplyLateralForces to compute how much of each axle's own
        // force budget the longitudinal side is asking for. Deliberately
        // NOT the raw throttle/brake pedal position (0..1): an early version
        // used pedal position directly as "usage" and a validation
        // simulation caught a real bug -- full throttle drove usage to 1.0
        // regardless of how weak the kart's actual engine acceleration was
        // compared to the tires' grip, collapsing lateral grip to ~0 and
        // sending rear slip to -80+ degrees. Using the actual requested
        // acceleration (which for a kart's modest engine is usually well
        // below the tire's lateral grip ceiling) against the axle's own
        // budget gives a physically sane usage ratio instead.
        private float _lastRequestedDriveAccelMps2;
        private float _lastRequestedBrakeAccelMps2;
        // Etapa 5 (2026-08-31): progressive brake pedal ramp, mirroring
        // _smoothedThrottle/UpdateThrottle -- see UpdateBrake. Used for all
        // physics-affecting brake logic (forward braking, reverse trigger,
        // the lock-ratio diagnostic below); the RAW _brakeInput above is
        // kept only for the BrakeInput property (pedal visual) and the
        // telemetry BrakeRaw column.
        private float _smoothedBrake;
        // Etapa 5: diagnostic 0..1 wheel-lock estimate -- see
        // KartDynamicsMath.CalculateWheelLockRatio.
        private float _brakeLockRatio;
        // Etapa 10 (2026-08-31): always computed from real wheel speed
        // (see KartDynamicsMath.CalculateEngineRPM), regardless of whether
        // tuning.UseTorqueCurveEngineModel is actually driving the kart's
        // acceleration -- a real telemetry/audio/UI value, not decoration.
        private float _engineRPM;

        // Etapa 3 (2026-08-31): rigid rear axle refinement -- per-corner
        // rear load estimates and the resulting axle bind factor. Written
        // in ApplyLateralForces (right after InnerRearLift, which they
        // depend on), read by ApplySteering the same tick for the scrub
        // resistance term.
        private float _rearInsideLoadNewtons;
        private float _rearOutsideLoadNewtons;
        private float _rearAxleBindingFactor;

        public float SpeedKph { get; private set; }
        public float SignedForwardSpeedKph { get; private set; }
        public float SlipAngleDegrees { get; private set; }
        public float GripRatio => _currentGripRatio;
        public float LateralWeightTransferRatio { get; private set; }
        public float InnerRearLift { get; private set; }
        public float BrakeOversteerFactor { get; private set; }

        /// <summary>Etapa 1 -- front axle slip angle (degrees). See auditoria-fisica-kart.md P1.</summary>
        public float FrontSlipAngleDegrees { get; private set; }
        /// <summary>Etapa 1 -- rear axle slip angle (degrees).</summary>
        public float RearSlipAngleDegrees { get; private set; }
        /// <summary>Etapa 1 -- smoothed front axle grip ratio (0..1).</summary>
        public float FrontGripRatio => _currentFrontGripRatio;
        /// <summary>Etapa 1 -- smoothed rear axle grip ratio (0..1).</summary>
        public float RearGripRatio => _currentRearGripRatio;
        /// <summary>
        /// Diagnostic only (per the Etapa 1 spec) -- never used to modify
        /// the physics itself. abs(frontSlip) - abs(rearSlip); positive
        /// means the front is sliding more than the rear (understeer
        /// tendency).
        /// </summary>
        public float UndersteerIndicator => Mathf.Abs(FrontSlipAngleDegrees) - Mathf.Abs(RearSlipAngleDegrees);
        /// <summary>
        /// Diagnostic only -- never used to modify the physics itself.
        /// abs(rearSlip) - abs(frontSlip); positive means the rear is
        /// sliding more than the front (oversteer tendency).
        /// </summary>
        public float OversteerIndicator => Mathf.Abs(RearSlipAngleDegrees) - Mathf.Abs(FrontSlipAngleDegrees);

        /// <summary>Etapa 2 -- fraction (0..1) of the front axle's lateral (cornering) grip budget currently in use.</summary>
        public float FrontLateralDemand => _frontLateralUsageRatio;
        /// <summary>Etapa 2 -- fraction (0..1) of the rear axle's lateral (cornering) grip budget currently in use.</summary>
        public float RearLateralDemand => _rearLateralUsageRatio;
        /// <summary>Etapa 2 -- fraction (0..1) of the front axle's longitudinal (braking) grip budget currently in use.</summary>
        public float FrontLongitudinalDemand => _frontLongitudinalUsageRatio;
        /// <summary>Etapa 2 -- fraction (0..1) of the rear axle's longitudinal (drive or braking) grip budget currently in use.</summary>
        public float RearLongitudinalDemand => _rearLongitudinalUsageRatio;
        /// <summary>Etapa 2 -- diagnostic combined (lateral+longitudinal) grip usage magnitude for the front axle, 0..1. Display only -- the physics uses the biased ellipse, not this plain circle.</summary>
        public float FrontCombinedGripUsage => KartDynamicsMath.CalculateCombinedGripUsage(_frontLateralUsageRatio, _frontLongitudinalUsageRatio);
        /// <summary>Etapa 2 -- diagnostic combined grip usage magnitude for the rear axle, 0..1.</summary>
        public float RearCombinedGripUsage => KartDynamicsMath.CalculateCombinedGripUsage(_rearLateralUsageRatio, _rearLongitudinalUsageRatio);
        /// <summary>Etapa 3 -- estimated load (Newtons) on the rear corner currently INSIDE the turn. See CalculateRearCornerLoadNewtons.</summary>
        public float RearInsideLoadNewtons => _rearInsideLoadNewtons;
        /// <summary>Etapa 3 -- estimated load (Newtons) on the rear corner currently OUTSIDE the turn.</summary>
        public float RearOutsideLoadNewtons => _rearOutsideLoadNewtons;
        /// <summary>Etapa 3 -- 0..1, how much the rigid rear axle is currently bound/fighting rotation. 0 = released (inside wheel unloaded), 1 = both corners still fully loaded.</summary>
        public float RearAxleBindingFactor => _rearAxleBindingFactor;
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
        /// <summary>Raw -1..1 input exactly as passed to SetInput, BEFORE the Etapa 4 steering response curve. See SteeringInput for the curved value every physics formula actually uses.</summary>
        public float RawSteeringInput => _rawSteeringInput;
        /// <summary>Alias for SteeringInput -- the curved value -- named to match the RECOVERY round's requested pipeline-trace telemetry set (RawSteeringInput -> ProcessedSteeringInput -> PhysicalSteeringAngleDegrees -> ...).</summary>
        public float ProcessedSteeringInput => _steeringInput;
        /// <summary>The actual commanded front-wheel angle in degrees (ProcessedSteeringInput * tuning.MaxSteeringAngleDegrees) -- this is the number CalculateAckermannYawRateDegreesPerSecond and the front axle slip angle actually use; -1..1 input is NOT itself a physical angle.</summary>
        public float PhysicalSteeringAngleDegrees => tuning != null ? _steeringInput * tuning.MaxSteeringAngleDegrees : 0f;
        /// <summary>Ackermann-geometry yaw rate requested by the current steering angle and speed, before any grip/scrub limiting.</summary>
        public float RequestedYawRateDegPerSec => _requestedYawRateDegPerSec;
        /// <summary>RequestedYawRateDegPerSec after the (opt-in, 0 by default) rigid rear axle scrub subtraction.</summary>
        public float ScrubLimitedYawRateDegPerSec => _scrubLimitedYawRateDegPerSec;
        /// <summary>ScrubLimitedYawRateDegPerSec after LimitYawRateToAvailableGrip caps it to what the front axle's current lateral grip can actually deliver.</summary>
        public float GripLimitedYawRateDegPerSec => _gripLimitedYawRateDegPerSec;
        /// <summary>The yaw rate KartDynamics actually targets this tick, after the MaximumYawRateDegrees safety clamp and the legacy whole-kart grip multiply.</summary>
        public float FinalYawRateDegPerSec => _finalYawRateDegPerSec;

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

        /// <summary>Etapa 5 -- smoothed brake input (0..1), after the ramp in <see cref="UpdateBrake"/>. This is what actually drives braking physics.</summary>
        public float SmoothedBrakeInput => _smoothedBrake;

        /// <summary>Etapa 5 -- diagnostic 0..1 wheel-lock estimate (0 = no lock, 1 = fully locked). Display/haptics only, does not itself change any force.</summary>
        public float BrakeLockRatio => _brakeLockRatio;

        /// <summary>Etapa 10 -- engine RPM implied by current wheel speed (see KartDynamicsMath.CalculateEngineRPM). Always available regardless of which acceleration model tuning.UseTorqueCurveEngineModel selects.</summary>
        public float EngineRPM => _engineRPM;

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
            // See the matching comment in Awake() -- same fix, for the
            // runtime-Configure() path (e.g. every PlayMode test's SpawnKart
            // helper, and any spawner that configures a kart right after
            // instantiating it).
            if (category != null)
            {
                _engineRPM = category.EngineIdleRPM;
            }
        }

        /// <summary>
        /// Rodada 46 (2026-09-01) founder feedback: "quando terminar a
        /// corrida o carro poderia parar e o tempo parar tbm" -- the time
        /// already froze (see RaceManager's round-20 fix), but the kart
        /// itself only had its INPUT cut (SetInputEnabled(false)), which
        /// stops new throttle/steering/brake but leaves whatever velocity
        /// the kart already had -- it would coast/slide to a stop over a
        /// few seconds instead of actually stopping the instant the race
        /// ends. Called once, right when RaceManager marks the race
        /// finished, on every kart (see AllActiveKarts above).
        /// </summary>
        public void StopImmediately()
        {
            if (_body == null)
            {
                return;
            }

            _body.linearVelocity = Vector3.zero;
            _body.angularVelocity = Vector3.zero;
        }

        public void SetInput(float steering, float throttle, float brake)
        {
            // Etapa 4 (2026-08-31): steering response curve, applied once
            // here so every reader of _steeringInput (ApplySteering's
            // Ackermann yaw rate, the front axle slip angle in
            // ApplyLateralForces, CalculateBrakingWithSteering, and
            // KartSteeringVisual's wheel/cockpit-wheel animation) sees the
            // SAME already-curved value, instead of each computing it
            // independently or some using raw input and others curved.
            // tuning may still be null this early (Configure() runs in
            // Awake, which could run after a caller's first SetInput in
            // rare edit-time ordering) -- default to the identity curve
            // (exponent 1) in that case, same as the tuning default.
            _rawSteeringInput = Mathf.Clamp(steering, -1f, 1f);
            var curveExponent = tuning != null ? tuning.SteeringResponseCurveExponent : 1f;
            _steeringInput = KartDynamicsMath.ApplySteeringResponseCurve(_rawSteeringInput, curveExponent);
            _throttleInput = Mathf.Clamp01(throttle);
            _brakeInput = Mathf.Clamp01(brake);
        }

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            ApplyBodyConfiguration();
            // Etapa 10 fix (2026-08-31, post-playtest-script round): without this,
            // a kart whose tuning is already assigned in the Inspector reads
            // EngineRPM == 0 for the brief window before its first FixedUpdate
            // tick (CalculateEngineRPM is only ever called from
            // ApplyLongitudinalForces). CalculateEngineRPM itself always clamps
            // to at least idle RPM once it runs -- this just makes that same
            // guarantee hold from frame one, for any telemetry/audio/UI reading
            // EngineRPM before physics has ticked even once.
            if (tuning != null)
            {
                _engineRPM = tuning.EngineIdleRPM;
            }
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

            if (KartPhysicsTelemetry.Enabled)
            {
                UpdateTelemetryKinematics(localVelocity, deltaTime);
            }

            UpdateThrottle(deltaTime);
            UpdateBrake(deltaTime);
            UpdateSlipstream(deltaTime);
            ApplyLongitudinalForces(forwardSpeed);
            ApplyLateralForces(localVelocity, deltaTime);
            ApplySteering(forwardSpeed);
            ApplyInstability(forwardSpeed);
            EnforceSpeedLimit();
        }

        /// <summary>
        /// Etapa 0 instrumentation only: measures this kart's own kinematic
        /// lateral/longitudinal acceleration by finite-differencing local
        /// velocity between physics steps. This only OBSERVES the result of
        /// the forces applied elsewhere in FixedUpdate -- it never feeds
        /// back into steering/throttle/grip -- and only runs while
        /// KartPhysicsTelemetry.Enabled is true, so it costs nothing when
        /// the debug tooling is off.
        /// </summary>
        private void UpdateTelemetryKinematics(Vector3 localVelocity, float deltaTime)
        {
            if (deltaTime > 0f)
            {
                _lateralAccelerationMps2 = (localVelocity.x - _previousLocalVelocityForTelemetry.x) / deltaTime;
                _longitudinalAccelerationMps2 = (localVelocity.z - _previousLocalVelocityForTelemetry.z) / deltaTime;
            }

            _previousLocalVelocityForTelemetry = localVelocity;
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

            _targetSlipstreamDragReduction = leaderDistance.HasValue
                ? KartDynamicsMath.CalculateSlipstreamDragReduction(
                    leaderDistance.Value,
                    tuning.KartLengthMeters,
                    tuning.SlipstreamMaxActivationLengths,
                    tuning.SlipstreamMaxReduction,
                    tuning.SlipstreamMinimumTimeSeconds,
                    _timeInSlipstream)
                : 0f;

            // Etapa 12 (2026-08-31): smooth the gate snap-in (minimumTime
            // threshold crossed) and the forward-cone snap-out (kart exits
            // the cone mid-overtake, leaderDistance becomes null, target
            // drops straight to 0) into a fade instead of an instant jump.
            // maxReduction/transitionSeconds gives a constant-time ramp
            // regardless of the tuned magnitude, matching how the throttle
            // and brake ramps (UpdateThrottle/UpdateBrake) already work.
            var maxStep = deltaTime * (tuning.SlipstreamMaxReduction / tuning.SlipstreamTransitionSeconds);
            _slipstreamDragReduction = Mathf.MoveTowards(
                _slipstreamDragReduction, _targetSlipstreamDragReduction, maxStep);
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
                // Round 39 (continuation 5): once karts are already this
                // close (about one kart length, center-to-center -- close
                // enough to be touching/nearly touching), treat it as
                // contact, not drafting -- see this method's own doc
                // comment above for the full reasoning. tuning is never
                // null here (FixedUpdate already returned early otherwise).
                var minimumDraftDistance = tuning.KartLengthMeters;
                if (distance < minimumDraftDistance)
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
            if (visualRoot == null)
            {
                return;
            }

            // Round 41 (2026-08-27) founder feedback, after the round-40
            // wheel/steering review: "quero que o carro vire faca curva
            // mais natural como um kart nao um balanco" -- this method used
            // to bank the WHOLE visual model a few degrees left/right
            // (Z-axis roll, using LateralWeightTransferRatio *
            // tuning.VisualWeightTransferDegrees) on top of the kart's real
            // steering-driven turn, to suggest weight transfer. That extra
            // motion read as an unwanted side-to-side sway rather than a
            // real kart turning. Removed entirely: the sense that the kart
            // is turning already comes from two things that stay untouched
            // by this change -- KartSteeringVisual's front-wheel angle
            // (exaggerated last round specifically so it reads clearly on
            // screen) and the kart's actual physics yaw (it really does
            // rotate to follow the corner, per KartDynamicsMath's
            // Ackermann/grip model). Forcing the visual root level here
            // guards against any leftover tilt from before this fix ships.
            // The underlying weight-transfer math (LateralWeightTransferRatio,
            // tuning.VisualWeightTransferDegrees) is untouched -- only this
            // cosmetic rotation is gone -- because that ratio still feeds
            // real grip/traction calculations elsewhere in this file, which
            // is physics, not visual, and was not part of this complaint.
            visualRoot.localRotation = Quaternion.identity;
        }

        private void UpdateThrottle(float deltaTime)
        {
            var rate = 1f / Mathf.Max(0.15f, tuning.ThrottleRampSeconds);
            _smoothedThrottle = Mathf.MoveTowards(_smoothedThrottle, _throttleInput, rate * deltaTime);
        }

        /// <summary>
        /// Etapa 5 (2026-08-31): progressive brake pedal ramp -- real brake
        /// pedals/hydraulics take a small but nonzero amount of time to
        /// build up pressure (apply) and to release it, instead of the
        /// pre-Etapa-5 instant on/off. Apply and release use DIFFERENT
        /// rates (tuning.BrakeApplySeconds / BrakeReleaseSeconds) since a
        /// driver stabbing the brake and a driver easing off it (e.g.
        /// trail braking, releasing progressively to rotate the kart) are
        /// physically different actions.
        /// </summary>
        private void UpdateBrake(float deltaTime)
        {
            var applying = _brakeInput > _smoothedBrake;
            var rampSeconds = applying ? tuning.BrakeApplySeconds : tuning.BrakeReleaseSeconds;
            var rate = 1f / Mathf.Max(0.02f, rampSeconds);
            _smoothedBrake = Mathf.MoveTowards(_smoothedBrake, _brakeInput, rate * deltaTime);
        }

        private void ApplyLongitudinalForces(float forwardSpeed)
        {
            var direction = forwardSpeed >= 0f ? 1f : -1f;
            // Round 39 (continuation 4): see EnforceSpeedLimit below for the
            // matching top-speed half of this change.
            var effectiveMaxSpeed = tuning.MaxSpeedMetersPerSecond * _surfaceGripMultiplier;

            // Etapa 10 (2026-08-31): RPM is always computed for telemetry/
            // audio/UI, regardless of which acceleration model is active.
            _engineRPM = KartDynamicsMath.CalculateEngineRPM(
                forwardSpeed, tuning.FinalDriveRatio, tuning.WheelRadiusMeters,
                tuning.EngineIdleRPM, tuning.EngineRedlineRPM);

            // The torque-curve model is OPT-IN per asset (default false --
            // see KartCategorySO.UseTorqueCurveEngineModel's doc comment);
            // every asset predating Etapa 10 keeps using the exact old
            // formula below unchanged.
            var acceleration = tuning.UseTorqueCurveEngineModel
                ? KartDynamicsMath.CalculateTorqueCurveAccelerationMetersPerSecondSquared(
                    _engineRPM, tuning.EngineMaxRPM,
                    tuning.TorqueAtIdleNewtonMeters, tuning.TorqueAtLowMidRpmNewtonMeters,
                    tuning.TorqueAtHighMidRpmNewtonMeters, tuning.TorqueAtRedlineNewtonMeters,
                    tuning.FinalDriveRatio, tuning.WheelRadiusMeters, tuning.MassKilograms)
                : KartDynamicsMath.CalculateAccelerationMetersPerSecondSquared(
                    forwardSpeed,
                    effectiveMaxSpeed,
                    tuning.ZeroToMaxSeconds);

            // Etapa 2 (2026-08-31): friction ellipse -- how much of each
            // axle's grip budget cornering ALREADY used, one physics tick
            // ago (see the field doc comments and KartDynamicsMath's Etapa
            // 2 section for why last-tick values are used here rather than
            // this tick's, which do not exist yet at this point in
            // FixedUpdate). A kart driving straight has ~0 lateral usage on
            // both axles, so these ratios are ~1 (full capacity) in that
            // case -- straight-line acceleration/braking is unaffected by
            // this change.
            var rearLongitudinalCapacityRatio = KartDynamicsMath.CalculateEllipseRemainingCapacityRatio(
                _rearLateralUsageRatio, tuning.RearFrictionEllipseBias);
            var frontLongitudinalCapacityRatioForBraking = KartDynamicsMath.CalculateEllipseRemainingCapacityRatio(
                _frontLateralUsageRatio, tuning.FrontFrictionEllipseBias);

            _lastRequestedDriveAccelMps2 = 0f;
            if (_smoothedThrottle > 0f && forwardSpeed < effectiveMaxSpeed)
            {
                var requestedDriveAccel = acceleration * _smoothedThrottle;
                _lastRequestedDriveAccelMps2 = requestedDriveAccel;
                _body.AddForce(transform.forward * (requestedDriveAccel * rearLongitudinalCapacityRatio),
                    ForceMode.Acceleration);
            }

            _lastRequestedBrakeAccelMps2 = 0f;
            _brakeLockRatio = 0f;
            // Etapa 5 (2026-08-31): all physics-affecting braking now reads
            // the RAMPED _smoothedBrake, not the raw pedal _brakeInput --
            // see UpdateBrake. _brakeInput is still used below only for the
            // reverse-trigger threshold check, which is about detecting
            // driver INTENT (holding the brake at a stop) rather than
            // applying a graduated force, so an instant read there is
            // correct and intentional.
            if (_smoothedBrake > 0f && forwardSpeed > 0.25f)
            {
                KartDynamicsMath.CalculateBrakingWithSteering(
                    _smoothedBrake,
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
                _lastRequestedBrakeAccelMps2 = effectiveBrake;
                // Combined front+rear ellipse capacity, weighted by brake
                // bias -- CalculateBrakingWithSteering itself is untouched
                // (still one shared, already-tested formula), this just
                // scales its result down when one axle is already loaded
                // up on cornering. Left as a combined-axle approximation
                // rather than two fully independent brake circuits, same
                // spirit as the Etapa 1 "intermediate architecture".
                var combinedBrakeCapacityRatio = tuning.RearBrakeDistribution * rearLongitudinalCapacityRatio +
                                                  (1f - tuning.RearBrakeDistribution) * frontLongitudinalCapacityRatioForBraking;
                _body.AddForce(-transform.forward * (effectiveBrake * combinedBrakeCapacityRatio), ForceMode.Acceleration);

                // Etapa 5: diagnostic wheel-lock estimate -- same requested/
                // available deceleration quantities CalculateBrakingWithSteering
                // used internally above (see its own lockRatio), exposed as
                // its own testable value. Does not feed back into any force.
                var requestedBrakeDeceleration = tuning.BrakeDeceleration * _smoothedBrake;
                var availableGripDeceleration = Mathf.Max(0.01f, _currentGripRatio) * tuning.LateralGripG * KartDynamicsMath.Gravity;
                _brakeLockRatio = KartDynamicsMath.CalculateWheelLockRatio(requestedBrakeDeceleration, availableGripDeceleration);
            }
            else
            {
                BrakeOversteerFactor = 0f;
            }

            if (_brakeInput > 0f && forwardSpeed <= 0.25f && forwardSpeed > -tuning.ReverseMaxSpeedMetersPerSecond)
            {
                _body.AddForce(-transform.forward * (tuning.ReverseAcceleration * _smoothedBrake),
                    ForceMode.Acceleration);
            }
            else if (_smoothedThrottle <= 0.01f && Mathf.Abs(forwardSpeed) > 0.05f)
            {
                // Etapa 5 (2026-08-31): two independently-tunable passive
                // decelerations, instead of one flat "coasting" number --
                // rollingResistance now scales with the CURRENT surface
                // (SurfaceDataSO.RollingResistanceMultiplier, 1.0 default =
                // identical to before), engineBraking does not (a kart's
                // drivetrain drags the same regardless of what the tires
                // are touching). Sum is IDENTICAL to the old single
                // coastingDeceleration term for every asset/surface
                // predating Etapa 5 (multiplier 1.0, engine braking 0).
                var rollingResistanceDeceleration = tuning.CoastingDeceleration * _surfaceRollingResistanceMultiplier;
                var totalPassiveDeceleration = rollingResistanceDeceleration + tuning.EngineBrakingDeceleration;
                _body.AddForce(-transform.forward * (direction * totalPassiveDeceleration),
                    ForceMode.Acceleration);
            }

            var planarVelocity = new Vector3(_body.linearVelocity.x, 0f, _body.linearVelocity.z);
            if (planarVelocity.sqrMagnitude > 0.01f)
            {
                // Slipstream (vácuo): drafting closely behind another kart
                // cuts aerodynamic drag, so the effective top speed while
                // tucked in behind someone is higher.
                var effectiveDrag = tuning.AerodynamicDrag * (1f - _slipstreamDragReduction);
                var dragAcceleration = planarVelocity.sqrMagnitude * effectiveDrag;
                _body.AddForce(-planarVelocity.normalized * dragAcceleration, ForceMode.Acceleration);
                if (KartPhysicsTelemetry.Enabled)
                {
                    _lastDragAccelerationMps2 = dragAcceleration;
                }
            }
            else if (KartPhysicsTelemetry.Enabled)
            {
                _lastDragAccelerationMps2 = 0f;
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
            // ---- Legacy whole-kart slip/grip: kept EXACTLY as before.
            // CalculateBrakingWithSteering (braking's wheel-lock threshold)
            // is the only remaining reader of _currentGripRatio; Etapa 1
            // deliberately left braking untouched (see auditoria-fisica-
            // kart.md, that overhaul is a separate later etapa), so this
            // must keep feeding it unchanged. Refactored to call the new
            // shared EvaluateAxleGripRatio helper instead of repeating the
            // same ternary inline -- no behavior change, just removes a
            // near-duplicate expression now that two more call sites below
            // need the identical shortcut.
            SlipAngleDegrees = Mathf.Atan2(localVelocity.x, Mathf.Abs(localVelocity.z) + 0.1f) * Mathf.Rad2Deg;
            var targetGrip = KartDynamicsMath.EvaluateAxleGripRatio(
                SlipAngleDegrees, tuning.PeakSlipAngleDegrees, tuning.FullLossSlipAngleDegrees, tuning.MinimumGripRatio);

            var gripRate = targetGrip < _currentGripRatio ? tuning.GripLossRate : tuning.GripRecoveryRate;
            _currentGripRatio = Mathf.MoveTowards(_currentGripRatio, targetGrip, gripRate * deltaTime);

            LateralWeightTransferRatio = KartDynamicsMath.CalculateLateralWeightTransferRatio(
                localVelocity.z,
                _steeringInput,
                tuning.MaxSpeedMetersPerSecond,
                tuning.CenterOfMassHeightMeters,
                tuning.RearTrackWidthMeters,
                tuning.WeightTransferGain);
            // Etapa 3 (2026-08-31): chassisFlexFactor adjusts the
            // effective threshold (default 1 -> EffectiveInnerRearLiftThreshold
            // == InnerRearLiftThreshold exactly, zero behavior change for
            // every existing asset).
            InnerRearLift = KartDynamicsMath.CalculateInnerRearLift(
                LateralWeightTransferRatio,
                tuning.EffectiveInnerRearLiftThreshold);

            var rigidAxleRelease = Mathf.Lerp(1f - tuning.RigidAxleGripInfluence, 1f, InnerRearLift);

            // Etapa 3: per-corner rear load estimates (Newtons) and the
            // resulting axle bind factor -- see KartDynamicsMath's Etapa 3
            // section for the full reasoning. NOTE (honest caveat, same
            // static-split limitation as before Etapa 3): KartCategorySO
            // has no front/rear static weight-bias parameter yet, so this
            // still assumes an even 50/50 front-rear split at rest before
            // splitting THAT in half again left/right -- a real kart is
            // usually rear-biased. Tracked as technical debt alongside the
            // Etapa 2 longitudinal-load-transfer leftover, not fixed here.
            var totalWeightNewtons = tuning.MassKilograms * KartDynamicsMath.Gravity;
            var staticRearCornerLoadNewtons = totalWeightNewtons * 0.25f; // 50% rear axle x 50% one side
            _rearInsideLoadNewtons = KartDynamicsMath.CalculateRearCornerLoadNewtons(
                staticRearCornerLoadNewtons, LateralWeightTransferRatio, isOutsideCorner: false);
            _rearOutsideLoadNewtons = KartDynamicsMath.CalculateRearCornerLoadNewtons(
                staticRearCornerLoadNewtons, LateralWeightTransferRatio, isOutsideCorner: true);
            _rearAxleBindingFactor = KartDynamicsMath.CalculateRearAxleBindingFactor(InnerRearLift);
            var brakeOversteerReduction = 1f - BrakeOversteerFactor * 0.3f; // braking with steering reduces grip

            // ---- Etapa 1 (2026-08-31): front/rear axle slip angle + grip.
            // Uses the point velocity AT each axle (not the whole-kart COM
            // velocity above), so yawing the kart genuinely gives the front
            // and rear different numbers -- see KartDynamicsMath's axle
            // functions for the sign conventions. Etapa 1.2 (2026-08-31)
            // replaced the original "half the wheelbase for both" guess
            // with each axle's own configurable distance from the center of
            // mass (KartCategorySO.FrontAxleDistanceFromCoMMeters /
            // RearAxleDistanceFromCoMMeters, default still splits the
            // wheelbase evenly).
            var comVelocityWorld = _body.linearVelocity;
            var angularVelocityWorld = _body.angularVelocity;
            var frontVelocityWorld = KartDynamicsMath.CalculateAxlePointVelocityWorld(
                comVelocityWorld, angularVelocityWorld, transform.forward * tuning.FrontAxleDistanceFromCoMMeters);
            var rearVelocityWorld = KartDynamicsMath.CalculateAxlePointVelocityWorld(
                comVelocityWorld, angularVelocityWorld, -transform.forward * tuning.RearAxleDistanceFromCoMMeters);
            var frontLocalVelocity = transform.InverseTransformDirection(frontVelocityWorld);
            var rearLocalVelocity = transform.InverseTransformDirection(rearVelocityWorld);

            FrontSlipAngleDegrees = KartDynamicsMath.CalculateFrontAxleSlipAngleDegrees(
                frontLocalVelocity.x, frontLocalVelocity.z, _steeringInput,
                tuning.MaxSteeringAngleDegrees, tuning.LowSpeedSlipThresholdMetersPerSecond);
            RearSlipAngleDegrees = KartDynamicsMath.CalculateAxleSlipAngleDegrees(
                rearLocalVelocity.x, rearLocalVelocity.z, tuning.LowSpeedSlipThresholdMetersPerSecond);

            var targetFrontGrip = KartDynamicsMath.EvaluateAxleGripRatio(
                FrontSlipAngleDegrees, tuning.FrontPeakSlipAngleDegrees, tuning.FrontFullLossSlipAngleDegrees,
                tuning.MinimumGripRatio);
            var targetRearGrip = KartDynamicsMath.EvaluateAxleGripRatio(
                RearSlipAngleDegrees, tuning.RearPeakSlipAngleDegrees, tuning.RearFullLossSlipAngleDegrees,
                tuning.MinimumGripRatio);

            var frontGripRate = targetFrontGrip < _currentFrontGripRatio ? tuning.GripLossRate : tuning.GripRecoveryRate;
            _currentFrontGripRatio = Mathf.MoveTowards(_currentFrontGripRatio, targetFrontGrip, frontGripRate * deltaTime);
            var rearGripRate = targetRearGrip < _currentRearGripRatio ? tuning.GripLossRate : tuning.GripRecoveryRate;
            _currentRearGripRatio = Mathf.MoveTowards(_currentRearGripRatio, targetRearGrip, rearGripRate * deltaTime);

            // Front axle = "capacidade de direcionar": surface + its own
            // grip curve only. Rear axle = "estabilidade/rotacao":
            // additionally carries the existing rigid-axle-release and
            // brake-oversteer terms, since both are fundamentally rear-axle
            // phenomena (inside rear wheel unloading; trail-braking weight
            // transfer off the rear) -- see the Etapa 1 spec ("insideRearUnload
            // -> alteracao da capacidade/rotacao do REAR AXLE"). Both share
            // the same LateralGripG*g baseline and surface multiplier the
            // old single value used, so with front/rear tuned equal (the
            // conservative Etapa 1 default) this stays close to the
            // previous single-axle result.
            // Etapa 2 (2026-08-31): friction ellipse -- how much of THIS
            // axle's grip is already being asked for on the LONGITUDINAL
            // axis, read same-tick (throttle/brake input and the raw
            // requested drive/brake acceleration are already known by the
            // time ApplyLateralForces runs -- see ApplyLongitudinalForces,
            // which runs first in FixedUpdate, and stores
            // _lastRequestedDriveAccelMps2 / _lastRequestedBrakeAccelMps2).
            //
            // IMPORTANT (found by simulation, not by inspection): usage is
            // the REQUESTED acceleration divided by the axle's own force
            // BUDGET (same order of magnitude as its lateral grip budget),
            // NOT the raw throttle/brake pedal position. An earlier version
            // of this patch used pedal position (0..1) directly as "usage",
            // which drove usage to 1.0 at full throttle regardless of how
            // weak the kart's actual engine acceleration was compared to
            // the tires' grip -- a Python simulation of these exact
            // formulas caught it collapsing lateral grip to ~0 and sending
            // rear slip past -80 degrees at ordinary full-throttle
            // cornering. Using the true requested force against budget
            // keeps a kart's (comparatively weak) engine from ever looking
            // like it saturates the tire on its own -- only combined with
            // genuinely hard cornering does the ellipse actually bite,
            // which is the intended "acelerar forte deve consumir parte da
            // aderencia restante" behavior, not an on/off switch tied to
            // the pedal.
            var baseLateralAcceleration = tuning.LateralGripG * KartDynamicsMath.Gravity * _surfaceGripMultiplier;
            var rearLongitudinalBudget = baseLateralAcceleration * _currentRearGripRatio;
            var frontLongitudinalBudget = baseLateralAcceleration * _currentFrontGripRatio;

            var rearDriveUsage = Mathf.Clamp01(_lastRequestedDriveAccelMps2 / Mathf.Max(0.01f, rearLongitudinalBudget));
            var rearBrakeShareUsage = Mathf.Clamp01(
                (_lastRequestedBrakeAccelMps2 * tuning.RearBrakeDistribution) / Mathf.Max(0.01f, rearLongitudinalBudget));
            _rearLongitudinalUsageRatio = Mathf.Max(rearDriveUsage, rearBrakeShareUsage);
            // Front never drives -- its longitudinal demand is its brake share only.
            _frontLongitudinalUsageRatio = Mathf.Clamp01(
                (_lastRequestedBrakeAccelMps2 * (1f - tuning.RearBrakeDistribution)) / Mathf.Max(0.01f, frontLongitudinalBudget));

            var frontEllipseLateralCapacity = KartDynamicsMath.CalculateEllipseRemainingCapacityRatio(
                _frontLongitudinalUsageRatio, tuning.FrontFrictionEllipseBias);
            var rearEllipseLateralCapacity = KartDynamicsMath.CalculateEllipseRemainingCapacityRatio(
                _rearLongitudinalUsageRatio, tuning.RearFrictionEllipseBias);

            _frontMaxLateralAcceleration = baseLateralAcceleration * _currentFrontGripRatio * frontEllipseLateralCapacity;
            var rearMaxLateralAcceleration = baseLateralAcceleration * _currentRearGripRatio *
                                              rigidAxleRelease * Mathf.Max(0.4f, brakeOversteerReduction) *
                                              rearEllipseLateralCapacity;

            // Store THIS tick's cornering-only usage (independent of
            // throttle/brake) for the NEXT tick's ApplyLongitudinalForces
            // to read -- see the field doc comments up top for why this is
            // a deliberate one-tick-lag, not a same-tick read.
            _frontLateralUsageRatio = Mathf.Clamp01(
                Mathf.Abs(FrontSlipAngleDegrees) / Mathf.Max(0.01f, tuning.FrontPeakSlipAngleDegrees));
            _rearLateralUsageRatio = Mathf.Clamp01(
                Mathf.Abs(RearSlipAngleDegrees) / Mathf.Max(0.01f, tuning.RearPeakSlipAngleDegrees));
            // 0..~1 ratio of the rear axle's own full capacity currently
            // available -- read by ApplySteering to weaken yaw damping when
            // the rear has actually lost grip (see that method for why this
            // is not a new artificial torque source).
            _rearGripAvailabilityRatio = baseLateralAcceleration > 0.0001f
                ? Mathf.Clamp01(rearMaxLateralAcceleration / baseLateralAcceleration)
                : 1f;

            // The car's overall sideways-force ceiling is whichever axle is
            // more depleted right now -- a simplified stand-in for a full
            // per-axle force model (see the Etapa 1 write-up for why moving
            // to real per-point forces was deliberately deferred rather
            // than attempted in the same step as this one).
            var maximumLateralAcceleration = Mathf.Min(_frontMaxLateralAcceleration, rearMaxLateralAcceleration);
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
            // Etapa 1 (2026-08-31): the yaw-rate ceiling below now comes
            // from the FRONT axle specifically ("o eixo dianteiro
            // representa principalmente: capacidade de direcionar o
            // kart") instead of the old combined single-grip value --
            // see ApplyLateralForces, which computes _frontMaxLateralAcceleration
            // right before this method runs every FixedUpdate.
            var ackermannYawRate = KartDynamicsMath.CalculateAckermannYawRateDegreesPerSecond(
                _steeringInput, forwardSpeed, tuning.WheelbaseMeters, tuning.MaxSteeringAngleDegrees);
            _requestedYawRateDegPerSec = ackermannYawRate;

            // Etapa 3 (2026-08-31): rigid rear axle scrub resistance --
            // subtracts from the requested rotation rate directly (a real
            // rotational resistance), rather than scaling any grip value
            // (see KartDynamicsMath's Etapa 3 section for why). Zero
            // whenever the axle is released (_rearAxleBindingFactor 0,
            // e.g. after enough lateral load transfer) or the tuning's max
            // scrub is 0 (every asset predating Etapa 3), so this is a
            // strict opt-in with no behavior change until deliberately
            // tuned.
            var scrubYawRateLossDegPerSec = KartDynamicsMath.CalculateRearAxleScrubYawRateLossDegPerSec(
                _rearAxleBindingFactor, tuning.RearAxleMaxScrubYawRateLossDegPerSec);
            var scrubbedAckermannYawRate = Mathf.Sign(ackermannYawRate) *
                Mathf.Max(0f, Mathf.Abs(ackermannYawRate) - scrubYawRateLossDegPerSec);
            _scrubLimitedYawRateDegPerSec = scrubbedAckermannYawRate;

            var tractionLimitedYawRate = KartDynamicsMath.LimitYawRateToAvailableGrip(
                scrubbedAckermannYawRate, forwardSpeed, _frontMaxLateralAcceleration);
            _gripLimitedYawRateDegPerSec = tractionLimitedYawRate;
            var targetYawRate = Mathf.Clamp(tractionLimitedYawRate * _currentGripRatio,
                -tuning.MaximumYawRateDegrees, tuning.MaximumYawRateDegrees);
            _finalYawRateDegPerSec = targetYawRate;

            var currentYawRate = transform.InverseTransformDirection(_body.angularVelocity).y * Mathf.Rad2Deg;
            // Etapa 1: yaw damping now scales with how much rear-axle grip
            // is actually available (_rearGripAvailabilityRatio, computed
            // in ApplyLateralForces) instead of always being the tuning's
            // raw YawDamping value. A rear tire that has lost grip provides
            // less of a real stabilizing counter-force, so weakening this
            // EXISTING damping term when rear grip drops is what lets the
            // tail step out (oversteer) on its own -- no new torque source,
            // no "if oversteering then add yaw" branch, just this one
            // already-existing term becoming sensitive to the new rear-axle
            // grip value. At full rear grip this is identical to before
            // (ratio == 1).
            // RECOVERY tuning round (2026-08-31): floor added -- see
            // KartCategorySO.MinimumYawDampingRatio's doc comment. Without
            // this, a real slide (rear grip -> near 0) removed almost all
            // damping right when convergence back to alignment needed it most.
            var effectiveYawDamping = tuning.YawDamping *
                Mathf.Max(tuning.MinimumYawDampingRatio, _rearGripAvailabilityRatio);
            var yawAcceleration = (targetYawRate - currentYawRate) * tuning.YawResponse -
                                  currentYawRate * effectiveYawDamping;
            _body.AddRelativeTorque(Vector3.up * (yawAcceleration * Mathf.Deg2Rad), ForceMode.Acceleration);

            if (KartPhysicsTelemetry.Enabled)
            {
                _lastRequestedYawRateDegPerSec = targetYawRate;
                _lastActualYawRateDegPerSec = currentYawRate;
            }
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
            // Round 39 (continuation 4): without this, a kart that reached
            // top speed on pavement and then rolled onto grass would keep
            // coasting at full speed forever, since nothing else in this
            // method ever slows the kart down -- only clamps an overshoot.
            // NOTE (honest caveat): this clamps speed down IMMEDIATELY on
            // entering a low-grip zone at high speed, since this method
            // runs every physics step. If a kart enters grass at very high
            // speed this could feel like a sudden brake rather than a
            // gradual slow-down; a smoother version would need a proper
            // deceleration force instead of a hard clamp. Flagging this so
            // it can be revisited if it feels wrong in testing.
            var effectiveMaxSpeed = tuning.MaxSpeedMetersPerSecond * _surfaceGripMultiplier;
            var localVelocity = transform.InverseTransformDirection(_body.linearVelocity);
            if (localVelocity.z <= effectiveMaxSpeed)
            {
                return;
            }

            localVelocity.z = effectiveMaxSpeed;
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
            // Etapa 1.2: local Z now reflects centerOfMassLongitudinalOffsetMeters
            // (positive = toward the rear, i.e. NEGATIVE local Z since
            // +Z/transform.forward is the front -- see KartCategorySO).
            // Default 0 keeps this identical to before for every existing
            // tuning asset.
            _body.centerOfMass = new Vector3(0f, -tuning.CenterOfMassHeightMeters, -tuning.CenterOfMassLongitudinalOffsetMeters);
            _body.interpolation = RigidbodyInterpolation.Interpolate;
            _body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        /// <summary>
        /// Etapa 0: fills a caller-owned sample with this kart's current
        /// physics state, for the debug overlay / CSV recorder. Takes the
        /// sample by ref so no allocation happens here -- the caller
        /// (KartPhysicsDebugOverlay / KartPhysicsTelemetryRecorder) owns a
        /// single reused instance. Safe to call regardless of
        /// KartPhysicsTelemetry.Enabled (it is the caller's job to gate
        /// whether/how often this gets called at all).
        /// </summary>
        public void CaptureTelemetry(ref KartPhysicsTelemetrySample sample)
        {
            var localVelocity = transform.InverseTransformDirection(_body.linearVelocity);

            sample.TimestampSeconds = Time.time;
            sample.SpeedKph = SpeedKph;
            sample.SpeedMps = new Vector2(_body.linearVelocity.x, _body.linearVelocity.z).magnitude;
            sample.ThrottleRaw = _throttleInput;
            sample.ThrottleSmoothed = _smoothedThrottle;
            sample.BrakeRaw = _brakeInput;
            // Etapa 5 (2026-08-31): real brake ramp now exists (UpdateBrake)
            // -- this is the actual smoothed/ramped value, not a mirror of
            // BrakeRaw anymore.
            sample.BrakeSmoothed = _smoothedBrake;
            sample.SteeringInput = _steeringInput;
            sample.RequestedYawRateDegPerSec = _lastRequestedYawRateDegPerSec;
            sample.ActualYawRateDegPerSec = _lastActualYawRateDegPerSec;
            sample.LateralVelocityMps = localVelocity.x;
            sample.LongitudinalVelocityMps = localVelocity.z;
            sample.SlipAngleDegrees = SlipAngleDegrees;
            sample.Grip = _currentGripRatio;
            sample.LateralAccelerationMps2 = _lateralAccelerationMps2;
            sample.LongitudinalAccelerationMps2 = _longitudinalAccelerationMps2;
            sample.LateralWeightTransferRatio = LateralWeightTransferRatio;
            sample.InsideRearUnloadFactor = InnerRearLift;
            sample.InsideRearEstimatedLoadNewtons = EstimateInsideRearLoadNewtons();
            sample.DraftFactor = _slipstreamDragReduction;
            sample.DragForceNewtons = _lastDragAccelerationMps2 * (tuning != null ? tuning.MassKilograms : 0f);
            sample.CurrentSurfaceName = _currentSurface != null ? _currentSurface.name : "Asfalto (padrao)";

            // Etapa 1: real front/rear values now exist.
            sample.FrontSlipAngleDegrees = FrontSlipAngleDegrees;
            sample.RearSlipAngleDegrees = RearSlipAngleDegrees;
            sample.FrontGrip = _currentFrontGripRatio;
            sample.RearGrip = _currentRearGripRatio;
            sample.UndersteerIndicator = UndersteerIndicator;
            sample.OversteerIndicator = OversteerIndicator;

            // Etapa 2 (2026-08-31): friction ellipse grip usage is now
            // implemented -- see FrontLateralDemand/RearLateralDemand/
            // FrontLongitudinalDemand/RearLongitudinalDemand and
            // Front/RearCombinedGripUsage above. FrontGripUsage/RearGripUsage
            // here are the LATERAL (cornering) usage, matching the debug
            // overlay's "Front/Rear grip usage" label; CombinedGripUsage is
            // the more-depleted of the two axles' combined (lateral+
            // longitudinal) usage, as a single whole-kart headline number.
            sample.FrontGripUsage = _frontLateralUsageRatio;
            sample.RearGripUsage = _rearLateralUsageRatio;
            sample.FrontLongitudinalGripUsage = _frontLongitudinalUsageRatio;
            sample.RearLongitudinalGripUsage = _rearLongitudinalUsageRatio;
            sample.CombinedGripUsage = Mathf.Max(FrontCombinedGripUsage, RearCombinedGripUsage);

            // RECOVERY tuning round (2026-08-31): full steering->yaw pipeline
            // trace, so a telemetry-based turning-matrix test/tool can see
            // exactly where commanded rotation is gained or lost.
            sample.RawSteeringInput = _rawSteeringInput;
            sample.ProcessedSteeringInput = _steeringInput;
            sample.PhysicalSteeringAngleDegrees = PhysicalSteeringAngleDegrees;
            sample.PipelineRequestedYawRateDegPerSec = _requestedYawRateDegPerSec;
            sample.ScrubLimitedYawRateDegPerSec = _scrubLimitedYawRateDegPerSec;
            sample.GripLimitedYawRateDegPerSec = _gripLimitedYawRateDegPerSec;
            sample.PipelineFinalYawRateDegPerSec = _finalYawRateDegPerSec;
        }

        /// <summary>
        /// Telemetry accessor for the Etapa 3 <see cref="RearInsideLoadNewtons"/>
        /// estimate, computed once per FixedUpdate in ApplyLateralForces
        /// (this just exposes that already-computed value -- no separate
        /// calculation happens here anymore). Still an approximation: see
        /// RearInsideLoadNewtons's computation site for the front/rear
        /// static-bias caveat that carries over unchanged from before
        /// Etapa 3.
        /// </summary>
        private float EstimateInsideRearLoadNewtons()
        {
            return tuning == null ? 0f : _rearInsideLoadNewtons;
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
            _surfaceRollingResistanceMultiplier = surface.SurfaceData.RollingResistanceMultiplier;
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
            _surfaceRollingResistanceMultiplier = 1f;
        }
    }
}
