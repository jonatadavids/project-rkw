using NUnit.Framework;

namespace RKW.Physics.Tests.EditMode
{
    /// <summary>
    /// Touch control layout feel pass (founder playtest feedback,
    /// 2026-08-19): side-by-side pedals and a rotating steering wheel.
    /// </summary>
    public sealed class KartInputLayoutMathTests
    {
        [Test]
        public void WheelRotation_NoSteering_IsZero()
        {
            var degrees = KartInputLayoutMath.CalculateSteeringWheelRotationDegrees(0f, 90f);

            Assert.That(degrees, Is.EqualTo(0f));
        }

        [Test]
        public void WheelRotation_FullRightSteering_IsMaxPositive()
        {
            var degrees = KartInputLayoutMath.CalculateSteeringWheelRotationDegrees(1f, 90f);

            Assert.That(degrees, Is.EqualTo(90f).Within(0.001f));
        }

        [Test]
        public void WheelRotation_FullLeftSteering_IsMaxNegative()
        {
            var degrees = KartInputLayoutMath.CalculateSteeringWheelRotationDegrees(-1f, 90f);

            Assert.That(degrees, Is.EqualTo(-90f).Within(0.001f));
        }

        [Test]
        public void WheelRotation_ClampsOutOfRangeInput()
        {
            var degrees = KartInputLayoutMath.CalculateSteeringWheelRotationDegrees(3f, 90f);

            Assert.That(degrees, Is.EqualTo(90f).Within(0.001f));
        }

        [Test]
        public void IsBrakeSide_LeftHalfOfRightZone_IsTrue()
        {
            var isBrake = KartInputLayoutMath.IsBrakeSide(touchX: 620f, rightZoneStartX: 600f, rightZoneWidth: 400f);

            Assert.That(isBrake, Is.True);
        }

        [Test]
        public void IsBrakeSide_RightHalfOfRightZone_IsFalse()
        {
            var isBrake = KartInputLayoutMath.IsBrakeSide(touchX: 950f, rightZoneStartX: 600f, rightZoneWidth: 400f);

            Assert.That(isBrake, Is.False);
        }

        [Test]
        public void IsBrakeSide_ExactMidpoint_IsThrottleSide()
        {
            // relativeX == 0.5 is not < 0.5, so the midpoint belongs to throttle.
            var isBrake = KartInputLayoutMath.IsBrakeSide(touchX: 800f, rightZoneStartX: 600f, rightZoneWidth: 400f);

            Assert.That(isBrake, Is.False);
        }

        [Test]
        public void IsBrakeSide_ZeroWidthZone_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => KartInputLayoutMath.IsBrakeSide(100f, 100f, 0f));
        }
    }
}
