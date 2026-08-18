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
