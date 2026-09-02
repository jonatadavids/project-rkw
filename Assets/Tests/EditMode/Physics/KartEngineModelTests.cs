using NUnit.Framework;
using RKW.Physics;
using UnityEditor;
using UnityEngine;

namespace RKW.Physics.Tests.EditMode
{
    /// <summary>
    /// Etapa 10 (2026-08-31) pure-math coverage for the opt-in RPM/torque
    /// engine model. EngineRPM itself is always active (see
    /// KartDynamics.EngineRPM); the torque-curve acceleration formula is
    /// opt-in per asset (KartCategorySO.UseTorqueCurveEngineModel, default
    /// false) -- see KartEngineModelIntegrationTests (PlayMode) for the
    /// live-kart RPM coverage.
    /// </summary>
    public sealed class KartEngineModelTests
    {
        [Test]
        public void EngineRPM_AtZeroSpeed_ClampsToIdle()
        {
            var rpm = KartDynamicsMath.CalculateEngineRPM(0f, 6f, 0.139f, 1800f, 10000f);
            Assert.That(rpm, Is.EqualTo(1800f).Within(0.01f));
        }

        [Test]
        public void EngineRPM_IncreasesWithWheelSpeed()
        {
            var atLow = KartDynamicsMath.CalculateEngineRPM(3f, 6f, 0.139f, 1800f, 10000f);
            var atHigh = KartDynamicsMath.CalculateEngineRPM(15f, 6f, 0.139f, 1800f, 10000f);
            Assert.That(atHigh, Is.GreaterThan(atLow));
        }

        [Test]
        public void EngineRPM_ClampsAtRedline_ForExtremeSpeed()
        {
            var rpm = KartDynamicsMath.CalculateEngineRPM(200f, 6f, 0.139f, 1800f, 10000f);
            Assert.That(rpm, Is.EqualTo(10000f).Within(0.01f));
        }

        [Test]
        public void EngineRPM_IgnoresDirection_UsesAbsoluteSpeed()
        {
            var forward = KartDynamicsMath.CalculateEngineRPM(10f, 6f, 0.139f, 1800f, 10000f);
            var reverse = KartDynamicsMath.CalculateEngineRPM(-10f, 6f, 0.139f, 1800f, 10000f);
            Assert.That(forward, Is.EqualTo(reverse).Within(0.01f));
        }

        [Test]
        public void EngineRPM_NeverNaNOrInfinite_AcrossRealisticSpeedRange()
        {
            for (var speed = -30f; speed <= 30f; speed += 2f)
            {
                var rpm = KartDynamicsMath.CalculateEngineRPM(speed, 6f, 0.139f, 1800f, 10000f);
                Assert.That(float.IsFinite(rpm), Is.True, $"speed={speed}: RPM non-finite");
            }
        }

        [Test]
        public void TorqueCurve_MatchesControlPointsExactly_AtBreakpoints()
        {
            const float idle = 9f, lowMid = 16f, highMid = 14f, redline = 8f;
            Assert.That(KartDynamicsMath.EvaluateTorqueCurveNewtonMeters(0f, idle, lowMid, highMid, redline), Is.EqualTo(idle).Within(0.001f));
            Assert.That(KartDynamicsMath.EvaluateTorqueCurveNewtonMeters(0.33f, idle, lowMid, highMid, redline), Is.EqualTo(lowMid).Within(0.001f));
            Assert.That(KartDynamicsMath.EvaluateTorqueCurveNewtonMeters(0.66f, idle, lowMid, highMid, redline), Is.EqualTo(highMid).Within(0.001f));
            Assert.That(KartDynamicsMath.EvaluateTorqueCurveNewtonMeters(1f, idle, lowMid, highMid, redline), Is.EqualTo(redline).Within(0.001f));
        }

        [Test]
        public void TorqueCurve_DefaultShape_RisesThenFalls()
        {
            const float idle = 9f, lowMid = 16f, highMid = 14f, redline = 8f;
            var atIdle = KartDynamicsMath.EvaluateTorqueCurveNewtonMeters(0f, idle, lowMid, highMid, redline);
            var atPeak = KartDynamicsMath.EvaluateTorqueCurveNewtonMeters(0.33f, idle, lowMid, highMid, redline);
            var atRedline = KartDynamicsMath.EvaluateTorqueCurveNewtonMeters(1f, idle, lowMid, highMid, redline);

            Assert.That(atPeak, Is.GreaterThan(atIdle), "A typical kart engine's torque should rise from idle.");
            Assert.That(atRedline, Is.LessThan(atPeak), "A typical kart engine's torque should fall off toward redline.");
        }

        [Test]
        public void TorqueCurve_ClampsOutOfRangeNormalizedRPM()
        {
            const float idle = 9f, lowMid = 16f, highMid = 14f, redline = 8f;
            Assert.That(KartDynamicsMath.EvaluateTorqueCurveNewtonMeters(-1f, idle, lowMid, highMid, redline), Is.EqualTo(idle).Within(0.001f));
            Assert.That(KartDynamicsMath.EvaluateTorqueCurveNewtonMeters(2f, idle, lowMid, highMid, redline), Is.EqualTo(redline).Within(0.001f));
        }

        [Test]
        public void TorqueCurveAcceleration_IsPositive_WithPositiveTorque()
        {
            var accel = KartDynamicsMath.CalculateTorqueCurveAccelerationMetersPerSecondSquared(
                5000f, 9500f, 9f, 16f, 14f, 8f, 6f, 0.139f, 170f);
            Assert.That(accel, Is.GreaterThan(0f));
            Assert.That(float.IsFinite(accel), Is.True);
        }

        [Test]
        public void TorqueCurveAcceleration_HalvingMass_DoublesAcceleration()
        {
            var accelFull = KartDynamicsMath.CalculateTorqueCurveAccelerationMetersPerSecondSquared(
                5000f, 9500f, 9f, 16f, 14f, 8f, 6f, 0.139f, 170f);
            var accelHalfMass = KartDynamicsMath.CalculateTorqueCurveAccelerationMetersPerSecondSquared(
                5000f, 9500f, 9f, 16f, 14f, 8f, 6f, 0.139f, 85f);
            Assert.That(accelHalfMass, Is.EqualTo(accelFull * 2f).Within(0.01f));
        }

        [Test]
        public void TorqueCurveAcceleration_HigherFinalDrive_ProducesMoreWheelForce()
        {
            var accelLowGear = KartDynamicsMath.CalculateTorqueCurveAccelerationMetersPerSecondSquared(
                5000f, 9500f, 9f, 16f, 14f, 8f, 4f, 0.139f, 170f);
            var accelHighGear = KartDynamicsMath.CalculateTorqueCurveAccelerationMetersPerSecondSquared(
                5000f, 9500f, 9f, 16f, 14f, 8f, 8f, 0.139f, 170f);
            Assert.That(accelHighGear, Is.GreaterThan(accelLowGear),
                "A higher final drive ratio should produce more wheel force (and hence acceleration) for the same engine torque.");
        }

        private static KartCategorySO CreateTuningWithEngineRpmRange(float idle, float max, float redline)
        {
            var so = ScriptableObject.CreateInstance<KartCategorySO>();
            var serialized = new SerializedObject(so);
            serialized.FindProperty("engineIdleRPM").floatValue = idle;
            serialized.FindProperty("engineMaxRPM").floatValue = max;
            serialized.FindProperty("engineRedlineRPM").floatValue = redline;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return so;
        }

        [Test]
        public void IsValid_RejectsRedlineBelowMaxRPM()
        {
            var tuning = CreateTuningWithEngineRpmRange(1800f, 9500f, 9000f);
            Assert.That(tuning.IsValid(out var reason), Is.False);
            Assert.That(reason, Does.Contain("redline"));
            Object.DestroyImmediate(tuning);
        }

        [Test]
        public void IsValid_RejectsMaxRpmAtOrBelowIdle()
        {
            var tuning = CreateTuningWithEngineRpmRange(5000f, 5000f, 9500f);
            Assert.That(tuning.IsValid(out var reason), Is.False);
            Assert.That(reason, Does.Contain("idle"));
            Object.DestroyImmediate(tuning);
        }

        [Test]
        public void IsValid_AcceptsDefaultEngineRpmRange()
        {
            var tuning = CreateTuningWithEngineRpmRange(1800f, 9500f, 10000f);
            Assert.That(tuning.IsValid(out _), Is.True);
            Object.DestroyImmediate(tuning);
        }
    }
}
