using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RKW.Physics
{
    /// <summary>
    /// Etapa 0 (2026-08-31): minimal before/after data capture for the
    /// physics evolution -- "press a key, drive a lap or a specific test,
    /// press the key again, get a CSV". Not a general profiling tool.
    ///
    /// Samples are buffered as plain structs (KartPhysicsTelemetrySample)
    /// in a pre-sized List, so recording a tick is a struct copy into an
    /// existing array slot, not a heap allocation -- only the final CSV
    /// write (once, when recording stops) builds strings.
    /// </summary>
    [RequireComponent(typeof(KartDynamics))]
    public sealed class KartPhysicsTelemetryRecorder : MonoBehaviour
    {
        [SerializeField] private Key recordToggleKey = Key.F4;
        // Etapa 1.1 (2026-08-31) fix for gate item 11: the previous fixed
        // "initialBufferCapacity = 8192" assumed 50 Hz physics (8192 / 50
        // ~= 164s), which the founder correctly flagged as possibly
        // shorter than a full race. This is now a configurable DURATION
        // instead of a raw sample count, and the actual capacity is
        // computed from Time.fixedDeltaTime at Awake (so it stays correct
        // even if the project's physics tick rate ever changes) -- see
        // ComputeMaxSamples below.
        [Min(1f)] [SerializeField] private float maxRecordingMinutes = 20f;

        private KartDynamics _target;
        private List<KartPhysicsTelemetrySample> _buffer;
        private bool _isRecording;
        private int _maxSamples;

        public bool IsRecording => _isRecording;

        /// <summary>
        /// How many FixedUpdate samples <see cref="maxRecordingMinutes"/>
        /// corresponds to, given the project's actual fixed timestep. Kept
        /// as a fixed cap (never grown mid-recording) on purpose: letting
        /// the List grow past its initial capacity would allocate in the
        /// middle of a recording, which is exactly the "allocation
        /// imprevisivel" the founder asked to avoid.
        /// </summary>
        private static int ComputeMaxSamples(float minutes)
        {
            var samplesPerSecond = 1f / Mathf.Max(0.0001f, Time.fixedDeltaTime);
            return Mathf.Max(64, Mathf.CeilToInt(minutes * 60f * samplesPerSecond));
        }

        private void Awake()
        {
            _target = GetComponent<KartDynamics>();
            _maxSamples = ComputeMaxSamples(maxRecordingMinutes);
            _buffer = new List<KartPhysicsTelemetrySample>(_maxSamples);
        }

        private void Update()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Keyboard.current != null && Keyboard.current[recordToggleKey].wasPressedThisFrame)
            {
                if (_isRecording)
                {
                    StopAndSave();
                }
                else
                {
                    StartRecording();
                }
            }
#endif
        }

        public void StartRecording()
        {
            _buffer.Clear();
            _isRecording = true;
        }

        private void FixedUpdate()
        {
            if (!_isRecording || !KartPhysicsTelemetry.Enabled)
            {
                return;
            }

            // Etapa 1.1 fix for gate item 11: never let the buffer grow
            // past its pre-sized capacity (that reallocation would be an
            // unpredictable mid-recording allocation). Instead of silently
            // dropping samples or crashing, auto-stop and flush what was
            // captured so far -- the founder still gets a usable CSV
            // instead of losing the whole recording, and the log line
            // makes it obvious this happened rather than hiding it.
            if (_buffer.Count >= _maxSamples)
            {
                Debug.LogWarning($"[KartPhysicsTelemetryRecorder] Reached the configured recording " +
                                  $"limit ({maxRecordingMinutes} min, {_maxSamples} samples) -- auto-saving " +
                                  "and stopping. Increase maxRecordingMinutes in the Inspector if you need longer captures.");
                StopAndSave();
                return;
            }

            var sample = default(KartPhysicsTelemetrySample);
            _target.CaptureTelemetry(ref sample);
            _buffer.Add(sample);
        }

        /// <summary>Stops recording (if active) and writes the buffered samples to a CSV file. Returns the full file path.</summary>
        public string StopAndSave()
        {
            _isRecording = false;
            return WriteCsv();
        }

        private string WriteCsv()
        {
            var directory = Path.Combine(Application.persistentDataPath, "KartPhysicsTelemetry");
            Directory.CreateDirectory(directory);
            var fileName = $"kart-telemetry-{DateTime.Now:yyyyMMdd-HHmmss}.csv";
            var fullPath = Path.Combine(directory, fileName);

            var culture = CultureInfo.InvariantCulture;
            var sb = new StringBuilder(_buffer.Count * 96 + 256);
            sb.AppendLine("timestamp,speedKph,speedMps,steering,throttleRaw,throttleSmoothed,brakeRaw,brakeSmoothed," +
                          "requestedYawRate,actualYawRate,slipAngle,grip,lateralG,longitudinalG,weightTransfer," +
                          "insideRearUnload,insideRearLoadN,draftFactor,dragForceN,surface," +
                          "frontSlipAngle,rearSlipAngle,frontGrip,rearGrip,understeerIndicator,oversteerIndicator");

            foreach (var s in _buffer)
            {
                sb.Append(s.TimestampSeconds.ToString(culture)).Append(',')
                  .Append(s.SpeedKph.ToString(culture)).Append(',')
                  .Append(s.SpeedMps.ToString(culture)).Append(',')
                  .Append(s.SteeringInput.ToString(culture)).Append(',')
                  .Append(s.ThrottleRaw.ToString(culture)).Append(',')
                  .Append(s.ThrottleSmoothed.ToString(culture)).Append(',')
                  .Append(s.BrakeRaw.ToString(culture)).Append(',')
                  .Append(s.BrakeSmoothed.ToString(culture)).Append(',')
                  .Append(s.RequestedYawRateDegPerSec.ToString(culture)).Append(',')
                  .Append(s.ActualYawRateDegPerSec.ToString(culture)).Append(',')
                  .Append(s.SlipAngleDegrees.ToString(culture)).Append(',')
                  .Append(s.Grip.ToString(culture)).Append(',')
                  .Append(s.LateralAccelerationMps2.ToString(culture)).Append(',')
                  .Append(s.LongitudinalAccelerationMps2.ToString(culture)).Append(',')
                  .Append(s.LateralWeightTransferRatio.ToString(culture)).Append(',')
                  .Append(s.InsideRearUnloadFactor.ToString(culture)).Append(',')
                  .Append(s.InsideRearEstimatedLoadNewtons.ToString(culture)).Append(',')
                  .Append(s.DraftFactor.ToString(culture)).Append(',')
                  .Append(s.DragForceNewtons.ToString(culture)).Append(',')
                  .Append(s.CurrentSurfaceName).Append(',')
                  .Append(FormatNullable(s.FrontSlipAngleDegrees, culture)).Append(',')
                  .Append(FormatNullable(s.RearSlipAngleDegrees, culture)).Append(',')
                  .Append(FormatNullable(s.FrontGrip, culture)).Append(',')
                  .Append(FormatNullable(s.RearGrip, culture)).Append(',')
                  .Append(FormatNullable(s.UndersteerIndicator, culture)).Append(',')
                  .Append(FormatNullable(s.OversteerIndicator, culture))
                  .AppendLine();
            }

            File.WriteAllText(fullPath, sb.ToString());
            Debug.Log($"[KartPhysicsTelemetryRecorder] Saved {_buffer.Count} samples to {fullPath}");
            return fullPath;
        }

        private static string FormatNullable(float? value, CultureInfo culture)
        {
            return value.HasValue ? value.Value.ToString(culture) : string.Empty;
        }
    }
}
