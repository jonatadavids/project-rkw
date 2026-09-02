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

        // ------------------------------------------------------------------
        // ETAPA 1 (2026-08-31) -- axle slip/grip model integrated into the
        // existing yaw architecture ("axle slip/grip model integrado a
        // arquitetura atual de yaw" -- precise naming requested explicitly
        // at the Etapa 1.1 validation gate, 2026-08-31, to avoid this being
        // read as a complete per-axle FORCE model). See auditoria-fisica-
        // kart.md, problem P1 ("nao existe modelo de pneu por roda") and
        // the founder's own Etapa 1 spec: split the single
        // whole-kart slip angle/grip used above into a FRONT axle value
        // (drives steering/direction capability) and a REAR axle value
        // (drives stability/rotation), so understeer and oversteer can
        // exist as two independently-varying numbers instead of one blended
        // one. Deliberately NOT four independent wheels yet (two axles was
        // the agreed cost/benefit tradeoff for this etapa), and deliberately
        // NOT combined with acceleration into a friction circle yet (that is
        // a separate, later etapa, kept isolated on purpose so a bad result
        // can be attributed to one change at a time).
        // ------------------------------------------------------------------

        /// <summary>
        /// Velocity of a point offset from a rigid body's center of mass,
        /// given the body's linear and angular velocity -- the same formula
        /// Rigidbody.GetPointVelocity uses internally (v = v_com + omega x
        /// r), reimplemented as a pure function here so it is unit-testable
        /// without a live Rigidbody/PhysX. All vectors are WORLD space; the
        /// offset is measured FROM the center of mass TO the point (e.g. the
        /// front axle). This is what makes the front and rear axle "feel"
        /// different velocities while the kart is yawing, even though both
        /// share the same rigid body.
        /// </summary>
        public static Vector3 CalculateAxlePointVelocityWorld(
            Vector3 centerOfMassVelocityWorld,
            Vector3 angularVelocityWorld,
            Vector3 axleOffsetFromCenterOfMassWorld)
        {
            return centerOfMassVelocityWorld + Vector3.Cross(angularVelocityWorld, axleOffsetFromCenterOfMassWorld);
        }

        /// <summary>
        /// Slip angle (degrees) from a point's LOCAL lateral/longitudinal
        /// velocity -- same convention as the existing whole-kart
        /// SlipAngleDegrees in KartDynamics (atan2(lateral, |longitudinal| +
        /// 0.1), same small epsilon, kept identical on purpose so this
        /// shares the existing calibration rather than introducing a subtly
        /// different curve shape). Below <paramref name="lowSpeedThresholdMetersPerSecond"/>
        /// total planar speed at this axle, returns exactly 0 instead of
        /// letting atan2 respond to velocity noise near zero -- a kart
        /// sitting still or creeping is not "sliding", and without this gate
        /// the angle can swing wildly (e.g. between -90 and 90 degrees) from
        /// sub-centimeter-per-second velocity jitter. This is the "nao
        /// permitir NaN/Infinity/oscilacoes" requirement; atan2 itself can
        /// never return NaN/Infinity for finite inputs, so the real risk
        /// here was never a crash, it was this near-zero-speed noise.
        /// </summary>
        public static float CalculateAxleSlipAngleDegrees(
            float lateralVelocityMetersPerSecond,
            float longitudinalVelocityMetersPerSecond,
            float lowSpeedThresholdMetersPerSecond)
        {
            var threshold = Mathf.Max(0.01f, lowSpeedThresholdMetersPerSecond);
            // Etapa 1.2 (2026-08-31) fix for the Etapa 1.1 validation gate
            // (item 7): the old version was a hard cutoff -- 0 below
            // `threshold`, the raw atan2 angle at/above it -- which produced
            // a real, measured jump (~7 degrees for the Rental Sport tuning)
            // over a 5 mm/s change in speed right at the threshold. This
            // replaces the cutoff with a SmoothStep blend across the lower
            // half of the threshold: still exactly 0 at/below half the
            // threshold (a kart truly at rest or creeping still reports no
            // slip, same guarantee as before), ramping smoothly up to the
            // FULL raw angle exactly at the threshold, and completely
            // unchanged above it -- so normal racing speeds (anything above
            // a fraction of a km/h) see zero behavior change; only the
            // narrow near-stationary band changes, from a snap to a ramp.
            var lowerBound = threshold * 0.5f;
            var planarSpeed = Mathf.Sqrt(lateralVelocityMetersPerSecond * lateralVelocityMetersPerSecond +
                                          longitudinalVelocityMetersPerSecond * longitudinalVelocityMetersPerSecond);
            var rawAngleDegrees = Mathf.Atan2(lateralVelocityMetersPerSecond,
                Mathf.Abs(longitudinalVelocityMetersPerSecond) + 0.1f) * Mathf.Rad2Deg;

            if (planarSpeed <= lowerBound)
            {
                return 0f;
            }

            if (planarSpeed >= threshold)
            {
                return rawAngleDegrees;
            }

            var blend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(lowerBound, threshold, planarSpeed));
            return rawAngleDegrees * blend;
        }

        /// <summary>
        /// Front axle slip angle additionally subtracts the front wheels'
        /// own steer angle from the raw velocity-direction angle above --
        /// standard vehicle-dynamics convention (slip angle = velocity
        /// angle - wheel heading angle). This is the one place steering
        /// input enters an axle's slip angle; <see cref="CalculateAxleSlipAngleDegrees"/>
        /// is used as-is for the rear axle, which does not steer. Concretely:
        /// pointing the front wheels into a turn while the velocity vector
        /// still points straight ahead (the instant after a steering input,
        /// before yaw has caught up) produces a non-zero front slip angle
        /// even with zero lateral velocity -- exactly the "frontSlip deve
        /// responder a orientacao das rodas" requirement.
        /// </summary>
        public static float CalculateFrontAxleSlipAngleDegrees(
            float lateralVelocityMetersPerSecond,
            float longitudinalVelocityMetersPerSecond,
            float steeringInput,
            float maxSteeringAngleDegrees,
            float lowSpeedThresholdMetersPerSecond)
        {
            var threshold = Mathf.Max(0.01f, lowSpeedThresholdMetersPerSecond);
            // Etapa 1.2 -- same blend fix as CalculateAxleSlipAngleDegrees
            // above, applied to the FINAL slip value (velocity angle minus
            // wheel angle), not just the velocity angle. Blending only the
            // velocity-angle term would leave a residual "-wheelAngle"
            // jump at near-zero speed even with the wheel turned and the
            // kart not moving -- a parked kart with the wheel cranked over
            // should still read 0 slip, matching the existing (correct)
            // behavior below the threshold.
            var lowerBound = threshold * 0.5f;
            var planarSpeed = Mathf.Sqrt(lateralVelocityMetersPerSecond * lateralVelocityMetersPerSecond +
                                          longitudinalVelocityMetersPerSecond * longitudinalVelocityMetersPerSecond);
            var velocityAngleDegrees = Mathf.Atan2(lateralVelocityMetersPerSecond,
                Mathf.Abs(longitudinalVelocityMetersPerSecond) + 0.1f) * Mathf.Rad2Deg;
            var wheelAngleDegrees = Mathf.Clamp(steeringInput, -1f, 1f) * Mathf.Max(1f, maxSteeringAngleDegrees);
            var rawSlipDegrees = velocityAngleDegrees - wheelAngleDegrees;

            if (planarSpeed <= lowerBound)
            {
                return 0f;
            }

            if (planarSpeed >= threshold)
            {
                return rawSlipDegrees;
            }

            var blend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(lowerBound, threshold, planarSpeed));
            return rawSlipDegrees * blend;
        }

        /// <summary>
        /// Same ternary shortcut the existing single-axle call site in
        /// KartDynamics.ApplyLateralForces already used (full grip below
        /// peak slip angle, EvaluateGripCurve's own falloff beyond it) --
        /// factored out here so the legacy whole-kart grip and the new
        /// front/rear axle grips all call one shared, already-proven curve
        /// instead of three copies of the same expression. No behavior
        /// change versus the inline expression it replaces.
        /// </summary>
        public static float EvaluateAxleGripRatio(
            float slipAngleDegrees,
            float peakSlipAngleDegrees,
            float fullLossSlipAngleDegrees,
            float minimumGripRatio)
        {
            return Mathf.Abs(slipAngleDegrees) <= peakSlipAngleDegrees
                ? 1f
                : EvaluateGripCurve(slipAngleDegrees, peakSlipAngleDegrees, fullLossSlipAngleDegrees, minimumGripRatio);
        }

        // ------------------------------------------------------------------
        // ETAPA 2 (2026-08-31) -- friction ellipse per axle. See
        // auditoria-fisica-kart.md P2 ("nao existe circulo de aderencia") and
        // the founder's Etapa 2 spec: each axle has ONE limited grip budget
        // shared between its lateral (cornering) demand and its
        // longitudinal (drive/brake) demand -- a tire cannot deliver 100%
        // of both at the same time. Implemented as an ELLIPSE (not a bare
        // circle) so lateral and longitudinal capacity can be biased
        // independently per category via *FrictionEllipseBias.
        //
        // Deliberately NOT a literal per-wheel slip-ratio/longitudinal-slip
        // simulation (that would need real tire slip-ratio curves this
        // project has no data for) -- this stays at the same "axle slip
        // angle + axle grip ratio" level of fidelity already established in
        // Etapa 1, extended with one more axis (how much of that budget is
        // already spent on the OTHER axis). See KartDynamics.ApplyLongitudinalForces
        // and ApplyLateralForces for how the two directions of this coupling
        // are wired in (throttle/brake -> less lateral grip is same-tick;
        // cornering -> less drive/brake force uses the PREVIOUS tick's slip
        // angle, to avoid a same-tick circular dependency between the two
        // methods -- a one-physics-tick, ~20ms lag, standard practice for
        // this kind of cross-coupling and not perceptible in play).
        // ------------------------------------------------------------------

        /// <summary>
        /// Given how much of an axle's grip budget is already spent on ONE
        /// axis (0 = none, 1 = fully spent), returns the remaining capacity
        /// ratio (0..1) available on the OTHER axis, per an elliptical
        /// (not circular, unless bias=1) combined-grip constraint:
        /// (usage/bias)^2 + (remaining)^2 &lt;= 1. A bias &gt; 1 means this axis
        /// tolerates more combined demand before the OTHER axis's capacity
        /// starts shrinking (e.g. a rear tire under acceleration typically
        /// holds a bit more combined grip than one purely cornering).
        /// </summary>
        public static float CalculateEllipseRemainingCapacityRatio(float usageRatio01, float bias)
        {
            var usage = Mathf.Clamp01(usageRatio01) / Mathf.Max(0.01f, bias);
            var remainingSquared = 1f - usage * usage;
            return Mathf.Sqrt(Mathf.Max(0f, remainingSquared));
        }

        /// <summary>
        /// Diagnostic-only combined usage magnitude (0..1) for telemetry --
        /// how "full" an axle's grip circle looks combining both axes,
        /// ignoring bias (a plain circle for display purposes only; the
        /// actual physics uses the biased ellipse above).
        /// </summary>
        public static float CalculateCombinedGripUsage(float lateralUsageRatio01, float longitudinalUsageRatio01)
        {
            var lateral = Mathf.Clamp01(lateralUsageRatio01);
            var longitudinal = Mathf.Clamp01(longitudinalUsageRatio01);
            return Mathf.Clamp01(Mathf.Sqrt(lateral * lateral + longitudinal * longitudinal));
        }

        // ------------------------------------------------------------------
        // ETAPA 3 (2026-08-31): rigid rear axle refinement.
        //
        // A go-kart has no differential -- both rear wheels are welded to
        // one solid axle and are always forced to spin at the SAME rate.
        // In a corner the inside wheel geometrically wants to travel a
        // shorter arc (spin slower) than the outside wheel. Because it
        // cannot, one wheel scrubs against the pavement. While both wheels
        // are still loaded (grounded, sharing weight roughly evenly) that
        // scrub actively RESISTS the kart rotating into the corner --
        // real karts feel like they understeer/fight rotation at
        // low-to-mid corner speed until enough lateral load transfer lifts
        // the inside rear wheel off the ground; once it is light enough to
        // spin freely (or barely touching), the bind releases and the kart
        // rotates much more freely. This is a rotational (yaw) resistance,
        // not a grip number -- see CalculateRearAxleScrubYawRateLossDegPerSec
        // below and KartDynamics.ApplySteering for why it is subtracted
        // from the requested yaw rate rather than folded into any grip
        // multiplier (that would conflate two different physical effects
        // and risks becoming an unearned "grip bonus/penalty").
        // ------------------------------------------------------------------

        /// <summary>
        /// Estimated normal load (Newtons) on ONE rear corner (inside or
        /// outside the current turn), from that corner's static share of
        /// the rear axle load plus the CONTINUOUS lateral weight-transfer
        /// ratio (0..1, from CalculateLateralWeightTransferRatio -- not the
        /// threshold-gated "lift" ratio: real load transfer starts the
        /// instant there is any lateral acceleration, it does not wait for
        /// a threshold). Both corners share the same static baseline and
        /// move by the same magnitude in opposite directions, so total
        /// rear axle load is conserved (outside gains exactly what inside
        /// loses) until inside would go negative, at which point it is
        /// clamped to 0 (a wheel cannot have negative load -- the excess is
        /// not added back onto the outside corner here, since a real wheel
        /// that has already lifted stops taking on more load from further
        /// transfer in this simplified per-corner model; see the class
        /// doc's "directionally-useful diagnostic, not exact" caveat).
        /// </summary>
        public static float CalculateRearCornerLoadNewtons(
            float staticRearCornerLoadNewtons, float lateralWeightTransferRatio01, bool isOutsideCorner)
        {
            var ratio = Mathf.Clamp01(lateralWeightTransferRatio01);
            var sign = isOutsideCorner ? 1f : -1f;
            return Mathf.Max(0f, staticRearCornerLoadNewtons * (1f + sign * ratio));
        }

        /// <summary>
        /// How much the rigid (differential-less) rear axle is currently
        /// "bound" -- forced to spin both wheels at the same rate despite
        /// cornering geometry wanting them to differ. 1 = both wheels still
        /// grounded and fighting each other (maximum bind); 0 = the inside
        /// wheel has lifted enough to spin freely, releasing the bind.
        /// Deliberately derived from the existing THRESHOLD-gated
        /// CalculateInnerRearLift result (not the raw continuous transfer
        /// ratio): a small amount of load transfer alone does not
        /// meaningfully free a tire that is still firmly on the ground, so
        /// the bind should not start releasing until the wheel is
        /// genuinely close to lifting.
        /// </summary>
        public static float CalculateRearAxleBindingFactor(float innerRearLift01)
        {
            return 1f - Mathf.Clamp01(innerRearLift01);
        }

        /// <summary>
        /// Degrees/second of the geometrically-requested (Ackermann) yaw
        /// rate lost to rigid-axle scrub resistance right now. A genuine
        /// rotational resistance -- see KartDynamics.ApplySteering, which
        /// SUBTRACTS this from the requested yaw rate, never scales any
        /// grip value by it. Zero whenever the axle is released
        /// (bindingFactor 0) or the tuning's max scrub is 0 (the default
        /// for every asset predating Etapa 3, so this is a strict opt-in
        /// with zero behavior change until deliberately tuned).
        /// </summary>
        public static float CalculateRearAxleScrubYawRateLossDegPerSec(
            float rearAxleBindingFactor01, float maxScrubYawRateLossDegPerSec)
        {
            return Mathf.Clamp01(rearAxleBindingFactor01) * Mathf.Max(0f, maxScrubYawRateLossDegPerSec);
        }

        // ------------------------------------------------------------------
        // ETAPA 4 (2026-08-31): steering response curve + Ackermann per-wheel
        // visual angles.
        // ------------------------------------------------------------------

        /// <summary>
        /// Reshapes a raw -1..1 steering input with a power curve, applied
        /// INSTANTANEOUSLY (no smoothing/lag added here -- see the Etapa 4
        /// spec's explicit "sem lag artificial de input" requirement; any
        /// separate input smoothing a platform layer wants is its own,
        /// unrelated concern). exponent == 1 is the identity function (every
        /// asset predating Etapa 4 defaults to this, so behavior is
        /// unchanged until deliberately tuned). exponent &gt; 1 gives finer
        /// control near center (small stick/touch movements produce even
        /// smaller wheel angles) with the full range still reachable at
        /// input == +/-1; exponent &lt; 1 does the opposite (more sensitive
        /// near center). Sign is preserved so the curve never flips
        /// direction.
        /// </summary>
        public static float ApplySteeringResponseCurve(float clampedInput, float curveExponent)
        {
            var input = Mathf.Clamp(clampedInput, -1f, 1f);
            var exponent = Mathf.Max(0.1f, curveExponent);
            return Mathf.Sign(input) * Mathf.Pow(Mathf.Abs(input), exponent);
        }

        /// <summary>
        /// Real Ackermann steering geometry: for one central/average
        /// steering angle, the wheel on the INSIDE of the turn must point
        /// sharper than the one on the OUTSIDE, since the two trace circles
        /// of different radii around the same turn center. This is
        /// VISUAL-ONLY -- KartDynamics.ApplySteering already correctly uses
        /// a single "bicycle model" effective angle for the whole front
        /// axle's actual physics (see CalculateAckermannYawRateDegreesPerSecond);
        /// this only makes the two individual front wheel MESHES point at
        /// the geometrically correct angles for that same turn, for
        /// KartSteeringVisual. Returns 0/0 for a (near) straight wheel,
        /// where inner/outer is meaningless.
        /// </summary>
        public static void CalculateAckermannWheelAnglesDegrees(
            float centralSteeringAngleDegrees,
            float wheelbaseMeters,
            float trackWidthMeters,
            out float innerWheelAngleDegrees,
            out float outerWheelAngleDegrees)
        {
            if (Mathf.Abs(centralSteeringAngleDegrees) < 0.01f)
            {
                innerWheelAngleDegrees = 0f;
                outerWheelAngleDegrees = 0f;
                return;
            }

            var wheelbase = Mathf.Max(0.1f, wheelbaseMeters);
            var halfTrack = Mathf.Max(0.05f, trackWidthMeters) * 0.5f;
            var centralAngleRadians = Mathf.Abs(centralSteeringAngleDegrees) * Mathf.Deg2Rad;
            var turnRadius = wheelbase / Mathf.Tan(centralAngleRadians);

            var innerRadius = Mathf.Max(0.01f, turnRadius - halfTrack);
            var outerRadius = turnRadius + halfTrack;

            var innerAngleRadians = Mathf.Atan(wheelbase / innerRadius);
            var outerAngleRadians = Mathf.Atan(wheelbase / outerRadius);

            var sign = Mathf.Sign(centralSteeringAngleDegrees);
            innerWheelAngleDegrees = sign * innerAngleRadians * Mathf.Rad2Deg;
            outerWheelAngleDegrees = sign * outerAngleRadians * Mathf.Rad2Deg;
        }

        // ------------------------------------------------------------------
        // ETAPA 5 (2026-08-31): brake lock-up diagnostic.
        // ------------------------------------------------------------------

        /// <summary>
        /// Diagnostic-only (telemetry/future haptics) 0..1 wheel-lock
        /// estimate: 0 while requested braking stays within available
        /// grip, ramping to 1 as requested braking overshoots available
        /// grip by 50% or more. Uses the SAME requested/available
        /// deceleration quantities CalculateBrakingWithSteering already
        /// computes internally to reduce its own effectiveDeceleration
        /// output when overshooting -- this does not change that
        /// function's behavior or signature, it just exposes the same
        /// underlying "how locked up are we" quantity as its own testable
        /// value. See KartDynamics.ApplyLongitudinalForces for the call
        /// site, which passes it the identical inputs (tuning.BrakeDeceleration
        /// * smoothed brake input, and grip*g) that CalculateBrakingWithSteering
        /// uses for its own lockRatio.
        /// </summary>
        public static float CalculateWheelLockRatio(float requestedDeceleration, float availableGripDeceleration)
        {
            var available = Mathf.Max(0.01f, availableGripDeceleration);
            return Mathf.Clamp01(Mathf.InverseLerp(available, available * 1.5f, Mathf.Max(0f, requestedDeceleration)));
        }

        // ------------------------------------------------------------------
        // ETAPA 10 (2026-08-31): RPM/torque engine model -- OPT-IN (see
        // KartCategorySO.UseTorqueCurveEngineModel, default false for every
        // asset predating Etapa 10). CalculateAccelerationMetersPerSecondSquared
        // above (proximity-to-top-speed) remains the DEFAULT, already-tuned
        // model and is completely untouched. EngineRPM itself (below) is
        // always computed and always available for telemetry/audio/UI
        // regardless of which acceleration model is actually driving the
        // kart -- it is a real, physically-derived-from-wheel-speed value,
        // not a decoration.
        //
        // The torque curve is modeled as 4 named Newton-meter values at
        // fixed points along the RPM range (idle, 33%, 66%, redline of
        // maxRPM) rather than a Unity AnimationCurve, specifically so this
        // data serializes as plain scalar YAML (safe to hand-author/patch
        // without Unity, and directly unit-testable) instead of
        // AnimationCurve's keyframe/tangent-mode YAML structure, which
        // carries real risk of producing a subtly malformed asset if
        // authored by hand outside the Editor.
        // ------------------------------------------------------------------

        /// <summary>
        /// Engine RPM implied by the kart's current wheel speed, assuming
        /// the drivetrain is fully locked (no clutch slip) -- reasonable
        /// for a kart's centrifugal clutch once above idle. Clamped to
        /// [idleRPM, redlineRPM]: below the RPM a near-zero wheel speed
        /// would imply, a real centrifugal clutch has already disengaged
        /// and the engine simply idles rather than following wheel speed
        /// down toward 0.
        /// </summary>
        public static float CalculateEngineRPM(
            float wheelSpeedMetersPerSecond, float finalDriveRatio, float wheelRadiusMeters,
            float idleRPM, float redlineRPM)
        {
            var radius = Mathf.Max(0.01f, wheelRadiusMeters);
            var wheelAngularVelocityRadPerSec = Mathf.Abs(wheelSpeedMetersPerSecond) / radius;
            var engineAngularVelocityRadPerSec = wheelAngularVelocityRadPerSec * Mathf.Max(0.1f, finalDriveRatio);
            var rpm = engineAngularVelocityRadPerSec * 60f / (2f * Mathf.PI);
            var minRpm = Mathf.Max(0f, idleRPM);
            var maxRpm = Mathf.Max(minRpm, redlineRPM);
            return Mathf.Clamp(rpm, minRpm, maxRpm);
        }

        /// <summary>
        /// Smoothly interpolates the 4-point torque curve (see this
        /// section's class-level comment) at a normalized RPM position
        /// (0 = idle, 1 = redline-relative-to-maxRPM). SmoothStep blending
        /// between segments, same style as EvaluateGripCurve above, avoids
        /// visible slope discontinuities at the 0.33/0.66 breakpoints.
        /// </summary>
        public static float EvaluateTorqueCurveNewtonMeters(
            float normalizedRPM01,
            float torqueAtIdle, float torqueAtLowMid, float torqueAtHighMid, float torqueAtRedline)
        {
            var t = Mathf.Clamp01(normalizedRPM01);
            if (t <= 0.33f)
            {
                return Mathf.Lerp(torqueAtIdle, torqueAtLowMid, Mathf.SmoothStep(0f, 1f, t / 0.33f));
            }

            if (t <= 0.66f)
            {
                return Mathf.Lerp(torqueAtLowMid, torqueAtHighMid, Mathf.SmoothStep(0f, 1f, (t - 0.33f) / 0.33f));
            }

            return Mathf.Lerp(torqueAtHighMid, torqueAtRedline, Mathf.SmoothStep(0f, 1f, (t - 0.66f) / 0.34f));
        }

        /// <summary>
        /// Full-throttle acceleration (m/s^2) implied by the torque curve
        /// at the given engine RPM, through the final drive ratio and wheel
        /// radius, divided by mass. The caller (KartDynamics.ApplyLongitudinalForces)
        /// scales this by smoothedThrottle exactly like the legacy formula
        /// is scaled, so both models plug into the same "acceleration *
        /// throttle" call site.
        /// </summary>
        public static float CalculateTorqueCurveAccelerationMetersPerSecondSquared(
            float engineRPM, float maxRPM,
            float torqueAtIdle, float torqueAtLowMid, float torqueAtHighMid, float torqueAtRedline,
            float finalDriveRatio, float wheelRadiusMeters, float massKilograms)
        {
            var normalizedRPM = Mathf.Clamp01(engineRPM / Mathf.Max(1f, maxRPM));
            var torqueNewtonMeters = Mathf.Max(0f, EvaluateTorqueCurveNewtonMeters(
                normalizedRPM, torqueAtIdle, torqueAtLowMid, torqueAtHighMid, torqueAtRedline));
            var wheelForceNewtons = torqueNewtonMeters * Mathf.Max(0.1f, finalDriveRatio) / Mathf.Max(0.01f, wheelRadiusMeters);
            return wheelForceNewtons / Mathf.Max(1f, massKilograms);
        }
    }
}
