using UnityEngine;

namespace RKW.Physics
{
    /// <summary>
    /// Etapa 8 (2026-08-31): pure, deterministic helpers for the driving
    /// assists layer. Every function here operates ONLY on the -1..1/0..1
    /// INPUT values a driver (or KartPrototypeInput on their behalf) would
    /// otherwise send straight to KartDynamics.SetInput -- none of them
    /// touch KartDynamics' internals, tuning, or physics formulas. This is
    /// the "input-layer only" requirement from the Etapa 8 spec: an assist
    /// can only reshape what the player already asked for (usually by
    /// REDUCING or smoothing it), never grant the kart more grip, power, or
    /// speed than the SAME raw input would already produce unassisted. See
    /// KartAssistController for how these are wired into the actual input
    /// pipeline, and each function's own doc comment for why it can never
    /// make a kart faster than good unassisted driving.
    /// </summary>
    public static class KartAssistMath
    {
        /// <summary>
        /// SteeringAssist: softens steering magnitude at higher speed, so a
        /// nervous beginner input at speed does not produce as sharp a
        /// direction change as the same input would at a crawl. Strictly a
        /// magnitude REDUCTION (multiplies by a factor strictly greater than 0 and at most 1) -- a
        /// skilled driver feeding the exact same raw input gets the exact
        /// same result with the assist off, so this can only ever be
        /// slower/gentler, never faster.
        /// </summary>
        public static float ApplySteeringAssist(
            float rawSteering, float speedRatio01, float maxReductionAtTopSpeed01)
        {
            var speed = Mathf.Clamp01(speedRatio01);
            var reduction = 1f - Mathf.Clamp01(maxReductionAtTopSpeed01) * speed;
            return rawSteering * reduction;
        }

        /// <summary>
        /// ThrottleAssist: eases throttle input down once the rear axle's
        /// combined (lateral+longitudinal) grip usage is already high,
        /// giving a beginner a little more margin before a full-throttle
        /// input actually breaks rear traction. Only ever reduces the
        /// requested throttle (never boosts it) and only once usage is
        /// already past <paramref name="easeStartUsage01"/>, so a driver
        /// who never gets that close to the limit sees no difference at
        /// all from Off.
        /// </summary>
        public static float ApplyThrottleAssist(
            float rawThrottle, float rearCombinedGripUsage01, float easeStartUsage01)
        {
            var usage = Mathf.Clamp01(rearCombinedGripUsage01);
            var start = Mathf.Clamp01(easeStartUsage01);
            if (usage <= start)
            {
                return rawThrottle;
            }

            var overshoot = Mathf.InverseLerp(start, 1f, usage);
            var reduction = Mathf.Clamp01(1f - overshoot);
            return rawThrottle * reduction;
        }

        /// <summary>
        /// BrakeAssist: a crude ABS-like ease-off -- reduces requested
        /// brake input as the wheel-lock ratio (see
        /// KartDynamicsMath.CalculateWheelLockRatio) climbs, capped at
        /// halving the request even at full lock (never removes braking
        /// entirely, matching how real ABS still brakes, just pulses/
        /// modulates instead of holding a full lock). Only ever reduces the
        /// requested value.
        /// </summary>
        public static float ApplyBrakeAssist(float rawBrake, float wheelLockRatio01)
        {
            var lockRatio = Mathf.Clamp01(wheelLockRatio01);
            return rawBrake * (1f - lockRatio * 0.5f);
        }

        /// <summary>
        /// StabilityAssist: rate-limits how fast the EFFECTIVE steering
        /// input can change (a simple low-pass filter, same MoveTowards
        /// pattern as the Etapa 5 brake ramp), damping the kind of rapid
        /// back-and-forth "sawing at the wheel" oscillation a nervous
        /// beginner input often has. Requires the caller to hold the
        /// previous smoothed value across ticks (see KartAssistController)
        /// -- this function itself stays a pure, stateless step function.
        /// A driver whose input never changes faster than the rate limit
        /// anyway (i.e. any smooth, well-modulated input) is completely
        /// unaffected, so this cannot make ragged input perform BETTER than
        /// smooth input already does unassisted -- only closer to it.
        /// </summary>
        public static float ApplyStabilityAssistSmoothing(
            float previousSmoothedSteering, float rawSteering, float maxRatePerSecond, float deltaTime)
        {
            var rate = Mathf.Max(0.1f, maxRatePerSecond);
            return Mathf.MoveTowards(previousSmoothedSteering, rawSteering, rate * deltaTime);
        }

        /// <summary>
        /// CounterSteerAssist: nudges steering toward the direction that
        /// would catch a severe rear slide (oversteer), but ONLY once the
        /// rear slip angle already exceeds <paramref name="severeSlipThresholdDegrees"/>
        /// AND the driver is not already steering that way. The nudge
        /// magnitude ramps in from 0 at the threshold to
        /// <paramref name="maxAssistSteering01"/> at twice the threshold,
        /// and is ADDED to (never replaces) the driver's own input, so a
        /// driver who is already countersteering correctly and hard gets
        /// no help beyond their own input (their steering is clamped to
        /// +/-1 either way) -- this cannot out-perform a skilled driver's
        /// own correct, well-timed countersteer, only help a driver who
        /// is not countersteering at all catch a slide they would
        /// otherwise have fully lost.
        ///
        /// SIGN CONVENTION (IMPORTANT, VALIDATED MATHEMATICALLY / UNITY
        /// PLAYTEST PENDING): per KartDynamicsMath's axle slip functions
        /// and KartBotMath's "positive steeringInput = steer right"
        /// convention, this assumes the correct catch direction has the
        /// SAME sign as rearSlipAngleDegrees (reasoned through the
        /// geometry in the Etapa 8 report -- a right turn's oversteer
        /// swings the tail to the kart's local left, i.e. negative rear
        /// slip, and catching it means steering further left, i.e. also
        /// negative -- signs match). This reasoning has NOT been confirmed
        /// against an actual spinning kart in Unity. KartAssistController
        /// keeps this specific assist disabled by default even under
        /// Beginner until that visual confirmation happens -- see its own
        /// doc comment.
        /// </summary>
        public static float ApplyCounterSteerAssist(
            float rawSteering, float rearSlipAngleDegrees, float severeSlipThresholdDegrees, float maxAssistSteering01)
        {
            var threshold = Mathf.Max(1f, severeSlipThresholdDegrees);
            var slipMagnitude = Mathf.Abs(rearSlipAngleDegrees);
            if (slipMagnitude <= threshold)
            {
                return rawSteering;
            }

            var catchDirection = Mathf.Sign(rearSlipAngleDegrees);
            var alreadyCatching = Mathf.Sign(rawSteering) == catchDirection && Mathf.Abs(rawSteering) > 0.05f;
            if (alreadyCatching)
            {
                return rawSteering;
            }

            var overshoot = Mathf.InverseLerp(threshold, threshold * 2f, slipMagnitude);
            var assistAmount = Mathf.Clamp01(overshoot) * Mathf.Clamp01(maxAssistSteering01);
            return Mathf.Clamp(rawSteering + catchDirection * assistAmount, -1f, 1f);
        }
    }
}
