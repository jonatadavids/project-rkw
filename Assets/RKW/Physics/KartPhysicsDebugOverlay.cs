using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RKW.Physics
{
    /// <summary>
    /// Etapa 0 (2026-08-31) dev-only physics telemetry overlay. Deliberately
    /// NOT part of the normal gameplay HUD -- see auditoria-fisica-kart.md
    /// (a raw grip number was shown to players once and removed for not
    /// meaning anything to them). This panel's audience is whoever is
    /// tuning the physics, not the player.
    ///
    /// Off by default -- toggle via the Inspector checkbox, or the toggle
    /// key (Editor / development builds only; the key check itself is
    /// compiled out of release builds, see the #if below, so this cannot
    /// accidentally ship visible or reachable in a store build). When off,
    /// KartDynamics skips the extra per-frame instrumentation entirely
    /// (see KartPhysicsTelemetry.Enabled), and this component's own OnGUI
    /// returns immediately without building any text.
    /// </summary>
    public sealed class KartPhysicsDebugOverlay : MonoBehaviour
    {
        [SerializeField] private KartDynamics target;
        // Etapa 1.1 (2026-08-31) gate item 10: F3/F4 need a physical
        // keyboard, so they are useless for testing on an actual phone.
        // This optional link lets the overlay also draw an on-screen
        // "GRAVAR/PARAR" touch button next to its own toggle -- see the
        // on-screen buttons block in OnGUI. Left unassigned, the recorder
        // button simply does not appear (e.g. a scene with no recorder).
        [SerializeField] private KartPhysicsTelemetryRecorder recorder;
        [SerializeField] private bool startEnabled;
        [SerializeField] private Key toggleKey = Key.F3;
        [Min(0.02f)] [SerializeField] private float refreshIntervalSeconds = 0.1f;

        private KartPhysicsTelemetrySample _sample;
        private readonly StringBuilder _builder = new StringBuilder(1024);
        private float _nextRefreshTime;
        private string _renderedText = string.Empty;

        private void Start()
        {
            KartPhysicsTelemetry.Enabled = startEnabled;
            if (target == null)
            {
                // Prototype convenience only: with several karts in a race
                // (player + bots), the first one found may not be the
                // player's kart. Assign `target` explicitly in the
                // Inspector for anything beyond a single-kart test scene.
                target = FindFirstObjectByType<KartDynamics>();
            }

            if (recorder == null)
            {
                recorder = FindFirstObjectByType<KartPhysicsTelemetryRecorder>();
            }
        }

        private void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
            {
                KartPhysicsTelemetry.Enabled = !KartPhysicsTelemetry.Enabled;
            }
#endif
        }

        private void OnGUI()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Etapa 1.1 gate item 10: on-device (phone) equivalent of the
            // F3/F4 keyboard shortcuts -- discreet touch buttons, top-right
            // corner, out of the way of driving controls. Compiled out of
            // release builds entirely by the same #if already used for the
            // keyboard shortcuts in Update(), so this can never ship
            // reachable in a store build, matching the founder's
            // "não permitir isso em release normal" requirement.
            DrawDeviceToggleButtons();
#endif

            if (!KartPhysicsTelemetry.Enabled || target == null)
            {
                return;
            }

            if (Time.unscaledTime >= _nextRefreshTime)
            {
                target.CaptureTelemetry(ref _sample);
                _renderedText = BuildText(ref _sample);
                _nextRefreshTime = Time.unscaledTime + refreshIntervalSeconds;
            }

            var area = new Rect(20f, 20f, 460f, 620f);
            GUI.Box(area, GUIContent.none);
            GUI.Label(new Rect(area.x + 10f, area.y + 6f, area.width - 20f, area.height - 12f), _renderedText);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void DrawDeviceToggleButtons()
        {
            const float buttonWidth = 220f;
            const float buttonHeight = 64f;
            const float margin = 16f;
            var screenWidth = Screen.width;

            var telemetryRect = new Rect(screenWidth - buttonWidth - margin, margin, buttonWidth, buttonHeight);
            var telemetryLabel = KartPhysicsTelemetry.Enabled ? "TELEMETRIA: ON (tocar p/ desligar)" : "TELEMETRIA: OFF (tocar p/ ligar)";
            if (GUI.Button(telemetryRect, telemetryLabel))
            {
                KartPhysicsTelemetry.Enabled = !KartPhysicsTelemetry.Enabled;
            }

            if (recorder == null)
            {
                return;
            }

            var recordRect = new Rect(screenWidth - buttonWidth - margin, margin + buttonHeight + 8f, buttonWidth, buttonHeight);
            var recordLabel = recorder.IsRecording ? "GRAVACAO: PARAR E SALVAR" : "GRAVACAO: INICIAR (CSV)";
            if (GUI.Button(recordRect, recordLabel))
            {
                if (recorder.IsRecording)
                {
                    recorder.StopAndSave();
                }
                else
                {
                    // Recording samples requires telemetry itself to be on
                    // (see KartPhysicsTelemetryRecorder.FixedUpdate) -- turn
                    // it on automatically so pressing "gravar" alone is
                    // enough on a phone with no keyboard to also press F3.
                    KartPhysicsTelemetry.Enabled = true;
                    recorder.StartRecording();
                }
            }
        }
#endif

        private string BuildText(ref KartPhysicsTelemetrySample s)
        {
            _builder.Clear();
            _builder.AppendLine("KART PHYSICS TELEMETRY (dev only -- Etapa 0/1)");
            _builder.AppendLine($"Speed: {s.SpeedKph:F1} km/h ({s.SpeedMps:F2} m/s)");
            _builder.AppendLine($"Throttle raw/smoothed: {s.ThrottleRaw:F2} / {s.ThrottleSmoothed:F2}");
            _builder.AppendLine($"Brake raw/smoothed: {s.BrakeRaw:F2} / {s.BrakeSmoothed:F2} (sem rampa ainda)");
            _builder.AppendLine($"Steering input: {s.SteeringInput:F2}");
            _builder.AppendLine($"Yaw rate requested/actual: {s.RequestedYawRateDegPerSec:F1} / {s.ActualYawRateDegPerSec:F1} deg/s");
            _builder.AppendLine($"Lateral / longitudinal velocity: {s.LateralVelocityMps:F2} / {s.LongitudinalVelocityMps:F2} m/s");
            _builder.AppendLine($"Slip angle (whole kart, legado): {s.SlipAngleDegrees:F1} deg");
            _builder.AppendLine($"Grip (whole kart, legado): {s.Grip:F2}");
            _builder.AppendLine($"Lateral / longitudinal accel: {s.LateralAccelerationMps2:F2} / {s.LongitudinalAccelerationMps2:F2} m/s2");
            _builder.AppendLine($"Lateral weight transfer: {s.LateralWeightTransferRatio:F2}");
            _builder.AppendLine($"Inside rear unload factor: {s.InsideRearUnloadFactor:F2}");
            _builder.AppendLine($"Inside rear estimated load: {s.InsideRearEstimatedLoadNewtons:F0} N (aprox., ver auditoria)");
            _builder.AppendLine($"Draft factor: {s.DraftFactor:F2}");
            _builder.AppendLine($"Drag force: {s.DragForceNewtons:F1} N");
            _builder.AppendLine($"Surface: {s.CurrentSurfaceName}");
            _builder.AppendLine("--- Etapa 1 (per-axle) ---");
            _builder.AppendLine($"Front slip angle: {FormatNullable(s.FrontSlipAngleDegrees)}");
            _builder.AppendLine($"Rear slip angle: {FormatNullable(s.RearSlipAngleDegrees)}");
            _builder.AppendLine($"Front grip: {FormatNullable(s.FrontGrip)}");
            _builder.AppendLine($"Rear grip: {FormatNullable(s.RearGrip)}");
            _builder.AppendLine($"Understeer indicator: {FormatNullable(s.UndersteerIndicator)}");
            _builder.AppendLine($"Oversteer indicator: {FormatNullable(s.OversteerIndicator)}");
            _builder.AppendLine("--- Etapa 2+ (friction circle, ainda nao implementado) ---");
            _builder.AppendLine($"Front/Rear grip usage: {FormatNullable(s.FrontGripUsage)} / {FormatNullable(s.RearGripUsage)}");
            _builder.AppendLine($"Front/Rear long. grip usage: {FormatNullable(s.FrontLongitudinalGripUsage)} / {FormatNullable(s.RearLongitudinalGripUsage)}");
            _builder.AppendLine($"Combined grip usage: {FormatNullable(s.CombinedGripUsage)}");
            return _builder.ToString();
        }

        private static string FormatNullable(float? value)
        {
            return value.HasValue ? value.Value.ToString("F2") : "N/A";
        }
    }
}
