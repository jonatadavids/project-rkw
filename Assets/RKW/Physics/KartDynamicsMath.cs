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
    }
}
