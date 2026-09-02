namespace RKW.Physics
{
    /// <summary>
    /// Etapa 0 (2026-08-31): single global on/off switch for all kart
    /// physics dev instrumentation (the extra per-frame math in
    /// KartDynamics, the debug overlay, and the CSV recorder). Default is
    /// OFF, and nothing that costs meaningful time runs while it is off --
    /// see the "Enabled" checks in KartDynamics.FixedUpdate,
    /// ApplyLongitudinalForces and ApplySteering. This intentionally has no
    /// #if UNITY_EDITOR guard: a real (non-editor) build can still enable
    /// it via KartPhysicsDebugOverlay's Inspector checkbox for on-device
    /// profiling, but the F-key toggle that flips it at runtime is itself
    /// compiled out of release builds (see KartPhysicsDebugOverlay).
    /// </summary>
    public static class KartPhysicsTelemetry
    {
        public static bool Enabled { get; set; }
    }
}
