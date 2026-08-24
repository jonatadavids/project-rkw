using NUnit.Framework;
using UnityEngine;

namespace RKW.Physics.Tests.EditMode
{
    /// <summary>
    /// Founder playtest feedback, 2026-08-20 (round 10): "o kart quase nao
    /// faz curva... sai deslizando de lado ate bater na parede... gira 180
    /// graus". Covers the two pure functions added to fix the steering
    /// model (see KartDynamicsMath and KartDynamics.ApplySteering for the
    /// full reasoning) — Ackermann-geometry yaw rate, and capping that
    /// request to whatever lateral grip is actually available.
    /// </summary>
    public sealed class KartDynamicsMathTests
    {
        [Test]
        public void AckermannYawRate_ZeroSteering_IsZero()
        {
            var yawRate = KartDynamicsMath.CalculateAckermannYawRateDegreesPerSecond(
                0f, 15f, 1.05f, 28f);

            Assert.That(yawRate, Is.EqualTo(0f));
        }

        [Test]
        public void AckermannYawRate_Stationary_IsZero()
        {
            var yawRate = KartDynamicsMath.CalculateAckermannYawRateDegreesPerSecond(
                1f, 0f, 1.05f, 28f);

            Assert.That(yawRate, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void AckermannYawRate_PositiveSteeringForward_IsPositive()
        {
            var yawRate = KartDynamicsMath.CalculateAckermannYawRateDegreesPerSecond(
                1f, 10f, 1.05f, 28f);

            Assert.That(yawRate, Is.GreaterThan(0f));
        }

        [Test]
        public void AckermannYawRate_ReversingFlipsSign()
        {
            // Founder playtest note: reversing should feel like a real
            // kart backing up — the same wheel angle swings the nose the
            // opposite way once speed is negative, with no separate
            // direction flag needed (unlike the old speedRatio-based model).
            var forward = KartDynamicsMath.CalculateAckermannYawRateDegreesPerSecond(1f, 10f, 1.05f, 28f);
            var reverse = KartDynamicsMath.CalculateAckermannYawRateDegreesPerSecond(1f, -10f, 1.05f, 28f);

            Assert.That(Mathf.Sign(reverse), Is.EqualTo(-Mathf.Sign(forward)));
            Assert.That(Mathf.Abs(reverse), Is.EqualTo(Mathf.Abs(forward)).Within(0.01f));
        }

        [Test]
        public void AckermannYawRate_ScalesLinearlyWithSpeed()
        {
            // Same steering angle -> same turn RADIUS regardless of speed;
            // yaw rate (how fast that radius gets swept) should scale
            // directly with speed. This is the crux of the founder's "o
            // carro deveria fazer a mesma curva a 20 ou a 40 km/h" feel.
            var slow = KartDynamicsMath.CalculateAckermannYawRateDegreesPerSecond(0.6f, 5f, 1.05f, 28f);
            var fast = KartDynamicsMath.CalculateAckermannYawRateDegreesPerSecond(0.6f, 10f, 1.05f, 28f);

            Assert.That(fast, Is.EqualTo(slow * 2f).Within(0.01f));
        }

        [Test]
        public void AckermannYawRate_MoreSteeringInput_IsSharperTurn()
        {
            var gentle = KartDynamicsMath.CalculateAckermannYawRateDegreesPerSecond(0.3f, 10f, 1.05f, 28f);
            var sharp = KartDynamicsMath.CalculateAckermannYawRateDegreesPerSecond(1f, 10f, 1.05f, 28f);

            Assert.That(Mathf.Abs(sharp), Is.GreaterThan(Mathf.Abs(gentle)));
        }

        [Test]
        public void LimitYawRate_WithinAvailableGrip_IsUnchanged()
        {
            // A gentle request that clearly doesn't need much lateral
            // accel should pass through untouched.
            var limited = KartDynamicsMath.LimitYawRateToAvailableGrip(
                requestedYawRateDegreesPerSecond: 20f,
                forwardSpeedMetersPerSecond: 5f,
                maxLateralAcceleration: 20f);

            Assert.That(limited, Is.EqualTo(20f).Within(0.001f));
        }

        [Test]
        public void LimitYawRate_ExceedsGrip_IsScaledDown()
        {
            // Founder playtest feedback: pushing beyond available grip
            // should widen the line (understeer), not spin the nose past
            // what the body can follow — so an over-demanding request must
            // come back smaller, same sign, never amplified.
            var requested = 200f;
            var limited = KartDynamicsMath.LimitYawRateToAvailableGrip(
                requestedYawRateDegreesPerSecond: requested,
                forwardSpeedMetersPerSecond: 20f,
                maxLateralAcceleration: 10f);

            Assert.That(limited, Is.LessThan(requested));
            Assert.That(Mathf.Sign(limited), Is.EqualTo(Mathf.Sign(requested)));
        }

        [Test]
        public void LimitYawRate_ResultNeverExceedsAvailableGrip()
        {
            var random = new System.Random(2026);
            for (var i = 0; i < 200; i++)
            {
                var requested = (float)(random.NextDouble() * 400f - 200f);
                var speed = (float)(random.NextDouble() * 30f + 2f); // above the low-speed skip
                var maxAccel = (float)(random.NextDouble() * 15f + 0.5f);

                var limited = KartDynamicsMath.LimitYawRateToAvailableGrip(requested, speed, maxAccel);
                var requiredAccel = Mathf.Abs(speed * limited * Mathf.Deg2Rad);

                Assert.That(requiredAccel, Is.LessThanOrEqualTo(maxAccel + 0.01f),
                    $"iteration {i}: requested={requested}, speed={speed}, maxAccel={maxAccel}, limited={limited}");
            }
        }

        [Test]
        public void LimitYawRate_BelowMinimumSpeed_SkipsLimit()
        {
            // At a crawl the centripetal demand is negligible either way —
            // the low-speed skip exists so the limiter never fights normal
            // slow-speed maneuvering (parking, creeping through the grid).
            var limited = KartDynamicsMath.LimitYawRateToAvailableGrip(
                requestedYawRateDegreesPerSecond: 500f,
                forwardSpeedMetersPerSecond: 0.5f,
                maxLateralAcceleration: 1f,
                minSpeedForLimitMetersPerSecond: 1.5f);

            Assert.That(limited, Is.EqualTo(500f));
        }
    }
}
