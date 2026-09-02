namespace RKW.Physics
{
    /// <summary>
    /// Etapa 0 (2026-08-31) instrumentation snapshot of one kart's physics
    /// state at one instant. Plain struct on purpose -- passed by ref into
    /// KartDynamics.CaptureTelemetry and stored by value in
    /// KartPhysicsTelemetryRecorder's buffer, so recording a sample never
    /// allocates on the heap.
    ///
    /// Fields marked "Etapa 1+" / "Etapa 2+" are nullable: they stay null
    /// (rendered as "N/A" by KartPhysicsDebugOverlay) until the physics
    /// etapa that actually computes them lands. Nothing here is guessed --
    /// see auditoria-fisica-kart.md for what each etapa is expected to add.
    /// </summary>
    public struct KartPhysicsTelemetrySample
    {
        public float TimestampSeconds;

        public float SpeedKph;
        public float SpeedMps;
        public float ThrottleRaw;
        public float ThrottleSmoothed;
        public float BrakeRaw;
        public float BrakeSmoothed;
        public float SteeringInput;
        public float RequestedYawRateDegPerSec;
        public float ActualYawRateDegPerSec;
        public float LateralVelocityMps;
        public float LongitudinalVelocityMps;
        public float SlipAngleDegrees;
        public float Grip;
        public float LateralAccelerationMps2;
        public float LongitudinalAccelerationMps2;
        public float LateralWeightTransferRatio;
        public float InsideRearUnloadFactor;
        public float InsideRearEstimatedLoadNewtons;
        public float DraftFactor;
        public float DragForceNewtons;
        public string CurrentSurfaceName;

        // Etapa 1 (per-axle tire model) -- populated once that etapa lands.
        public float? FrontSlipAngleDegrees;
        public float? RearSlipAngleDegrees;
        public float? FrontGrip;
        public float? RearGrip;
        public float? UndersteerIndicator;
        public float? OversteerIndicator;

        // Etapa 2 (2026-08-31): friction ellipse / combined grip usage --
        // populated by KartDynamics.CaptureTelemetry. Front/RearGripUsage
        // are the LATERAL (cornering) usage ratio; the *Longitudinal*
        // fields are the drive/brake usage ratio; CombinedGripUsage is a
        // single whole-kart headline number (the more-depleted axle's
        // combined lateral+longitudinal usage).
        public float? FrontGripUsage;
        public float? RearGripUsage;
        public float? FrontLongitudinalGripUsage;
        public float? RearLongitudinalGripUsage;
        public float? CombinedGripUsage;

        // RECOVERY tuning round (2026-08-31): full steering->yaw pipeline
        // trace -- see KartDynamics's matching properties for what each stage
        // means. Not nullable (unlike the Etapa 1/2 fields above) because
        // KartDynamics now always computes these, regardless of
        // KartPhysicsTelemetry.Enabled.
        public float RawSteeringInput;
        public float ProcessedSteeringInput;
        public float PhysicalSteeringAngleDegrees;
        public float PipelineRequestedYawRateDegPerSec;
        public float ScrubLimitedYawRateDegPerSec;
        public float GripLimitedYawRateDegPerSec;
        public float PipelineFinalYawRateDegPerSec;
    }
}
