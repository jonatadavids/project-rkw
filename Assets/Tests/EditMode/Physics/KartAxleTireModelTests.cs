using NUnit.Framework;
using UnityEngine;

namespace RKW.Physics.Tests.EditMode
{
    /// <summary>
    /// Etapa 1 (2026-08-31): tests for the new per-axle tire model added to
    /// KartDynamicsMath (CalculateAxlePointVelocityWorld,
    /// CalculateAxleSlipAngleDegrees, CalculateFrontAxleSlipAngleDegrees,
    /// EvaluateAxleGripRatio). These call the real production functions --
    /// unlike ThrottleRampPropertyTest.cs and SurfaceGripPropertyTest.cs
    /// (see auditoria-fisica-kart.md P6), nothing here reimplements the
    /// formula being tested.
    /// </summary>
    public sealed class KartAxleTireModelTests
    {
        private const float MaxSteeringAngleDegrees = 28f;
        private const float LowSpeedThreshold = 0.3f;

        // ---- Teste 1: slip zero ----
        [Test]
        public void SlipAngle_MovingStraightNoSteering_IsZeroFrontAndRear()
        {
            var front = KartDynamicsMath.CalculateFrontAxleSlipAngleDegrees(
                lateralVelocityMetersPerSecond: 0f,
                longitudinalVelocityMetersPerSecond: 10f,
                steeringInput: 0f,
                maxSteeringAngleDegrees: MaxSteeringAngleDegrees,
                lowSpeedThresholdMetersPerSecond: LowSpeedThreshold);
            var rear = KartDynamicsMath.CalculateAxleSlipAngleDegrees(
                lateralVelocityMetersPerSecond: 0f,
                longitudinalVelocityMetersPerSecond: 10f,
                lowSpeedThresholdMetersPerSecond: LowSpeedThreshold);

            Assert.That(front, Is.EqualTo(0f).Within(0.01f));
            Assert.That(rear, Is.EqualTo(0f).Within(0.01f));
        }

        // ---- Teste 2: sideslip ----
        [Test]
        public void SlipAngle_WithLateralVelocity_HasCoherentSignAndMagnitude()
        {
            var front = KartDynamicsMath.CalculateFrontAxleSlipAngleDegrees(
                lateralVelocityMetersPerSecond: 2f,
                longitudinalVelocityMetersPerSecond: 10f,
                steeringInput: 0f,
                maxSteeringAngleDegrees: MaxSteeringAngleDegrees,
                lowSpeedThresholdMetersPerSecond: LowSpeedThreshold);
            var rear = KartDynamicsMath.CalculateAxleSlipAngleDegrees(
                lateralVelocityMetersPerSecond: 2f,
                longitudinalVelocityMetersPerSecond: 10f,
                lowSpeedThresholdMetersPerSecond: LowSpeedThreshold);

            Assert.That(front, Is.GreaterThan(0f), "Positive lateral velocity should give a positive slip angle.");
            Assert.That(rear, Is.GreaterThan(0f));
            // No steering applied, so with identical lateral/longitudinal
            // velocity at both axles, front and rear should agree here --
            // steering is the only thing that is supposed to tell them apart
            // (see Teste 3).
            Assert.That(front, Is.EqualTo(rear).Within(0.01f));
        }

        // ---- Teste 3: steering ----
        [Test]
        public void SteeringInput_AffectsFrontSlipOnly_NotRearSlip()
        {
            var frontNoSteer = KartDynamicsMath.CalculateFrontAxleSlipAngleDegrees(
                0f, 10f, steeringInput: 0f, MaxSteeringAngleDegrees, LowSpeedThreshold);
            var frontWithSteer = KartDynamicsMath.CalculateFrontAxleSlipAngleDegrees(
                0f, 10f, steeringInput: 0.5f, MaxSteeringAngleDegrees, LowSpeedThreshold);
            var rearNoSteer = KartDynamicsMath.CalculateAxleSlipAngleDegrees(0f, 10f, LowSpeedThreshold);

            Assert.That(frontWithSteer, Is.Not.EqualTo(frontNoSteer).Within(0.001f),
                "Front slip angle must respond to the wheel's own steer angle.");
            Assert.That(frontWithSteer, Is.EqualTo(-0.5f * MaxSteeringAngleDegrees).Within(0.01f));
            // CalculateAxleSlipAngleDegrees (used for the rear) has no
            // steeringInput parameter at all -- there is no way for the
            // rear axle to receive it directly, by construction.
            Assert.That(rearNoSteer, Is.EqualTo(0f).Within(0.01f));
        }

        // ---- Teste 4: yaw / axle point velocity ----
        [Test]
        public void AxlePointVelocity_WithAngularVelocity_DiffersBetweenFrontAndRear()
        {
            var comVelocity = new Vector3(0f, 0f, 10f);
            var angularVelocity = new Vector3(0f, 1f, 0f); // yawing, world space
            var halfWheelbase = 0.5f;

            var front = KartDynamicsMath.CalculateAxlePointVelocityWorld(
                comVelocity, angularVelocity, new Vector3(0f, 0f, halfWheelbase));
            var rear = KartDynamicsMath.CalculateAxlePointVelocityWorld(
                comVelocity, angularVelocity, new Vector3(0f, 0f, -halfWheelbase));

            Assert.That(front.x, Is.Not.EqualTo(rear.x).Within(0.001f),
                "Front and rear axle points must see different lateral velocity while the kart is yawing.");
            // Sanity: with zero angular velocity, both must collapse back to
            // the plain center-of-mass velocity (no yaw = no difference).
            var frontNoYaw = KartDynamicsMath.CalculateAxlePointVelocityWorld(
                comVelocity, Vector3.zero, new Vector3(0f, 0f, halfWheelbase));
            Assert.That(frontNoYaw, Is.EqualTo(comVelocity));
        }

        // ---- Teste 5: grip curve, front and rear ----
        [Test]
        public void AxleGripRatio_PeaksThenFallsOffProgressively_FrontAndRear()
        {
            // Note: EvaluateAxleGripRatio deliberately reuses the same
            // shortcut the legacy single-axle call site already used (full
            // grip 1.0 for any slip within the peak, only EvaluateGripCurve's
            // own falloff kicks in beyond it) -- see KartDynamicsMath and
            // auditoria-fisica-kart.md's Etapa 1 BEFORE/AFTER. That shortcut
            // means the production value does NOT rise gradually from 0 the
            // way EvaluateGripCurve's internal SmoothStep alone would; it is
            // flat at maximum up to the peak, then falls. This test asserts
            // the REAL production behavior (flat, then progressively lower),
            // not an idealized rising curve, so it stays honest about what
            // actually runs -- see the audit's own criticism (P6) of tests
            // that assert something the shipped code does not do.
            AssertAxlePeaksThenFalls(peakSlipAngleDegrees: 7f, fullLossSlipAngleDegrees: 24f, minimumGripRatio: 0.28f);
            AssertAxlePeaksThenFalls(peakSlipAngleDegrees: 8f, fullLossSlipAngleDegrees: 28f, minimumGripRatio: 0.32f);
        }

        private static void AssertAxlePeaksThenFalls(float peakSlipAngleDegrees, float fullLossSlipAngleDegrees,
            float minimumGripRatio)
        {
            for (var slip = 0f; slip <= peakSlipAngleDegrees; slip += 1f)
            {
                var grip = KartDynamicsMath.EvaluateAxleGripRatio(
                    slip, peakSlipAngleDegrees, fullLossSlipAngleDegrees, minimumGripRatio);
                Assert.That(grip, Is.EqualTo(1f).Within(0.001f),
                    $"Grip should be at maximum for any slip ({slip}) within the peak ({peakSlipAngleDegrees}).");
            }

            var previousGrip = 1f;
            for (var slip = peakSlipAngleDegrees + 1f; slip <= fullLossSlipAngleDegrees + 5f; slip += 1f)
            {
                var grip = KartDynamicsMath.EvaluateAxleGripRatio(
                    slip, peakSlipAngleDegrees, fullLossSlipAngleDegrees, minimumGripRatio);
                Assert.That(grip, Is.LessThanOrEqualTo(previousGrip + 0.0001f),
                    $"Grip must fall off progressively beyond the peak (slip={slip}).");
                Assert.That(grip, Is.GreaterThanOrEqualTo(minimumGripRatio - 0.0001f));
                previousGrip = grip;
            }
        }

        // ---- Teste 6: low speed stability ----
        [Test]
        public void SlipAngle_NearZeroSpeed_NeverProducesNaNInfinityOrOscillation()
        {
            var random = new System.Random(2026);
            for (var i = 0; i < 200; i++)
            {
                var lateral = RandomFloat(random, -0.05f, 0.05f);
                var longitudinal = RandomFloat(random, -0.05f, 0.05f);
                var steering = RandomFloat(random, -1f, 1f);

                var front = KartDynamicsMath.CalculateFrontAxleSlipAngleDegrees(
                    lateral, longitudinal, steering, MaxSteeringAngleDegrees, LowSpeedThreshold);
                var rear = KartDynamicsMath.CalculateAxleSlipAngleDegrees(lateral, longitudinal, LowSpeedThreshold);

                Assert.That(float.IsNaN(front), Is.False, $"iteration {i}: front NaN");
                Assert.That(float.IsInfinity(front), Is.False, $"iteration {i}: front Infinity");
                Assert.That(float.IsNaN(rear), Is.False, $"iteration {i}: rear NaN");
                Assert.That(float.IsInfinity(rear), Is.False, $"iteration {i}: rear Infinity");
                // Below the low-speed threshold both are pinned to exactly 0
                // -- the whole point of the gate is to remove oscillation
                // from velocity noise this small, not just bound it.
                Assert.That(front, Is.EqualTo(0f).Within(0.001f), $"iteration {i}");
                Assert.That(rear, Is.EqualTo(0f).Within(0.001f), $"iteration {i}");
            }

            // Exactly at rest (0,0) must not throw or produce anything odd either.
            var atRestFront = KartDynamicsMath.CalculateFrontAxleSlipAngleDegrees(0f, 0f, 1f, MaxSteeringAngleDegrees, LowSpeedThreshold);
            Assert.That(float.IsFinite(atRestFront), Is.True);
            Assert.That(atRestFront, Is.EqualTo(0f).Within(0.001f));
        }

        // ---- Teste 7: left/right symmetry ----
        [Test]
        public void SlipAngleAndGrip_LeftVsRightCurve_AreSymmetric()
        {
            const float lateral = 3f;
            const float longitudinal = 12f;
            const float steering = 0.6f;

            var frontRight = KartDynamicsMath.CalculateFrontAxleSlipAngleDegrees(
                lateral, longitudinal, steering, MaxSteeringAngleDegrees, LowSpeedThreshold);
            var frontLeft = KartDynamicsMath.CalculateFrontAxleSlipAngleDegrees(
                -lateral, longitudinal, -steering, MaxSteeringAngleDegrees, LowSpeedThreshold);
            var rearRight = KartDynamicsMath.CalculateAxleSlipAngleDegrees(lateral, longitudinal, LowSpeedThreshold);
            var rearLeft = KartDynamicsMath.CalculateAxleSlipAngleDegrees(-lateral, longitudinal, LowSpeedThreshold);

            Assert.That(frontLeft, Is.EqualTo(-frontRight).Within(0.01f));
            Assert.That(rearLeft, Is.EqualTo(-rearRight).Within(0.01f));

            var gripRight = KartDynamicsMath.EvaluateAxleGripRatio(frontRight, 7f, 24f, 0.28f);
            var gripLeft = KartDynamicsMath.EvaluateAxleGripRatio(frontLeft, 7f, 24f, 0.28f);
            Assert.That(gripLeft, Is.EqualTo(gripRight).Within(0.0001f),
                "Grip depends only on |slip angle|, so mirrored left/right must give identical grip.");
        }

        private static float RandomFloat(System.Random random, float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }
    }
}
