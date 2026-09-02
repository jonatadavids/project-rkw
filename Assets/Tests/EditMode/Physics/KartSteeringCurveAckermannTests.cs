using NUnit.Framework;
using RKW.Physics;
using UnityEngine;

namespace RKW.Physics.Tests.EditMode
{
    /// <summary>
    /// Etapa 4 (2026-08-31): steering response curve and per-wheel
    /// Ackermann visual angle math. Both are pure functions in
    /// KartDynamicsMath -- see KartDynamics.SetInput (curve) and
    /// KartSteeringVisual (Ackermann angles) for where production code
    /// calls them.
    /// </summary>
    public sealed class KartSteeringCurveAckermannTests
    {
        [Test]
        public void SteeringCurve_ExponentOne_IsIdentity()
        {
            foreach (var input in new[] { -1f, -0.5f, -0.2f, 0f, 0.2f, 0.5f, 1f })
            {
                var curved = KartDynamicsMath.ApplySteeringResponseCurve(input, 1f);
                Assert.That(curved, Is.EqualTo(input).Within(0.0001f),
                    $"input={input}: exponent 1 must be an exact identity (every pre-Etapa-4 asset relies on this).");
            }
        }

        [Test]
        public void SteeringCurve_PreservesSign()
        {
            Assert.That(KartDynamicsMath.ApplySteeringResponseCurve(0.4f, 2f), Is.GreaterThan(0f));
            Assert.That(KartDynamicsMath.ApplySteeringResponseCurve(-0.4f, 2f), Is.LessThan(0f));
            Assert.That(KartDynamicsMath.ApplySteeringResponseCurve(0f, 2f), Is.EqualTo(0f));
        }

        [Test]
        public void SteeringCurve_ExponentGreaterThanOne_ReducesSmallInputsMoreThanLarge()
        {
            var smallCurved = KartDynamicsMath.ApplySteeringResponseCurve(0.2f, 2f);
            var largeCurved = KartDynamicsMath.ApplySteeringResponseCurve(0.9f, 2f);

            // 0.2^2 = 0.04 (a much bigger relative reduction than 0.9^2 = 0.81)
            Assert.That(smallCurved / 0.2f, Is.LessThan(largeCurved / 0.9f),
                "A >1 exponent should shrink small inputs proportionally more than large ones (finer center control).");
        }

        [Test]
        public void SteeringCurve_EndpointsAlwaysReachFullRange_RegardlessOfExponent()
        {
            foreach (var exponent in new[] { 0.3f, 1f, 2f, 3f })
            {
                Assert.That(KartDynamicsMath.ApplySteeringResponseCurve(1f, exponent), Is.EqualTo(1f).Within(0.0001f));
                Assert.That(KartDynamicsMath.ApplySteeringResponseCurve(-1f, exponent), Is.EqualTo(-1f).Within(0.0001f));
            }
        }

        [Test]
        public void SteeringCurve_ClampsOutOfRangeInput()
        {
            Assert.That(KartDynamicsMath.ApplySteeringResponseCurve(5f, 1f), Is.EqualTo(1f).Within(0.0001f));
            Assert.That(KartDynamicsMath.ApplySteeringResponseCurve(-5f, 1f), Is.EqualTo(-1f).Within(0.0001f));
        }

        [Test]
        public void AckermannWheelAngles_NearStraight_BothZero()
        {
            KartDynamicsMath.CalculateAckermannWheelAnglesDegrees(0f, 1.05f, 1.05f, out var inner, out var outer);
            Assert.That(inner, Is.EqualTo(0f));
            Assert.That(outer, Is.EqualTo(0f));
        }

        [Test]
        public void AckermannWheelAngles_Turning_InnerIsSharperThanOuter()
        {
            KartDynamicsMath.CalculateAckermannWheelAnglesDegrees(20f, 1.05f, 1.05f, out var inner, out var outer);
            Assert.That(Mathf.Abs(inner), Is.GreaterThan(Mathf.Abs(outer)),
                "The inside wheel of a turn must point sharper than the outside wheel (real Ackermann geometry).");
        }

        [Test]
        public void AckermannWheelAngles_SignMatchesCentralAngleDirection()
        {
            KartDynamicsMath.CalculateAckermannWheelAnglesDegrees(20f, 1.05f, 1.05f, out var innerRight, out var outerRight);
            KartDynamicsMath.CalculateAckermannWheelAnglesDegrees(-20f, 1.05f, 1.05f, out var innerLeft, out var outerLeft);

            Assert.That(innerRight, Is.GreaterThan(0f));
            Assert.That(outerRight, Is.GreaterThan(0f));
            Assert.That(innerLeft, Is.LessThan(0f));
            Assert.That(outerLeft, Is.LessThan(0f));
            Assert.That(Mathf.Abs(innerLeft), Is.EqualTo(Mathf.Abs(innerRight)).Within(0.0001f),
                "Left and right turns of the same magnitude should be mirror images.");
        }

        [Test]
        public void AckermannWheelAngles_NeverNaNOrInfinite_AcrossRealisticRange()
        {
            for (var angle = -35f; angle <= 35f; angle += 2.5f)
            {
                KartDynamicsMath.CalculateAckermannWheelAnglesDegrees(angle, 1.05f, 1.05f, out var inner, out var outer);
                Assert.That(float.IsFinite(inner), Is.True, $"angle={angle}: inner non-finite");
                Assert.That(float.IsFinite(outer), Is.True, $"angle={angle}: outer non-finite");
            }
        }
    }
}
