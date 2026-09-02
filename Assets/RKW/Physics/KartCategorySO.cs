using UnityEngine;

namespace RKW.Physics
{
    [CreateAssetMenu(fileName = "KartPrototypeTuning", menuName = "RKW/Physics/Kart Prototype Tuning")]
    public sealed class KartCategorySO : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string categoryId = "prototype-school";

        [Header("Body")]
        [Min(1f)] [SerializeField] private float massKilograms = 165f;
        [Min(0.01f)] [SerializeField] private float centerOfMassHeightMeters = 0.22f;
        [Min(0.1f)] [SerializeField] private float wheelbaseMeters = 1.05f;
        [Min(0.1f)] [SerializeField] private float rearTrackWidthMeters = 1.05f;
        // Etapa 4 (2026-08-31): used only by the Ackermann per-wheel VISUAL
        // angle (KartSteeringVisual) -- no physics formula in this file
        // reads it. Defaulted equal to rearTrackWidthMeters for every
        // existing asset (real rental karts usually have front and rear
        // track widths close to each other), so the visual wheel angles
        // this etapa adds start from a physically reasonable geometry
        // rather than an arbitrary guess.
        [Min(0.1f)] [SerializeField] private float frontTrackWidthMeters = 1.05f;
        // Etapa 1.2 (2026-08-31): longitudinal position of the center of
        // mass, as an offset from the wheelbase midpoint. Positive = toward
        // the REAR (the realistic default direction for a kart -- engine
        // and driver both sit closer to the rear axle -- even though the
        // default VALUE below is 0, i.e. "no change from before"). Replaces
        // the old hard assumption (front/rear axle each exactly half the
        // wheelbase from the center of mass) flagged as a candidate blocker
        // at the Etapa 1.1 gate. See FrontAxleDistanceFromCoMMeters /
        // RearAxleDistanceFromCoMMeters below for the derived distances --
        // they always sum to exactly wheelbaseMeters by construction, so
        // there is nothing to independently validate/desync.
        [SerializeField] private float centerOfMassLongitudinalOffsetMeters = 0f;

        [Header("Longitudinal")]
        [Min(1f)] [SerializeField] private float maxSpeedKph = 55f;
        [Min(0.1f)] [SerializeField] private float zeroToMaxSeconds = 8f;
        [Min(0f)] [SerializeField] private float brakeDeceleration = 10f;
        [Range(0.5f, 1f)] [SerializeField] private float rearBrakeDistribution = 0.7f;
        [Min(0f)] [SerializeField] private float brakeOversteerGain = 1.2f;
        [Min(0f)] [SerializeField] private float reverseAcceleration = 2.5f;
        [Min(1f)] [SerializeField] private float reverseMaxSpeedKph = 12f;
        // Etapa 5 (2026-08-31): this is now specifically the ROLLING
        // RESISTANCE baseline (tire/road friction while coasting), scaled
        // per-surface by SurfaceDataSO.RollingResistanceMultiplier -- see
        // engineBrakingDeceleration below for the separate, surface-
        // independent drivetrain-drag component. Field name/value
        // unchanged from before Etapa 5 (every asset keeps its exact old
        // number, and every surface's multiplier defaults to 1.0), so
        // this rename is documentation-only, not a behavior change.
        [Min(0f)] [SerializeField] private float coastingDeceleration = 1.6f;
        // Etapa 5 (2026-08-31): engine braking (drivetrain compression
        // drag when off-throttle) -- ADDED ON TOP of coastingDeceleration
        // above, not surface-dependent. Default 0 for every asset
        // predating Etapa 5, so this is a strict opt-in with zero
        // behavior change until deliberately tuned.
        [Min(0f)] [SerializeField] private float engineBrakingDeceleration = 0f;
        [Min(0f)] [SerializeField] private float aerodynamicDrag = 0.012f;
        [Min(0f)] [SerializeField] private float steeringLossAcceleration = 2.5f;
        [Min(0.15f)] [SerializeField] private float throttleRampSeconds = 0.2f;
        // Etapa 5 (2026-08-31): progressive brake pedal ramp, mirroring
        // throttleRampSeconds above but with its own (shorter -- real
        // brake pedals/hydraulics respond faster than an engine can
        // accelerate) apply/release times. This is a genuine, INTENTIONAL
        // behavior change from the pre-Etapa-5 instant on/off brake input
        // -- see the Etapa 5 report section for why an exactly-neutral
        // default is not possible for a ramp feature (instant application
        // literally means ramp time 0, which is not the physically
        // realistic behavior being added) and why the chosen defaults are
        // short enough that braking distances should not change
        // meaningfully, pending Unity playtest re-confirmation.
        [Min(0.02f)] [SerializeField] private float brakeApplySeconds = 0.08f;
        [Min(0.02f)] [SerializeField] private float brakeReleaseSeconds = 0.18f;

        [Header("Lateral grip")]
        [Min(0.1f)] [SerializeField] private float lateralGripG = 1f;
        [Range(1f, 30f)] [SerializeField] private float peakSlipAngleDegrees = 8f;
        [Range(2f, 60f)] [SerializeField] private float fullLossSlipAngleDegrees = 28f;
        [Range(0f, 1f)] [SerializeField] private float minimumGripRatio = 0.32f;
        [Min(0.1f)] [SerializeField] private float lateralResponse = 7f;
        [Min(0.1f)] [SerializeField] private float gripLossRate = 5f;
        [Min(0.1f)] [SerializeField] private float gripRecoveryRate = 2.5f;

        [Header("Axle slip/grip model (Etapa 1) -- NOT a full per-axle force model, see auditoria-fisica-kart.md")]
        // Etapa 1 (2026-08-31): front/rear split of the grip curve above.
        // Defaulted per-asset to mirror this category's existing
        // peakSlipAngleDegrees/fullLossSlipAngleDegrees so the very first
        // build of the axle model behaves ~like the old single-axle one
        // (see auditoria-fisica-kart.md P1 and KartDynamicsMath's new axle
        // functions) -- minimumGripRatio stays shared above rather than
        // duplicated per axle, to keep this tuning block from ballooning.
        //
        // RECOVERY tuning round (2026-08-31): frontPeakSlipAngleDegrees /
        // frontFullLossSlipAngleDegrees range widened (was 1-30 / 2-60).
        // Root cause found via the turning-matrix validation script
        // (kartgrid_turning_matrix.py): CalculateFrontAxleSlipAngleDegrees
        // reads "velocity angle MINUS commanded wheel angle" as the front
        // slip -- so the instant a player steers, before the kart's yaw has
        // caught up, the formula reads almost the FULL commanded wheel
        // angle (up to MaxSteeringAngleDegrees, 26-30 degrees on the
        // shipped presets) as if it were tire slip. With the old 1-30 range
        // there was no room to set frontPeakSlipAngleDegrees above a
        // realistic max steering angle, so any real steering input alone
        // (with zero actual sliding) tipped the front axle past its own
        // peak and collapsed front grip -- which then capped the
        // achievable yaw rate via LimitYawRateToAvailableGrip, which kept
        // yaw from developing, which kept the slip angle from ever
        // shrinking back down: a self-reinforcing "more steering wheel,
        // less actual turning" loop, confirmed mathematically by the
        // validation script before this range was widened. Rear keeps the
        // old, narrower range unchanged -- the rear axle does not steer, so
        // its slip angle is real drift, not this geometric artifact, and
        // realistic (tighter) peak/full-loss values there are exactly what
        // gives the rear its own, separate "can still step out" character.
        [Range(1f, 30f)] [SerializeField] private float rearPeakSlipAngleDegrees = 8f;
        [Range(2f, 60f)] [SerializeField] private float rearFullLossSlipAngleDegrees = 28f;
        [Range(1f, 50f)] [SerializeField] private float frontPeakSlipAngleDegrees = 8f;
        [Range(2f, 90f)] [SerializeField] private float frontFullLossSlipAngleDegrees = 28f;
        [Min(0.05f)] [SerializeField] private float lowSpeedSlipThresholdMetersPerSecond = 0.3f;

        [Header("Friction ellipse (Etapa 2)")]
        // Etapa 2 (2026-08-31): how much combined lateral+longitudinal
        // demand each axle tolerates before the OTHER axis's capacity
        // starts shrinking (see KartDynamicsMath.CalculateEllipseRemainingCapacityRatio).
        // 1.0 = a plain circle. Defaulted to 1.0 for both axles (a
        // deliberately conservative, "prove the coupling doesn't break
        // anything before tuning it" starting point -- NOT a claim that
        // 1.0 is the physically ideal value for a kart).
        [Min(0.2f)] [SerializeField] private float frontFrictionEllipseBias = 1f;
        [Min(0.2f)] [SerializeField] private float rearFrictionEllipseBias = 1f;

        [Header("Steering and rigid axle")]
        [Range(1f, 60f)] [SerializeField] private float maxSteeringAngleDegrees = 28f;
        // Etapa 4 (2026-08-31): shapes raw -1..1 steering input before it
        // drives anything else (Ackermann yaw rate, front axle slip, the
        // visual wheels). 1 = identity/no change (default, matches every
        // asset predating Etapa 4 exactly). See
        // KartDynamicsMath.ApplySteeringResponseCurve.
        [Range(0.3f, 3f)] [SerializeField] private float steeringResponseCurveExponent = 1f;
        [Min(1f)] [SerializeField] private float maximumYawRateDegrees = 105f;
        [Min(0.1f)] [SerializeField] private float yawResponse = 7f;
        [Min(0f)] [SerializeField] private float yawDamping = 2.5f;
        [Min(0f)] [SerializeField] private float weightTransferGain = 3.4f;
        // RECOVERY tuning round (2026-08-31, "KARTGRID -- RECOVERY & PLAYABILITY
        // TUNING"): playtest found that while the rear axle loses grip
        // (ApplySteering scales YawDamping by _rearGripAvailabilityRatio, which
        // can approach 0 during a real slide), the kart lost almost ALL yaw
        // damping right when it needed it most to converge back out of a
        // slide -- a direct contributor to the reported "kart continua
        // atravessado ate bater" bug. This is a FLOOR on that scaling, not a
        // new torque source: effective damping can shrink as rear grip drops,
        // but never below this fraction of the tuned base YawDamping. 1.0
        // would fully disable the grip-sensitive weakening (never used by any
        // asset); 0 reproduces the pre-recovery-round behavior exactly.
        [Range(0f, 1f)] [SerializeField] private float minimumYawDampingRatio = 0.4f;
        [Range(0f, 1f)] [SerializeField] private float innerRearLiftThreshold = 0.62f;
        [Range(0f, 1f)] [SerializeField] private float rigidAxleGripInfluence = 0.22f;
        [Range(0f, 12f)] [SerializeField] private float visualWeightTransferDegrees = 4f;

        [Header("Rigid rear axle refinement (Etapa 3)")]
        // Etapa 3 (2026-08-31): explicit rigid-axle "bind/scrub" resistance
        // to ROTATION (yaw) -- separate from rigidAxleGripInfluence above
        // (a grip multiplier, left completely untouched so no existing
        // tuning changes). Default 0 = feature off for every asset that
        // predates Etapa 3, until deliberately tuned. See
        // KartDynamicsMath.CalculateRearAxleScrubYawRateLossDegPerSec.
        [Min(0f)] [SerializeField] private float rearAxleMaxScrubYawRateLossDegPerSec = 0f;
        // How readily the chassis flexes to let the inside rear wheel lift
        // under lateral load transfer. 1 = no change from before (default,
        // matches every existing asset exactly). Above 1 = flexes more
        // easily, so the inside wheel lifts (and the axle bind releases)
        // at a LOWER transfer ratio than innerRearLiftThreshold alone would
        // imply. Below 1 = stiffer chassis, needs MORE transfer first.
        [Min(0.1f)] [SerializeField] private float chassisFlexFactor = 1f;

        [Header("Engine (Etapa 10, opt-in torque-curve model)")]
        // Etapa 10 (2026-08-31): OFF by default for every asset predating
        // this etapa -- CalculateAccelerationMetersPerSecondSquared (the
        // existing, already-tuned "proximity to top speed" formula, driven
        // by zeroToMaxSeconds/maxSpeedKph above) remains the acceleration
        // source until a designer explicitly opts a specific asset in here
        // AND tunes the torque values below to reach a similar 0-60/top
        // speed feel. See KartDynamics.ApplyLongitudinalForces for the
        // branch and KartDynamicsMath's Etapa 10 section for the full
        // reasoning (including why MaxSpeedKph above stays a hard safety
        // ceiling even in torque-curve mode, rather than being replaced).
        [SerializeField] private bool useTorqueCurveEngineModel = false;
        [Min(500f)] [SerializeField] private float engineIdleRPM = 1800f;
        [Min(1000f)] [SerializeField] private float engineMaxRPM = 9500f;
        [Min(1000f)] [SerializeField] private float engineRedlineRPM = 10000f;
        // Torque (Nm) at 4 fixed points along the RPM range (idle, 33%,
        // 66%, redline of engineMaxRPM), smoothly interpolated -- see
        // KartDynamicsMath.EvaluateTorqueCurveNewtonMeters. A typical small
        // 2-stroke kart engine peaks in the low-to-mid range and falls off
        // toward redline, hence the default shape (rises then falls).
        [Min(0f)] [SerializeField] private float torqueAtIdleNewtonMeters = 9f;
        [Min(0f)] [SerializeField] private float torqueAtLowMidRpmNewtonMeters = 16f;
        [Min(0f)] [SerializeField] private float torqueAtHighMidRpmNewtonMeters = 14f;
        [Min(0f)] [SerializeField] private float torqueAtRedlineNewtonMeters = 8f;
        // Reserved for a future engine-inertia-based throttle response
        // (smoothing how fast RPM itself can change, independent of the
        // Etapa 5 pedal ramp) -- NOT YET consumed by any formula. Tracked
        // explicitly as technical debt rather than silently wired in
        // half-finished.
        [Min(0.001f)] [SerializeField] private float engineInertiaKgM2 = 0.02f;
        [Min(1f)] [SerializeField] private float finalDriveRatio = 6f;
        [Min(0.05f)] [SerializeField] private float wheelRadiusMeters = 0.139f;

        [Header("Slipstream (draft)")]
        // Founder playtest feedback, 2026-08-20 (round 8): "não consegui
        // ver o vácuo funcionando" — KartDynamicsMath.CalculateSlipstreamDragReduction
        // and its M2-T17 property test already existed, but nothing in
        // KartDynamics ever called it, so the feature had zero effect in
        // play despite the task being checked off. These are the tuning
        // knobs it needs; see KartDynamics.UpdateSlipstream for the wiring.
        [Min(0.1f)] [SerializeField] private float kartLengthMeters = 1.8f;
        [Range(1f, 4f)] [SerializeField] private float slipstreamMaxActivationLengths = 1.6f;
        [Range(0f, 0.3f)] [SerializeField] private float slipstreamMaxReduction = 0.2f;
        [Min(0f)] [SerializeField] private float slipstreamMinimumTimeSeconds = 0.6f;
        // Etapa 12 (2026-08-31): time (seconds) for SlipstreamDragReduction
        // to ramp from 0 to its target value (and back). Without this, the
        // minimumTimeInSlipstream gate and the forward-cone cutoff in
        // FindLeaderDistanceMeters both cause an instant snap (in when the
        // gate is crossed, out when a kart exits the cone mid-overtake)
        // instead of a smooth fade. The pure CalculateSlipstreamDragReduction
        // formula itself is unchanged -- this only smooths its OUTPUT over
        // time inside KartDynamics.UpdateSlipstream. Default is short/subtle
        // on purpose: this is a smoothness fix, not a new gameplay effect.
        [Min(0.01f)] [SerializeField] private float slipstreamTransitionSeconds = 0.35f;

        public string CategoryId => categoryId;
        public float MassKilograms => massKilograms;
        public float CenterOfMassHeightMeters => centerOfMassHeightMeters;
        public float WheelbaseMeters => wheelbaseMeters;
        public float RearTrackWidthMeters => rearTrackWidthMeters;
        public float FrontTrackWidthMeters => frontTrackWidthMeters;
        public float CenterOfMassLongitudinalOffsetMeters => centerOfMassLongitudinalOffsetMeters;
        /// <summary>Distance from the center of mass to the FRONT axle (meters). See centerOfMassLongitudinalOffsetMeters.</summary>
        public float FrontAxleDistanceFromCoMMeters => wheelbaseMeters * 0.5f + centerOfMassLongitudinalOffsetMeters;
        /// <summary>Distance from the center of mass to the REAR axle (meters). Always sums with the front distance to exactly WheelbaseMeters.</summary>
        public float RearAxleDistanceFromCoMMeters => wheelbaseMeters * 0.5f - centerOfMassLongitudinalOffsetMeters;
        public float MaxSpeedKph => maxSpeedKph;
        public float MaxSpeedMetersPerSecond => maxSpeedKph / 3.6f;
        public float ZeroToMaxSeconds => zeroToMaxSeconds;
        public float BrakeDeceleration => brakeDeceleration;
        public float RearBrakeDistribution => rearBrakeDistribution;
        public float BrakeOversteerGain => brakeOversteerGain;
        public float ReverseAcceleration => reverseAcceleration;
        public float ReverseMaxSpeedMetersPerSecond => reverseMaxSpeedKph / 3.6f;
        public float CoastingDeceleration => coastingDeceleration;
        public float EngineBrakingDeceleration => engineBrakingDeceleration;
        public float BrakeApplySeconds => brakeApplySeconds;
        public float BrakeReleaseSeconds => brakeReleaseSeconds;
        public float AerodynamicDrag => aerodynamicDrag;
        public float SteeringLossAcceleration => steeringLossAcceleration;
        public float ThrottleRampSeconds => throttleRampSeconds;
        public bool UseTorqueCurveEngineModel => useTorqueCurveEngineModel;
        public float EngineIdleRPM => engineIdleRPM;
        public float EngineMaxRPM => engineMaxRPM;
        public float EngineRedlineRPM => engineRedlineRPM;
        public float TorqueAtIdleNewtonMeters => torqueAtIdleNewtonMeters;
        public float TorqueAtLowMidRpmNewtonMeters => torqueAtLowMidRpmNewtonMeters;
        public float TorqueAtHighMidRpmNewtonMeters => torqueAtHighMidRpmNewtonMeters;
        public float TorqueAtRedlineNewtonMeters => torqueAtRedlineNewtonMeters;
        public float EngineInertiaKgM2 => engineInertiaKgM2;
        public float FinalDriveRatio => finalDriveRatio;
        public float WheelRadiusMeters => wheelRadiusMeters;
        public float LateralGripG => lateralGripG;
        public float PeakSlipAngleDegrees => peakSlipAngleDegrees;
        public float FullLossSlipAngleDegrees => fullLossSlipAngleDegrees;
        public float MinimumGripRatio => minimumGripRatio;
        public float LateralResponse => lateralResponse;
        public float GripLossRate => gripLossRate;
        public float GripRecoveryRate => gripRecoveryRate;
        public float FrontPeakSlipAngleDegrees => frontPeakSlipAngleDegrees;
        public float RearPeakSlipAngleDegrees => rearPeakSlipAngleDegrees;
        public float FrontFullLossSlipAngleDegrees => frontFullLossSlipAngleDegrees;
        public float RearFullLossSlipAngleDegrees => rearFullLossSlipAngleDegrees;
        public float LowSpeedSlipThresholdMetersPerSecond => lowSpeedSlipThresholdMetersPerSecond;
        public float FrontFrictionEllipseBias => frontFrictionEllipseBias;
        public float RearFrictionEllipseBias => rearFrictionEllipseBias;
        public float MaxSteeringAngleDegrees => maxSteeringAngleDegrees;
        public float SteeringResponseCurveExponent => steeringResponseCurveExponent;
        public float MaximumYawRateDegrees => maximumYawRateDegrees;
        public float YawResponse => yawResponse;
        public float YawDamping => yawDamping;
        public float WeightTransferGain => weightTransferGain;
        public float MinimumYawDampingRatio => minimumYawDampingRatio;
        public float InnerRearLiftThreshold => innerRearLiftThreshold;
        public float RigidAxleGripInfluence => rigidAxleGripInfluence;
        public float VisualWeightTransferDegrees => visualWeightTransferDegrees;
        public float RearAxleMaxScrubYawRateLossDegPerSec => rearAxleMaxScrubYawRateLossDegPerSec;
        public float ChassisFlexFactor => chassisFlexFactor;
        /// <summary>
        /// innerRearLiftThreshold adjusted by chassisFlexFactor (higher
        /// flex = lower/easier effective threshold). Equals
        /// innerRearLiftThreshold exactly when chassisFlexFactor is 1 (the
        /// default for every existing asset).
        /// </summary>
        public float EffectiveInnerRearLiftThreshold =>
            Mathf.Clamp01(innerRearLiftThreshold / Mathf.Max(0.1f, chassisFlexFactor));
        public float KartLengthMeters => kartLengthMeters;
        public float SlipstreamMaxActivationLengths => slipstreamMaxActivationLengths;
        public float SlipstreamMaxReduction => slipstreamMaxReduction;
        public float SlipstreamMinimumTimeSeconds => slipstreamMinimumTimeSeconds;
        public float SlipstreamTransitionSeconds => slipstreamTransitionSeconds;

        public bool IsValid(out string reason)
        {
            if (string.IsNullOrWhiteSpace(categoryId))
            {
                reason = "Category ID is required.";
                return false;
            }

            if (fullLossSlipAngleDegrees <= peakSlipAngleDegrees)
            {
                reason = "Full-loss slip angle must exceed peak slip angle.";
                return false;
            }

            if (frontFullLossSlipAngleDegrees <= frontPeakSlipAngleDegrees)
            {
                reason = "Front full-loss slip angle must exceed front peak slip angle.";
                return false;
            }

            if (rearFullLossSlipAngleDegrees <= rearPeakSlipAngleDegrees)
            {
                reason = "Rear full-loss slip angle must exceed rear peak slip angle.";
                return false;
            }

            // Etapa 1.2: keep both derived axle distances positive and
            // sane -- an offset at or beyond half the wheelbase would put
            // the center of mass on top of (or past) one of the axles.
            if (Mathf.Abs(centerOfMassLongitudinalOffsetMeters) >= wheelbaseMeters * 0.5f)
            {
                reason = "Center of mass longitudinal offset must be smaller than half the wheelbase.";
                return false;
            }

            // Etapa 10: keep the RPM range internally consistent regardless
            // of whether the torque-curve model is actually enabled for
            // this asset -- EngineRPM (telemetry) is always computed.
            if (engineRedlineRPM < engineMaxRPM)
            {
                reason = "Engine redline RPM must be at or above max RPM.";
                return false;
            }

            if (engineMaxRPM <= engineIdleRPM)
            {
                reason = "Engine max RPM must exceed idle RPM.";
                return false;
            }

            reason = string.Empty;
            return true;
        }
    }
}
