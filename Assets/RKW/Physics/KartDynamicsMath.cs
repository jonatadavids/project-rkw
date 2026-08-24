using UnityEngine;

namespace RKW.Physics
{
    /// <summary>Pure, deterministic helpers used by the first simcade kart prototype.</summary>
    public static class KartDynamicsMath
    {
        public const float Gravity = 9.81f;

        public static float EvaluateGripCurve(
            float slipAngleDegrees,
            float peakSlipAngleDegrees,
            float fullLossSlipAngleDegrees,
            float minimumGripRatio)
        {
            var slip = Mathf.Abs(slipAngleDegrees);
            var peak = Mathf.Max(0.01f, peakSlipAngleDegrees);
            var loss = Mathf.Max(peak + 0.01f, fullLossSlipAngleDegrees);
            var minimum = Mathf.Clamp01(minimumGripRatio);

            if (slip <= peak)
            {
                var normalized = slip / peak;
                return Mathf.SmoothStep(0f, 1f, normalized);
            }

            var falloff = Mathf.InverseLerp(peak, loss, slip);
            return Mathf.Lerp(1f, minimum, Mathf.SmoothStep(0f, 1f, falloff));
        }

        public static float CalculateLateralWeightTransferRatio(
            float speedMetersPerSecond,
            float steeringMagnitude,
            float maxSpeedMetersPerSecond,
            float centerOfMassHeightMeters,
            float rearTrackWidthMeters,
            float transferGain)
        {
            var speedRatio = Mathf.Clamp01(Mathf.Abs(speedMetersPerSecond) /
                                            Mathf.Max(0.01f, maxSpeedMetersPerSecond));
            var geometricRatio = Mathf.Max(0f, centerOfMassHeightMeters) /
                                 Mathf.Max(0.01f, rearTrackWidthMeters);
            return Mathf.Clamp01(
                speedRatio * speedRatio * Mathf.Clamp01(Mathf.Abs(steeringMagnitude)) *
                geometricRatio * Mathf.Max(0f, transferGain));
        }

        public static float CalculateInnerRearLift(float transferRatio, float liftThreshold)
        {
            return Mathf.InverseLerp(Mathf.Clamp01(liftThreshold), 1f, Mathf.Clamp01(transferRatio));
        }

        public static float CalculateAccelerationMetersPerSecondSquared(
            float speedMetersPerSecond,
            float maxSpeedMetersPerSecond,
            float zeroToMaxSeconds)
        {
            var maximum = Mathf.Max(0.01f, maxSpeedMetersPerSecond);
            var baseAcceleration = maximum / Mathf.Max(0.01f, zeroToMaxSeconds);
            var speedRatio = Mathf.Clamp01(Mathf.Abs(speedMetersPerSecond) / maximum);
            return baseAcceleration * (1f - speedRatio * speedRatio);
        }

        /// <summary>
        /// Calculates slipstream drag reduction based on distance to leading kart.
        /// Closer = more reduction (monotonically decreasing with distance).
        /// Returns value in [0, maxReduction].
        /// </summary>
        public static float CalculateSlipstreamDragReduction(
            float distanceToLeader,
            float kartLength,
            float maxActivationDistance,
            float maxReduction,
            float minimumTimeInSlipstream,
            float timeInSlipstream)
        {
            if (timeInSlipstream < minimumTimeInSlipstream)
            {
                return 0f;
            }

            var maxDist = Mathf.Max(0.01f, maxActivationDistance * kartLength);
            if (distanceToLeader > maxDist || distanceToLeader <= 0f)
            {
                return 0f;
            }

            // Closer = more reduction (linear interpolation, clamped)
            var normalizedDistance = distanceToLeader / maxDist;
            var reduction = Mathf.Lerp(maxReduction, 0f, normalizedDistance);
            return Mathf.Clamp(reduction, 0f, maxReduction);
        }

        /// <summary>
        /// Calculates effective braking deceleration considering rear-biased distribution,
        /// steering-induced oversteer tendency, and wheel lock threshold.
        /// </summary>
        public static void CalculateBrakingWithSteering(
            float brakeInput,
            float steeringMagnitude,
            float speedMetersPerSecond,
            float maxBrakeDeceleration,
            float rearBrakeDistribution,
            float currentGripRatio,
            float lateralGripG,
            float brakeOversteerGain,
            out float effectiveDeceleration,
            out float oversteerFactor)
        {
            brakeInput = Mathf.Clamp01(brakeInput);
            steeringMagnitude = Mathf.Clamp01(Mathf.Abs(steeringMagnitude));
            rearBrakeDistribution = Mathf.Clamp(rearBrakeDistribution, 0.5f, 1f);

            // Base brake force
            var requestedDeceleration = maxBrakeDeceleration * brakeInput;

            // Lock threshold: max braking = available grip * g
            var availableGrip = Mathf.Max(0.01f, currentGripRatio) * lateralGripG * Gravity;
            var lockRatio = requestedDeceleration / Mathf.Max(0.01f, availableGrip);

            // If braking exceeds grip, reduce effectiveness (wheel lock)
            effectiveDeceleration = lockRatio > 1f
                ? requestedDeceleration * (1f / lockRatio) * 0.85f // locked wheels = less braking
                : requestedDeceleration;

            // Oversteer factor: braking with steering causes rear to slide
            oversteerFactor = steeringMagnitude * brakeInput * rearBrakeDistribution *
                              Mathf.Max(0f, brakeOversteerGain);

            // Straight-line braking is always more effective (no oversteer penalty)
            if (steeringMagnitude > 0.01f)
            {
                var steeringPenalty = 1f - steeringMagnitude * 0.15f;
                effectiveDeceleration *= Mathf.Max(0.7f, steeringPenalty);
            }
        }

        public static float CalculateSteeringSpeedLoss(
            float steeringMagnitude,
            float speedMetersPerSecond,
            float maxSpeedMetersPerSecond,
            float maximumLossAcceleration)
        {
            var speedRatio = Mathf.Clamp01(Mathf.Abs(speedMetersPerSecond) /
                                            Mathf.Max(0.01f, maxSpeedMetersPerSecond));
            return Mathf.Clamp01(Mathf.Abs(steeringMagnitude)) * speedRatio * speedRatio *
                   Mathf.Max(0f, maximumLossAcceleration);
        }

        /// <summary>
        /// Founder playtest feedback, 2026-08-20 (round 10): "o kart quase
        /// nao faz curva mesmo forcando... sai deslizando de lado ate bater
        /// na parede... o carrinho gira 180 graus... falta ser mais gostoso
        /// a direcao se assemelhar mais um kart". The old steering model
        /// commanded a yaw (rotation) rate directly from a fixed
        /// degrees-per-second cap, independent of how fast the kart was
        /// actually moving — at real driving speeds that implied a turn
        /// radius no real kart's tires could hold (a 105-120 deg/s yaw rate
        /// at 70 km/h asks for several G of lateral grip), so the nose spun
        /// far faster than the body's momentum could follow and the two
        /// diverged into a slide or a spin. This is how an actual kart's
        /// front wheels work: turning them by some angle sets a turn
        /// RADIUS (Ackermann geometry: radius = wheelbase / tan(angle)),
        /// and the yaw rate you get from that radius is simply how fast
        /// your current speed carries you around it (yawRate = speed /
        /// radius). Speed scales how quickly you sweep the corner, not how
        /// tight the corner is — matching how a kart driver actually
        /// experiences cornering (the same steering angle gives the same
        /// line at 20 or 40 km/h, just swept faster). Signed speed also
        /// means reversing naturally flips the felt steering direction —
        /// the same as backing up a real kart — with no separate
        /// direction flag needed.
        /// </summary>
        public static float CalculateAckermannYawRateDegreesPerSecond(
            float steeringInput,
            float forwardSpeedMetersPerSecond,
            float wheelbaseMeters,
            float maxSteeringAngleDegrees)
        {
            var steerAngleRadians = Mathf.Clamp(steeringInput, -1f, 1f) *
                                     Mathf.Max(1f, maxSteeringAngleDegrees) * Mathf.Deg2Rad;
            if (Mathf.Abs(steerAngleRadians) < 0.0001f)
            {
                return 0f;
            }

            var wheelbase = Mathf.Max(0.1f, wheelbaseMeters);
            return forwardSpeedMetersPerSecond * Mathf.Tan(steerAngleRadians) / wheelbase * Mathf.Rad2Deg;
        }

        /// <summary>
        /// Caps a requested yaw rate so the resulting turn never demands
        /// more centripetal acceleration (speed * yawRate) than the tires
        /// can currently supply. This is the real-world reason pushing a
        /// kart beyond its grip WIDENS the line (understeer) instead of
        /// spinning — the fix for the round-10 "gira 180 graus" complaint:
        /// once <see cref="CalculateAckermannYawRateDegreesPerSecond"/>
        /// asks for more curvature than available grip allows, this scales
        /// the request down proportionally rather than letting the yaw
        /// controller keep spinning the nose past what the body can
        /// physically follow. Skipped below
        /// <paramref name="minSpeedForLimitMetersPerSecond"/> — at a crawl
        /// the required centripetal force is negligible anyway, and
        /// applying the limit there would only fight normal low-speed
        /// maneuvering for no benefit.
        /// </summary>
        public static float LimitYawRateToAvailableGrip(
            float requestedYawRateDegreesPerSecond,
            float forwardSpeedMetersPerSecond,
            float maxLateralAcceleration,
            float minSpeedForLimitMetersPerSecond = 1.5f)
        {
            var speed = Mathf.Abs(forwardSpeedMetersPerSecond);
            if (speed < Mathf.Max(0f, minSpeedForLimitMetersPerSecond) || maxLateralAcceleration <= 0f)
            {
                return requestedYawRateDegreesPerSecond;
            }

            var requestedYawRateRadPerSec = requestedYawRateDegreesPerSecond * Mathf.Deg2Rad;
            var requiredLateralAcceleration = Mathf.Abs(speed * requestedYawRateRadPerSec);
            if (requiredLateralAcceleration <= maxLateralAcceleration)
            {
                return requestedYawRateDegreesPerSecond;
            }

            var scale = maxLateralAcceleration / requiredLateralAcceleration;
            return requestedYawRateDegreesPerSecond * scale;
        }
    }
}
