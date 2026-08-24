using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace RKW.Physics.Tests.EditMode
{
    /// <summary>
    /// Demo bot AI feel pass (founder playtest feedback, 2026-08-19:
    /// "poderia ter pelo menos 1 bot competindo").
    /// </summary>
    public sealed class KartBotMathTests
    {
        [Test]
        public void SteeringToTarget_TargetDirectlyAhead_IsZero()
        {
            var steering = KartBotMath.CalculateSteeringToTarget(
                Vector3.zero, Vector3.forward, new Vector3(0f, 0f, 10f), 1f);

            Assert.That(steering, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void SteeringToTarget_TargetToTheRight_IsPositive()
        {
            var steering = KartBotMath.CalculateSteeringToTarget(
                Vector3.zero, Vector3.forward, new Vector3(10f, 0f, 5f), 1f);

            Assert.That(steering, Is.GreaterThan(0f));
        }

        [Test]
        public void SteeringToTarget_TargetToTheLeft_IsNegative()
        {
            var steering = KartBotMath.CalculateSteeringToTarget(
                Vector3.zero, Vector3.forward, new Vector3(-10f, 0f, 5f), 1f);

            Assert.That(steering, Is.LessThan(0f));
        }

        [Test]
        public void SteeringToTarget_NeverExceedsMaxMagnitude()
        {
            // Target almost directly behind — largest possible steering demand.
            var steering = KartBotMath.CalculateSteeringToTarget(
                Vector3.zero, Vector3.forward, new Vector3(0.01f, 0f, -10f), 0.7f);

            Assert.That(Mathf.Abs(steering), Is.LessThanOrEqualTo(0.7f));
        }

        [Test]
        public void SteeringToTarget_SamePositionAsTarget_IsZero()
        {
            var steering = KartBotMath.CalculateSteeringToTarget(
                new Vector3(5f, 0f, 5f), Vector3.forward, new Vector3(5f, 0f, 5f), 1f);

            Assert.That(steering, Is.EqualTo(0f));
        }

        [Test]
        public void HasReachedWaypoint_WithinRadius_IsTrue()
        {
            var reached = KartBotMath.HasReachedWaypoint(
                new Vector3(0f, 0f, 0f), new Vector3(2f, 0f, 0f), arrivalRadiusMeters: 4f);

            Assert.That(reached, Is.True);
        }

        [Test]
        public void HasReachedWaypoint_OutsideRadius_IsFalse()
        {
            var reached = KartBotMath.HasReachedWaypoint(
                new Vector3(0f, 0f, 0f), new Vector3(10f, 0f, 0f), arrivalRadiusMeters: 4f);

            Assert.That(reached, Is.False);
        }

        [Test]
        public void HasReachedWaypoint_IgnoresHeightDifference()
        {
            // Y (height) must not affect arrival — karts don't jump.
            var reached = KartBotMath.HasReachedWaypoint(
                new Vector3(0f, 50f, 0f), new Vector3(1f, 0f, 0f), arrivalRadiusMeters: 4f);

            Assert.That(reached, Is.True);
        }

        [Test]
        public void CalculateThrottle_NoSteering_IsMaxThrottle()
        {
            var throttle = KartBotMath.CalculateThrottle(0f, minThrottle: 0.5f, maxThrottle: 0.9f);

            Assert.That(throttle, Is.EqualTo(0.9f).Within(0.001f));
        }

        [Test]
        public void CalculateThrottle_FullSteering_IsMinThrottle()
        {
            var throttle = KartBotMath.CalculateThrottle(1f, minThrottle: 0.5f, maxThrottle: 0.9f);

            Assert.That(throttle, Is.EqualTo(0.5f).Within(0.001f));
        }

        [Test]
        public void AdvanceWaypointIndex_WrapsAroundAtEnd()
        {
            var next = KartBotMath.AdvanceWaypointIndex(currentIndex: 6, waypointCount: 7);

            Assert.That(next, Is.EqualTo(0));
        }

        [Test]
        public void AdvanceWaypointIndex_NormalStep_Increments()
        {
            var next = KartBotMath.AdvanceWaypointIndex(currentIndex: 2, waypointCount: 7);

            Assert.That(next, Is.EqualTo(3));
        }

        [Test]
        public void AdvanceWaypointIndex_EmptyPath_ReturnsZero()
        {
            var next = KartBotMath.AdvanceWaypointIndex(currentIndex: 3, waypointCount: 0);

            Assert.That(next, Is.EqualTo(0));
        }

        /// <summary>
        /// Round-20 regression test: reproduces the exact reported bug
        /// ("os bots nao entenderam a pista automaticamente na largada
        /// foram para o lado"). A kart parked mid-way along a long
        /// path segment (like a grid slot on the south straight, x=-19)
        /// is numerically CLOSER to the far vertex behind it (x=-38,
        /// distance 19) than to the vertex ahead (x=38, distance 57) —
        /// a naive "nearest vertex" pick would target the vertex behind
        /// the kart, exactly reproducing the "went sideways" bug. The
        /// segment-based search must still identify the segment the kart
        /// is actually sitting on (index 0, from -38 to 38).
        /// </summary>
        [Test]
        public void FindNearestPathSegmentStartIndex_PositionMidLongSegment_ReturnsSegmentNotNearestVertex()
        {
            var path = new List<Vector3>
            {
                new Vector3(-38f, 0f, -14f),
                new Vector3(38f, 0f, -14f),
                new Vector3(43f, 0f, -13f),
            };
            var spawnPosition = new Vector3(-19f, 0f, -14f);

            var index = KartBotMath.FindNearestPathSegmentStartIndex(spawnPosition, path);

            Assert.That(index, Is.EqualTo(0));
        }

        [Test]
        public void FindNearestPathSegmentStartIndex_PositionNearSecondSegment_ReturnsSecondIndex()
        {
            var path = new List<Vector3>
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(100f, 0f, 0f),
                new Vector3(100f, 0f, 100f),
                new Vector3(0f, 0f, 100f),
            };
            var spawnPosition = new Vector3(95f, 0f, 10f);

            var index = KartBotMath.FindNearestPathSegmentStartIndex(spawnPosition, path);

            Assert.That(index, Is.EqualTo(1));
        }

        [Test]
        public void FindNearestPathSegmentStartIndex_WrapsToLastSegment()
        {
            var path = new List<Vector3>
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(100f, 0f, 0f),
                new Vector3(100f, 0f, 100f),
                new Vector3(0f, 0f, 100f),
            };
            var spawnPosition = new Vector3(5f, 0f, 90f); // near the closing edge (index 3, back to index 0)

            var index = KartBotMath.FindNearestPathSegmentStartIndex(spawnPosition, path);

            Assert.That(index, Is.EqualTo(3));
        }

        [Test]
        public void FindNearestPathSegmentStartIndex_EmptyPath_ReturnsZero()
        {
            var index = KartBotMath.FindNearestPathSegmentStartIndex(Vector3.zero, new List<Vector3>());

            Assert.That(index, Is.EqualTo(0));
        }

        [Test]
        public void FindNearestPathSegmentStartIndex_NullPath_ReturnsZero()
        {
            var index = KartBotMath.FindNearestPathSegmentStartIndex(Vector3.zero, null);

            Assert.That(index, Is.EqualTo(0));
        }

        /// <summary>
        /// Round-21 founder feedback: "a inteligencia do bot controlada por
        /// matemática parece um desastre kkk" — pure-pursuit-style lookahead
        /// steering (see KartBotMath.CalculateLookaheadSteeringTarget XML
        /// doc for the full rationale). Lookahead entirely within the first
        /// segment: expect a straight interpolation, no wrap.
        /// </summary>
        [Test]
        public void CalculateLookaheadSteeringTarget_WithinFirstSegment_ReturnsInterpolatedPoint()
        {
            var path = new List<Vector3>
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(10f, 0f, 0f),
                new Vector3(10f, 0f, 10f),
            };

            var result = KartBotMath.CalculateLookaheadSteeringTarget(path, currentTargetIndex: 0, lookaheadDistanceMeters: 4f);

            Assert.That(result.x, Is.EqualTo(4f).Within(0.001f));
            Assert.That(result.z, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void CalculateLookaheadSteeringTarget_CrossesSegmentBoundary_ReturnsPointOnNextSegment()
        {
            var path = new List<Vector3>
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(10f, 0f, 0f),
                new Vector3(10f, 0f, 10f),
            };

            // First segment (index 0 -> 1) is 10m; 13m lookahead spills 3m
            // into the next segment (index 1 -> 2).
            var result = KartBotMath.CalculateLookaheadSteeringTarget(path, currentTargetIndex: 0, lookaheadDistanceMeters: 13f);

            Assert.That(result.x, Is.EqualTo(10f).Within(0.001f));
            Assert.That(result.z, Is.EqualTo(3f).Within(0.001f));
        }

        [Test]
        public void CalculateLookaheadSteeringTarget_WrapsAroundClosedLoop()
        {
            var path = new List<Vector3>
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(10f, 0f, 0f),
                new Vector3(10f, 0f, 10f),
                new Vector3(0f, 0f, 10f),
            };

            // Starting at the last waypoint (index 3): segment 3->0 is 10m
            // (down to (0,0,0)), leaving 5m to spend on segment 0->1 —
            // should land at the loop's start, not run off the end of the list.
            var result = KartBotMath.CalculateLookaheadSteeringTarget(path, currentTargetIndex: 3, lookaheadDistanceMeters: 15f);

            Assert.That(result.x, Is.EqualTo(5f).Within(0.001f));
            Assert.That(result.z, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void CalculateLookaheadSteeringTarget_ZeroLookahead_ReturnsCurrentTargetExactly()
        {
            var path = new List<Vector3>
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(10f, 0f, 0f),
            };

            var result = KartBotMath.CalculateLookaheadSteeringTarget(path, currentTargetIndex: 1, lookaheadDistanceMeters: 0f);

            Assert.That(result.x, Is.EqualTo(10f).Within(0.001f));
            Assert.That(result.z, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void CalculateLookaheadSteeringTarget_EmptyPath_ReturnsZeroVector()
        {
            var result = KartBotMath.CalculateLookaheadSteeringTarget(new List<Vector3>(), currentTargetIndex: 0, lookaheadDistanceMeters: 5f);

            Assert.That(result, Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void CalculateLookaheadSteeringTarget_NullPath_ReturnsZeroVector()
        {
            var result = KartBotMath.CalculateLookaheadSteeringTarget(null, currentTargetIndex: 0, lookaheadDistanceMeters: 5f);

            Assert.That(result, Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void CalculateLookaheadSteeringTarget_SinglePointPath_ReturnsThatPoint()
        {
            var path = new List<Vector3> { new Vector3(7f, 0f, 2f) };

            var result = KartBotMath.CalculateLookaheadSteeringTarget(path, currentTargetIndex: 0, lookaheadDistanceMeters: 5f);

            Assert.That(result.x, Is.EqualTo(7f).Within(0.001f));
            Assert.That(result.z, Is.EqualTo(2f).Within(0.001f));
        }

        [Test]
        public void GetSteeringErrorDegrees_HardIsZero()
        {
            Assert.That(KartBotMath.GetSteeringErrorDegrees(BotDifficulty.Hard), Is.EqualTo(0f));
        }

        [Test]
        public void GetSteeringErrorDegrees_EasyIsGreaterThanMedium()
        {
            var easy = KartBotMath.GetSteeringErrorDegrees(BotDifficulty.Easy);
            var medium = KartBotMath.GetSteeringErrorDegrees(BotDifficulty.Medium);

            Assert.That(easy, Is.GreaterThan(medium));
        }

        [Test]
        public void GetArrivalRadiusMeters_HardIsSmallestOfTheThree()
        {
            var easy = KartBotMath.GetArrivalRadiusMeters(BotDifficulty.Easy);
            var medium = KartBotMath.GetArrivalRadiusMeters(BotDifficulty.Medium);
            var hard = KartBotMath.GetArrivalRadiusMeters(BotDifficulty.Hard);

            Assert.That(hard, Is.LessThan(medium));
            Assert.That(medium, Is.LessThan(easy));
        }

        [Test]
        public void ApplySteeringError_ZeroError_ReturnsBaseSteering()
        {
            var steering = KartBotMath.ApplySteeringError(0.3f, errorDegrees: 0f, maxSteeringMagnitude: 1f);

            Assert.That(steering, Is.EqualTo(0.3f).Within(0.0001f));
        }

        [Test]
        public void ApplySteeringError_ClampsToMaxMagnitude()
        {
            var steering = KartBotMath.ApplySteeringError(0.9f, errorDegrees: 60f, maxSteeringMagnitude: 1f);

            Assert.That(steering, Is.LessThanOrEqualTo(1f));
        }

        [Test]
        public void ApplySteeringError_NegativeError_ReducesSteering()
        {
            var steering = KartBotMath.ApplySteeringError(0.3f, errorDegrees: -30f, maxSteeringMagnitude: 1f);

            Assert.That(steering, Is.LessThan(0.3f));
        }

        [Test]
        public void HasMadeInsufficientProgress_BelowThreshold_IsTrue()
        {
            Assert.That(KartBotMath.HasMadeInsufficientProgress(0.2f, minimumProgressMeters: 1.5f), Is.True);
        }

        [Test]
        public void HasMadeInsufficientProgress_AboveThreshold_IsFalse()
        {
            Assert.That(KartBotMath.HasMadeInsufficientProgress(3f, minimumProgressMeters: 1.5f), Is.False);
        }

        [Test]
        public void CalculateRecoverySteering_ZeroLastSteering_ReturnsNonZero()
        {
            var steering = KartBotMath.CalculateRecoverySteering(0f);

            Assert.That(steering, Is.EqualTo(1f));
        }

        [Test]
        public void CalculateRecoverySteering_OpposesLastSteeringDirection()
        {
            var steeringWhenLastWasRight = KartBotMath.CalculateRecoverySteering(0.6f);
            var steeringWhenLastWasLeft = KartBotMath.CalculateRecoverySteering(-0.6f);

            Assert.That(steeringWhenLastWasRight, Is.EqualTo(-1f));
            Assert.That(steeringWhenLastWasLeft, Is.EqualTo(1f));
        }

        [Test]
        public void CalculateCornerSharpness01_StraightLine_IsZero()
        {
            var sharpness = KartBotMath.CalculateCornerSharpness01(
                new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 10f), new Vector3(0f, 0f, 20f));

            Assert.That(sharpness, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void CalculateCornerSharpness01_RightAngleTurn_IsPartial()
        {
            var sharpness = KartBotMath.CalculateCornerSharpness01(
                new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 10f), new Vector3(10f, 0f, 10f));

            Assert.That(sharpness, Is.GreaterThan(0f));
            Assert.That(sharpness, Is.LessThan(1f));
        }

        [Test]
        public void CalculateCornerSharpness01_HairpinReversal_IsClampedToOne()
        {
            var sharpness = KartBotMath.CalculateCornerSharpness01(
                new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 10f), new Vector3(0f, 0f, 0f));

            Assert.That(sharpness, Is.EqualTo(1f));
        }

        [Test]
        public void CalculateCornerAwareThrottle_SharpUpcomingCorner_ReducesThrottleEvenWithoutCurrentSteering()
        {
            var throttle = KartBotMath.CalculateCornerAwareThrottle(
                absSteering01: 0f, cornerSharpness01: 1f, minThrottle: 0.4f, maxThrottle: 0.85f);

            Assert.That(throttle, Is.EqualTo(0.4f).Within(0.001f));
        }

        [Test]
        public void CalculateCornerAwareThrottle_StraightAheadNoSteering_IsMaxThrottle()
        {
            var throttle = KartBotMath.CalculateCornerAwareThrottle(
                absSteering01: 0f, cornerSharpness01: 0f, minThrottle: 0.4f, maxThrottle: 0.85f);

            Assert.That(throttle, Is.EqualTo(0.85f).Within(0.001f));
        }

        [Test]
        public void CalculateCornerBrake_SlowSpeedIntoSharpCorner_IsNearZero()
        {
            var brake = KartBotMath.CalculateCornerBrake(cornerSharpness01: 1f, speedRatio01: 0f, maxBrakeInput: 0.6f);

            Assert.That(brake, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void CalculateCornerBrake_FastIntoSharpCorner_IsPositive()
        {
            var brake = KartBotMath.CalculateCornerBrake(cornerSharpness01: 1f, speedRatio01: 1f, maxBrakeInput: 0.6f);

            Assert.That(brake, Is.EqualTo(0.6f).Within(0.001f));
        }

        [Test]
        public void CalculateCornerBrake_StraightAtFullSpeed_IsZero()
        {
            var brake = KartBotMath.CalculateCornerBrake(cornerSharpness01: 0f, speedRatio01: 1f, maxBrakeInput: 0.6f);

            Assert.That(brake, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void GetMaxThrottle_HardReachesFullThrottle()
        {
            Assert.That(KartBotMath.GetMaxThrottle(BotDifficulty.Hard), Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void GetMaxThrottle_IncreasesWithDifficulty()
        {
            var easy = KartBotMath.GetMaxThrottle(BotDifficulty.Easy);
            var medium = KartBotMath.GetMaxThrottle(BotDifficulty.Medium);
            var hard = KartBotMath.GetMaxThrottle(BotDifficulty.Hard);

            Assert.That(medium, Is.GreaterThan(easy));
            Assert.That(hard, Is.GreaterThan(medium));
        }

        [Test]
        public void GetMinThrottle_NeverExceedsMaxThrottleForSameDifficulty()
        {
            foreach (BotDifficulty difficulty in System.Enum.GetValues(typeof(BotDifficulty)))
            {
                Assert.That(KartBotMath.GetMinThrottle(difficulty), Is.LessThan(KartBotMath.GetMaxThrottle(difficulty)));
            }
        }

        // Round 14: grip-based cornering speed target, replacing the old
        // sharpness-heuristic throttle for bot competitiveness.

        [Test]
        public void PreviousWaypointIndex_WrapsAroundAtStart()
        {
            var previous = KartBotMath.PreviousWaypointIndex(currentIndex: 0, waypointCount: 8);

            Assert.That(previous, Is.EqualTo(7));
        }

        [Test]
        public void PreviousWaypointIndex_NormalStep_Decrements()
        {
            var previous = KartBotMath.PreviousWaypointIndex(currentIndex: 3, waypointCount: 8);

            Assert.That(previous, Is.EqualTo(2));
        }

        [Test]
        public void PreviousWaypointIndex_EmptyPath_ReturnsZero()
        {
            var previous = KartBotMath.PreviousWaypointIndex(currentIndex: 3, waypointCount: 0);

            Assert.That(previous, Is.EqualTo(0));
        }

        [Test]
        public void CalculateTurnAngleRadians_CollinearPoints_IsZero()
        {
            var angle = KartBotMath.CalculateTurnAngleRadians(
                new Vector3(-30f, 0f, -15f), new Vector3(-15f, 0f, -15f), new Vector3(30f, 0f, -15f));

            Assert.That(angle, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void CalculateTurnAngleRadians_RightAngleTurn_IsHalfPi()
        {
            var angle = KartBotMath.CalculateTurnAngleRadians(
                new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 10f), new Vector3(10f, 0f, 10f));

            Assert.That(angle, Is.EqualTo(Mathf.PI / 2f).Within(0.001f));
        }

        [Test]
        public void CalculateTurnAngleRadians_HairpinReversal_IsPi()
        {
            var angle = KartBotMath.CalculateTurnAngleRadians(
                new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 10f), new Vector3(0f, 0f, 0f));

            Assert.That(angle, Is.EqualTo(Mathf.PI).Within(0.001f));
        }

        [Test]
        public void CalculateTurnAngleRadians_DuplicatePoint_IsZero()
        {
            // The bot path's closing waypoint duplicates the start/finish
            // point (see OvalMvpTrackConfiguration.asset) — must not
            // divide-by-zero or blow up.
            var angle = KartBotMath.CalculateTurnAngleRadians(
                new Vector3(-15f, 0f, -15f), new Vector3(-15f, 0f, -15f), new Vector3(30f, 0f, -15f));

            Assert.That(angle, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void CalculateTurnAngleRadians_KnownOvalCorner_MatchesHandComputedAngle()
        {
            // The oval track's NE apex (before/at/after = waypoints 1/2/3):
            // ~36.87 degrees (0.6435 rad), hand-verified via the
            // dot-product formula: into=(5,15), outOf=(-5,15),
            // cos = dot/(|into||outOf|) = 200/250 = 0.8, acos(0.8)=0.6435.
            // This one waypoint-level turn is fairly gentle on its own —
            // the oval approximates its ~180° east-side bend as 3 of these
            // in a row (waypoints 1, 2, 3); the wall-hitting bug from round
            // 15/16 came from the circumradius formula's take on THIS same
            // triple (25m) still being too optimistic once fed through the
            // full sequential simulation, not from any single angle being
            // huge in isolation.
            var angle = KartBotMath.CalculateTurnAngleRadians(
                new Vector3(30f, 0f, -15f), new Vector3(35f, 0f, 0f), new Vector3(30f, 0f, 15f));

            Assert.That(angle, Is.EqualTo(0.6435f).Within(0.01f));
        }

        [Test]
        public void CalculateMaxCorneringSpeedMetersPerSecond_StraightAhead_IsSentinelHigh()
        {
            var speed = KartBotMath.CalculateMaxCorneringSpeedMetersPerSecond(
                turnAngleRadians: 0f, availableDistanceMeters: 20f, maxLateralAcceleration: 11.8f, safetyMargin01: 0.9f);

            Assert.That(speed, Is.GreaterThan(100f));
        }

        [Test]
        public void CalculateMaxCorneringSpeedMetersPerSecond_MoreAvailableDistance_IsFaster()
        {
            var tight = KartBotMath.CalculateMaxCorneringSpeedMetersPerSecond(1.2f, 10f, 11.8f, 0.5f);
            var wide = KartBotMath.CalculateMaxCorneringSpeedMetersPerSecond(1.2f, 50f, 11.8f, 0.5f);

            Assert.That(wide, Is.GreaterThan(tight));
        }

        [Test]
        public void CalculateMaxCorneringSpeedMetersPerSecond_SharperAngle_IsSlower()
        {
            var gentle = KartBotMath.CalculateMaxCorneringSpeedMetersPerSecond(0.5f, 20f, 11.8f, 0.5f);
            var sharp = KartBotMath.CalculateMaxCorneringSpeedMetersPerSecond(2.5f, 20f, 11.8f, 0.5f);

            Assert.That(sharp, Is.LessThan(gentle));
        }

        [Test]
        public void CalculateMaxCorneringSpeedMetersPerSecond_HigherSafetyMargin_IsFaster()
        {
            var cautious = KartBotMath.CalculateMaxCorneringSpeedMetersPerSecond(1.2f, 20f, 11.8f, 0.3f);
            var aggressive = KartBotMath.CalculateMaxCorneringSpeedMetersPerSecond(1.2f, 20f, 11.8f, 0.45f);

            Assert.That(aggressive, Is.GreaterThan(cautious));
        }

        [Test]
        public void CalculateMaxCorneringSpeedMetersPerSecond_KnownOvalCorner_MatchesSimulationValidatedValue()
        {
            // Hard-mode target speed for the oval's NE apex (turn angle
            // 0.6435 rad, approach leg 15.81m — see
            // CalculateTurnAngleRadians_KnownOvalCorner_MatchesHandComputedAngle),
            // using the formula this round replaced the circumradius
            // approach with. Value cross-checked against a full dynamics
            // simulation (sequential waypoint following, PD yaw-response
            // lag, steering error) that confirmed the bot actually
            // completes laps at margin=0.45 without running wide into the
            // wall or stalling in an orbit — margins around 0.55-0.7 did
            // either of those instead, which wasn't obvious from the
            // formula alone.
            var speed = KartBotMath.CalculateMaxCorneringSpeedMetersPerSecond(
                turnAngleRadians: 0.6435f, availableDistanceMeters: 15.81f,
                maxLateralAcceleration: 11.77f, safetyMargin01: 0.45f);

            Assert.That(speed, Is.EqualTo(7.65f).Within(0.05f));
        }

        [Test]
        public void GetCorneringSafetyMargin01_HardIsHighestOfTheThree()
        {
            var easy = KartBotMath.GetCorneringSafetyMargin01(BotDifficulty.Easy);
            var medium = KartBotMath.GetCorneringSafetyMargin01(BotDifficulty.Medium);
            var hard = KartBotMath.GetCorneringSafetyMargin01(BotDifficulty.Hard);

            Assert.That(medium, Is.GreaterThan(easy));
            Assert.That(hard, Is.GreaterThan(medium));
        }

        [Test]
        public void CalculateThrottleForTargetSpeed_FarBelowTarget_IsMaxThrottle()
        {
            var throttle = KartBotMath.CalculateThrottleForTargetSpeed(
                currentSpeedMps: 5f, targetSpeedMps: 20f, minThrottle: 0.4f, maxThrottle: 1f);

            Assert.That(throttle, Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void CalculateThrottleForTargetSpeed_AtOrAboveTarget_IsZero()
        {
            // Round 15: must be 0, not minThrottle — KartDynamics applies
            // throttle and brake as simultaneous, independent forces, so
            // any nonzero throttle here would fight the brake exactly when
            // the bot is trying hardest to shed speed for a corner (root
            // cause of "seguiram reto... não tem noção de fazer curva").
            var throttle = KartBotMath.CalculateThrottleForTargetSpeed(
                currentSpeedMps: 22f, targetSpeedMps: 20f, minThrottle: 0.4f, maxThrottle: 1f);

            Assert.That(throttle, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void CalculateThrottleForTargetSpeed_ExactlyAtTarget_IsZero()
        {
            var throttle = KartBotMath.CalculateThrottleForTargetSpeed(
                currentSpeedMps: 20f, targetSpeedMps: 20f, minThrottle: 0.4f, maxThrottle: 1f);

            Assert.That(throttle, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void CalculateBrakeForTargetSpeed_BelowTarget_IsZero()
        {
            var brake = KartBotMath.CalculateBrakeForTargetSpeed(
                currentSpeedMps: 10f, targetSpeedMps: 20f, maxBrakeInput: 0.6f);

            Assert.That(brake, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void CalculateBrakeForTargetSpeed_FarAboveTarget_IsMaxBrake()
        {
            var brake = KartBotMath.CalculateBrakeForTargetSpeed(
                currentSpeedMps: 25f, targetSpeedMps: 20f, maxBrakeInput: 0.6f);

            Assert.That(brake, Is.EqualTo(0.6f).Within(0.001f));
        }

        [Test]
        public void IsWithinCorneringLookahead_CloseDistance_IsTrue()
        {
            var within = KartBotMath.IsWithinCorneringLookahead(
                distanceToTargetMeters: 5f, currentSpeedMps: 15f, lookaheadSeconds: 1.6f);

            Assert.That(within, Is.True);
        }

        [Test]
        public void IsWithinCorneringLookahead_FarDistance_IsFalse()
        {
            var within = KartBotMath.IsWithinCorneringLookahead(
                distanceToTargetMeters: 50f, currentSpeedMps: 15f, lookaheadSeconds: 1.6f);

            Assert.That(within, Is.False);
        }

        [Test]
        public void IsWithinCorneringLookahead_StoppedKart_StillHasMinimalLookahead()
        {
            // At 0 speed the scaled lookahead would be 0 — must still cover
            // a small floor so a bot starting right at a corner reacts.
            var within = KartBotMath.IsWithinCorneringLookahead(
                distanceToTargetMeters: 0.5f, currentSpeedMps: 0f, lookaheadSeconds: 1.6f);

            Assert.That(within, Is.True);
        }

        // Round 22 ("no médio/difícil os bots ficam confusos, no fácil
        // não") — geometric racecraft gate tests. See
        // KartBotMath.ShouldApplyRacecraftBias XML doc for the diagnosis:
        // the speed-based cornering gate alone didn't reliably suppress
        // racecraft on this track's continuous gentle curve.
        [Test]
        public void ShouldApplyRacecraftBias_CorneringPhase_IsFalseRegardlessOfBend()
        {
            var result = KartBotMath.ShouldApplyRacecraftBias(
                isCorneringPhase: true, turnAngleRadians: 0f, maxBendRadiansForRacecraft: 0.1f);

            Assert.That(result, Is.False);
        }

        [Test]
        public void ShouldApplyRacecraftBias_StraightSegment_IsTrue()
        {
            // A true straight has ~0 turn angle between consecutive waypoints.
            var result = KartBotMath.ShouldApplyRacecraftBias(
                isCorneringPhase: false, turnAngleRadians: 0f, maxBendRadiansForRacecraft: 0.1f);

            Assert.That(result, Is.True);
        }

        [Test]
        public void ShouldApplyRacecraftBias_GentleArcSegment_IsFalse()
        {
            // The round-20/21 stadium track's arc segments each turn ~15°
            // (180° / 12 segments per end) — well past a 0.1 rad (~5.7°)
            // threshold, so racecraft should be suppressed here even though
            // no sharp, speed-limited corner is imminent.
            var fifteenDegreesInRadians = 15f * Mathf.Deg2Rad;

            var result = KartBotMath.ShouldApplyRacecraftBias(
                isCorneringPhase: false, turnAngleRadians: fifteenDegreesInRadians, maxBendRadiansForRacecraft: 0.1f);

            Assert.That(result, Is.False);
        }

        [Test]
        public void ShouldApplyRacecraftBias_BendExactlyAtThreshold_IsTrue()
        {
            var result = KartBotMath.ShouldApplyRacecraftBias(
                isCorneringPhase: false, turnAngleRadians: 0.1f, maxBendRadiansForRacecraft: 0.1f);

            Assert.That(result, Is.True);
        }

        [Test]
        public void ShouldApplyRacecraftBias_NegativeTurnAngle_UsesAbsoluteValue()
        {
            // CalculateTurnAngleRadians only ever returns 0..π (Acos of a
            // dot product), but the gate should be robust to a
            // hypothetical signed input anyway rather than silently letting
            // a sharp negative bend through.
            var result = KartBotMath.ShouldApplyRacecraftBias(
                isCorneringPhase: false, turnAngleRadians: -0.3f, maxBendRadiansForRacecraft: 0.1f);

            Assert.That(result, Is.False);
        }

        // Round 23 ("ele automaticamente bateu na traseira do meu carro na
        // largada", testing Hard with 9 bots): tried a HasClearedStartingGrid
        // gate + tests here. Reverted the same round after it caused a worse
        // regression (Hard bots piling into the grass at the first corner) —
        // see KartBotController's round-23 comment. Function and tests
        // removed together.

        // Round 18 ("pilotagem agressiva, bloquear, tentar se manter na
        // frente parece muito mecanico") — racecraft lateral-offset tests.
        [Test]
        public void GetRacecraftAggressiveness01_EasyIsZero_HardIsHighestOfTheThree()
        {
            var easy = KartBotMath.GetRacecraftAggressiveness01(BotDifficulty.Easy);
            var medium = KartBotMath.GetRacecraftAggressiveness01(BotDifficulty.Medium);
            var hard = KartBotMath.GetRacecraftAggressiveness01(BotDifficulty.Hard);

            Assert.That(easy, Is.EqualTo(0f).Within(0.001f));
            Assert.That(medium, Is.GreaterThan(easy));
            Assert.That(hard, Is.GreaterThan(medium));
        }

        [Test]
        public void CalculateDefensiveLateralOffsetMeters_RivalCommittedRightBehind_BlocksRight()
        {
            var offset = KartBotMath.CalculateDefensiveLateralOffsetMeters(
                rivalDistanceBehindMeters: 6f, rivalLateralOffsetMeters: 1.2f,
                engagementRangeMeters: 12f, maxOffsetMeters: 1.6f, aggressiveness01: 0.85f);

            Assert.That(offset, Is.EqualTo(0.544f).Within(0.01f));
        }

        [Test]
        public void CalculateDefensiveLateralOffsetMeters_RivalCenteredBehind_IsZero()
        {
            // Nothing to block yet — the rival hasn't committed to a side.
            var offset = KartBotMath.CalculateDefensiveLateralOffsetMeters(
                rivalDistanceBehindMeters: 6f, rivalLateralOffsetMeters: 0f,
                engagementRangeMeters: 12f, maxOffsetMeters: 1.6f, aggressiveness01: 0.85f);

            Assert.That(offset, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void CalculateDefensiveLateralOffsetMeters_RivalOutOfRange_IsZero()
        {
            var offset = KartBotMath.CalculateDefensiveLateralOffsetMeters(
                rivalDistanceBehindMeters: 20f, rivalLateralOffsetMeters: 1f,
                engagementRangeMeters: 12f, maxOffsetMeters: 1.6f, aggressiveness01: 0.85f);

            Assert.That(offset, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void CalculateDefensiveLateralOffsetMeters_ZeroAggressiveness_IsZero()
        {
            // Easy bots (aggressiveness 0) never block, no matter how close
            // or committed the rival is.
            var offset = KartBotMath.CalculateDefensiveLateralOffsetMeters(
                rivalDistanceBehindMeters: 6f, rivalLateralOffsetMeters: 1f,
                engagementRangeMeters: 12f, maxOffsetMeters: 1.6f, aggressiveness01: 0f);

            Assert.That(offset, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void CalculateDefensiveLateralOffsetMeters_NeverExceedsMaxOffset()
        {
            // Rival right on the bumper and fully committed to a side —
            // the worst case — must still stay within the cap.
            var offset = KartBotMath.CalculateDefensiveLateralOffsetMeters(
                rivalDistanceBehindMeters: 0.5f, rivalLateralOffsetMeters: 3f,
                engagementRangeMeters: 12f, maxOffsetMeters: 1.6f, aggressiveness01: 1f);

            Assert.That(Mathf.Abs(offset), Is.LessThanOrEqualTo(1.6f));
        }

        [Test]
        public void CalculateOvertakingLateralOffsetMeters_RivalCenteredAhead_UsesPreferredSide()
        {
            var offset = KartBotMath.CalculateOvertakingLateralOffsetMeters(
                rivalDistanceAheadMeters: 6f, rivalLateralOffsetMeters: 0.1f,
                engagementRangeMeters: 12f, maxOffsetMeters: 1.6f, aggressiveness01: 0.85f,
                preferredPassSideSign: 1f);

            Assert.That(offset, Is.EqualTo(0.68f).Within(0.01f));
        }

        [Test]
        public void CalculateOvertakingLateralOffsetMeters_RivalCommittedRightAhead_AttacksOppositeSide()
        {
            // Rival has already parked on the right — attacking bot swings
            // left instead, regardless of its own "preferred" side.
            var offset = KartBotMath.CalculateOvertakingLateralOffsetMeters(
                rivalDistanceAheadMeters: 6f, rivalLateralOffsetMeters: 1.2f,
                engagementRangeMeters: 12f, maxOffsetMeters: 1.6f, aggressiveness01: 0.85f,
                preferredPassSideSign: 1f);

            Assert.That(offset, Is.EqualTo(-0.68f).Within(0.01f));
        }

        [Test]
        public void CalculateOvertakingLateralOffsetMeters_RivalOutOfRange_IsZero()
        {
            var offset = KartBotMath.CalculateOvertakingLateralOffsetMeters(
                rivalDistanceAheadMeters: 20f, rivalLateralOffsetMeters: 0.1f,
                engagementRangeMeters: 12f, maxOffsetMeters: 1.6f, aggressiveness01: 0.85f,
                preferredPassSideSign: 1f);

            Assert.That(offset, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void CalculateOvertakingLateralOffsetMeters_NoRivalAhead_IsZero()
        {
            // Distance of 0 (or negative) is how KartBotController signals
            // "no rival found" — must not be misread as "rival right on
            // top of us".
            var offset = KartBotMath.CalculateOvertakingLateralOffsetMeters(
                rivalDistanceAheadMeters: 0f, rivalLateralOffsetMeters: 0f,
                engagementRangeMeters: 12f, maxOffsetMeters: 1.6f, aggressiveness01: 0.85f,
                preferredPassSideSign: 1f);

            Assert.That(offset, Is.EqualTo(0f).Within(0.001f));
        }

        // Round 25 (2026-08-24) founder feedback: "talvez um mecanismo para
        // recentralizar o kart se caso ele travar ou tiver voltando pra
        // trás, pq ele está sem referência total".

        [Test]
        public void DistanceToNearestPathPointMeters_PositionOnPath_IsZero()
        {
            var path = new List<Vector3>
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(100f, 0f, 0f),
                new Vector3(100f, 0f, 100f),
                new Vector3(0f, 0f, 100f),
            };

            var distance = KartBotMath.DistanceToNearestPathPointMeters(new Vector3(50f, 0f, 0f), path);

            Assert.That(distance, Is.EqualTo(0f).Within(0.01f));
        }

        [Test]
        public void DistanceToNearestPathPointMeters_PositionOffPath_ReturnsPerpendicularDistance()
        {
            var path = new List<Vector3>
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(100f, 0f, 0f),
                new Vector3(100f, 0f, 100f),
                new Vector3(0f, 0f, 100f),
            };

            var distance = KartBotMath.DistanceToNearestPathPointMeters(new Vector3(50f, 0f, 30f), path);

            Assert.That(distance, Is.EqualTo(30f).Within(0.01f));
        }

        [Test]
        public void DistanceToNearestPathPointMeters_NullPath_ReturnsZero()
        {
            var distance = KartBotMath.DistanceToNearestPathPointMeters(new Vector3(50f, 0f, 30f), null);

            Assert.That(distance, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void DistanceToNearestPathPointMeters_EmptyPath_ReturnsZero()
        {
            var distance = KartBotMath.DistanceToNearestPathPointMeters(new Vector3(50f, 0f, 30f), new List<Vector3>());

            Assert.That(distance, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void DistanceToNearestPathPointMeters_SinglePointPath_ReturnsDistanceToThatPoint()
        {
            var path = new List<Vector3> { new Vector3(10f, 0f, 0f) };

            var distance = KartBotMath.DistanceToNearestPathPointMeters(new Vector3(10f, 0f, 6f), path);

            Assert.That(distance, Is.EqualTo(6f).Within(0.01f));
        }

        [Test]
        public void IsFacingAgainstPathDirection_FacingSameWayAsPath_IsFalse()
        {
            var path = new List<Vector3>
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(100f, 0f, 0f),
            };

            var isBackward = KartBotMath.IsFacingAgainstPathDirection(
                new Vector3(50f, 0f, 0f), new Vector3(1f, 0f, 0f), path);

            Assert.That(isBackward, Is.False);
        }

        [Test]
        public void IsFacingAgainstPathDirection_FacingOppositeOfPath_IsTrue()
        {
            var path = new List<Vector3>
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(100f, 0f, 0f),
            };

            // Path direction here is +X; a kart facing -X is driving
            // straight back toward where it came from.
            var isBackward = KartBotMath.IsFacingAgainstPathDirection(
                new Vector3(50f, 0f, 0f), new Vector3(-1f, 0f, 0f), path);

            Assert.That(isBackward, Is.True);
        }

        [Test]
        public void IsFacingAgainstPathDirection_FacingSideways_IsFalse()
        {
            var path = new List<Vector3>
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(100f, 0f, 0f),
            };

            // 90 degrees off is imprecise steering, not "backward" — must
            // not trip the coarse ~120 degree threshold.
            var isBackward = KartBotMath.IsFacingAgainstPathDirection(
                new Vector3(50f, 0f, 0f), new Vector3(0f, 0f, 1f), path);

            Assert.That(isBackward, Is.False);
        }

        [Test]
        public void IsFacingAgainstPathDirection_TooShortPath_IsFalse()
        {
            var path = new List<Vector3> { new Vector3(0f, 0f, 0f) };

            var isBackward = KartBotMath.IsFacingAgainstPathDirection(Vector3.zero, new Vector3(-1f, 0f, 0f), path);

            Assert.That(isBackward, Is.False);
        }

        [Test]
        public void IsFacingAgainstPathDirection_NullPath_IsFalse()
        {
            var isBackward = KartBotMath.IsFacingAgainstPathDirection(Vector3.zero, new Vector3(-1f, 0f, 0f), null);

            Assert.That(isBackward, Is.False);
        }
    }
}
