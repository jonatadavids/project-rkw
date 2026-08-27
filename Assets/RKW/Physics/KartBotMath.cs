using System.Collections.Generic;
using UnityEngine;

namespace RKW.Physics
{
    /// <summary>
    /// Founder playtest feedback, 2026-08-19: "o kart nivel rápido acerta
    /// todas as curvas... não que ele fique mais rápido, mas um certo
    /// padrão". Difficulty mainly differentiates bots by cornering
    /// precision (<see cref="KartBotMath.GetSteeringErrorDegrees"/>,
    /// <see cref="KartBotMath.GetArrivalRadiusMeters"/>), not raw speed.
    /// Follow-up feedback, 2026-08-20: "o modo dificil nao me parece tao
    /// dificil assim meu carro parece que corre mais que os bots" — bots
    /// were capped well below the player's throttle ceiling regardless of
    /// difficulty, so even a Hard bot with a perfect line couldn't keep
    /// pace. <see cref="KartBotMath.GetMaxThrottle"/> now also scales with
    /// difficulty, with Hard reaching the player's full throttle.
    /// </summary>
    public enum BotDifficulty
    {
        Easy,
        Medium,
        Hard
    }

    /// <summary>
    /// Pure waypoint-following logic for the demo bot kart (founder playtest
    /// feedback, 2026-08-19: "poderia ter pelo menos 1 bot competindo").
    /// This is a lightweight prototype driver for playtest excitement —
    /// deliberately NOT the M4 bot AI milestone (5 profiles, error
    /// modelling, rule compliance). No Unity lifecycle dependency, so it is
    /// fully EditMode testable.
    /// </summary>
    public static class KartBotMath
    {
        /// <summary>
        /// Signed steering value (-1..1) to turn the kart's forward vector
        /// toward <paramref name="targetPosition"/>. Positive = steer right.
        /// </summary>
        public static float CalculateSteeringToTarget(
            Vector3 currentPosition, Vector3 currentForward, Vector3 targetPosition, float maxSteeringMagnitude)
        {
            var toTarget = targetPosition - currentPosition;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.0001f)
            {
                return 0f;
            }

            var forward = currentForward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
            {
                return 0f;
            }
            forward.Normalize();
            toTarget.Normalize();

            var right = new Vector3(forward.z, 0f, -forward.x);
            var signedAngleDegrees = Mathf.Atan2(Vector3.Dot(toTarget, right), Vector3.Dot(toTarget, forward)) * Mathf.Rad2Deg;

            // Full steering lock by 60 degrees off-target.
            var normalized = Mathf.Clamp(signedAngleDegrees / 60f, -1f, 1f);
            var clampedMagnitude = Mathf.Clamp01(maxSteeringMagnitude);
            return Mathf.Clamp(normalized, -clampedMagnitude, clampedMagnitude);
        }

        /// <summary>True once the kart is within <paramref name="arrivalRadiusMeters"/> of the waypoint (XZ plane).</summary>
        public static bool HasReachedWaypoint(Vector3 currentPosition, Vector3 waypointPosition, float arrivalRadiusMeters)
        {
            var dx = currentPosition.x - waypointPosition.x;
            var dz = currentPosition.z - waypointPosition.z;
            var radius = Mathf.Max(0f, arrivalRadiusMeters);
            return (dx * dx + dz * dz) <= radius * radius;
        }

        /// <summary>
        /// Bots lift off the throttle in proportion to how hard they're
        /// steering — a crude but effective way to avoid a bot that plows
        /// straight through corners at full throttle.
        /// </summary>
        public static float CalculateThrottle(float absSteering01, float minThrottle, float maxThrottle)
        {
            return Mathf.Lerp(maxThrottle, minThrottle, Mathf.Clamp01(absSteering01));
        }

        /// <summary>Advances to the next index in a closed loop of waypoints.</summary>
        public static int AdvanceWaypointIndex(int currentIndex, int waypointCount)
        {
            if (waypointCount <= 0)
            {
                return 0;
            }

            return (currentIndex + 1) % waypointCount;
        }

        /// <summary>
        /// Round-20 founder feedback: "os bots nao entenderam a pista
        /// automaticamente na largada foram para o lado" — <see cref="KartBotController.Configure"/>
        /// used to always start a bot targeting waypoint index 0, which
        /// only worked by coincidence on the old tracks where waypoint 0
        /// happened to sit near/ahead of the grid. The round-20 stadium
        /// track's waypoint 0 is the far end of the south straight, BEHIND
        /// every grid slot — bots spawned facing forward but immediately
        /// steered toward a point behind them, i.e. "went sideways" at the
        /// green light.
        ///
        /// The general, track-shape-independent fix: find which EDGE of
        /// the closed path (waypoint[i] to waypoint[i+1]) the kart is
        /// currently closest to (nearest point on the segment, not nearest
        /// vertex — nearest-vertex would have the same bug, since the
        /// nearest vertex to a kart parked mid-straight can easily be the
        /// "wrong" one behind it), and return the index of that edge's
        /// START. The caller then targets the waypoint one step ahead
        /// (<see cref="AdvanceWaypointIndex"/>) — i.e. drive forward along
        /// whichever leg of the path the kart actually starts on, exactly
        /// like a real racing game snapping a car onto the nearest point
        /// of the track spline. Works for any spawn position on any
        /// closed-loop path, not just this track's grid.
        /// </summary>
        public static int FindNearestPathSegmentStartIndex(Vector3 position, IReadOnlyList<Vector3> path)
        {
            if (path == null || path.Count == 0)
            {
                return 0;
            }

            if (path.Count == 1)
            {
                return 0;
            }

            var bestIndex = 0;
            var bestDistanceSquared = float.MaxValue;
            for (var i = 0; i < path.Count; i++)
            {
                var a = path[i];
                var b = path[(i + 1) % path.Count];
                var distanceSquared = SquaredDistanceToSegmentXZ(position, a, b);
                if (distanceSquared < bestDistanceSquared)
                {
                    bestDistanceSquared = distanceSquared;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        /// <summary>
        /// Founder request, 2026-08-24 (round 25): "talvez um mecanismo
        /// para recentralizar o kart se caso ele travar ou tiver voltando
        /// pra trás, pq ele está sem referência total... tem que ter
        /// alguma maneira de dar inteligência pra ele e que essa
        /// inteligência permaneça no aumento da pista". Distance (meters,
        /// XZ plane) from a point to the nearest point anywhere on the
        /// closed-loop path — used by <see cref="KartBotController"/> to
        /// detect a bot that's badly off-course (knocked into the infield
        /// by a collision, spun out past the barrier), as distinct from
        /// the existing near-zero-movement "stuck against a wall" check
        /// (<see cref="HasMadeInsufficientProgress"/>), which only catches
        /// a bot that has stopped moving, not one that's moving away from
        /// the track entirely. Reuses the same segment-distance math as
        /// <see cref="FindNearestPathSegmentStartIndex"/> (kept as a
        /// separate function rather than refactored together, to avoid
        /// touching that already-verified function). Track-shape/size
        /// independent — works unchanged after any future
        /// StadiumHalfStraightMeters change (round 25's track expansion
        /// included), since it only ever reasons about <paramref name="path"/>
        /// itself, never a hardcoded distance.
        /// </summary>
        public static float DistanceToNearestPathPointMeters(Vector3 position, IReadOnlyList<Vector3> path)
        {
            if (path == null || path.Count == 0)
            {
                return 0f;
            }

            if (path.Count == 1)
            {
                var dx = position.x - path[0].x;
                var dz = position.z - path[0].z;
                return Mathf.Sqrt(dx * dx + dz * dz);
            }

            var bestDistanceSquared = float.MaxValue;
            for (var i = 0; i < path.Count; i++)
            {
                var a = path[i];
                var b = path[(i + 1) % path.Count];
                var distanceSquared = SquaredDistanceToSegmentXZ(position, a, b);
                if (distanceSquared < bestDistanceSquared)
                {
                    bestDistanceSquared = distanceSquared;
                }
            }

            return Mathf.Sqrt(bestDistanceSquared);
        }

        /// <summary>
        /// Founder request, 2026-08-24 (round 25, same feedback as
        /// <see cref="DistanceToNearestPathPointMeters"/>): "voltando pra
        /// trás" — a bot that gets spun around (collision, a recovery
        /// maneuver that overshot) can end up facing back into the race
        /// instead of along it, without necessarily being "stuck" (it can
        /// still be moving, just the wrong way) or far from the path
        /// (still geometrically close to the track). True once the kart's
        /// forward vector is more than ~120° off the track's own direction
        /// of travel at the nearest point on the path — a much coarser
        /// threshold than steering error, deliberately: this only needs to
        /// catch "aimed backward", not "aimed imprecisely".
        /// </summary>
        public static bool IsFacingAgainstPathDirection(Vector3 position, Vector3 forward, IReadOnlyList<Vector3> path)
        {
            if (path == null || path.Count < 2)
            {
                return false;
            }

            var nearestIndex = FindNearestPathSegmentStartIndex(position, path);
            var a = path[nearestIndex];
            var b = path[(nearestIndex + 1) % path.Count];
            var pathDirection = new Vector2(b.x - a.x, b.z - a.z);
            var kartForward = new Vector2(forward.x, forward.z);
            if (pathDirection.sqrMagnitude < 0.0001f || kartForward.sqrMagnitude < 0.0001f)
            {
                return false;
            }

            var dot = Vector2.Dot(pathDirection.normalized, kartForward.normalized);
            return dot < -0.5f;
        }

        /// <summary>Squared distance (XZ plane) from a point to the closest point on segment a-b.</summary>
        private static float SquaredDistanceToSegmentXZ(Vector3 point, Vector3 a, Vector3 b)
        {
            var abx = b.x - a.x;
            var abz = b.z - a.z;
            var lengthSquared = abx * abx + abz * abz;
            if (lengthSquared < 0.0001f)
            {
                var dax = point.x - a.x;
                var daz = point.z - a.z;
                return dax * dax + daz * daz;
            }

            var t = ((point.x - a.x) * abx + (point.z - a.z) * abz) / lengthSquared;
            t = Mathf.Clamp01(t);
            var closestX = a.x + abx * t;
            var closestZ = a.z + abz * t;
            var dx = point.x - closestX;
            var dz = point.z - closestZ;
            return dx * dx + dz * dz;
        }

        /// <summary>
        /// Round-21 founder feedback: "a inteligencia do bot controlada por
        /// matemática parece um desastre kkk". Researched how real racing
        /// games steer along a waypoint path: the standard technique is
        /// "pure pursuit" — steer at a point some lookahead distance AHEAD
        /// on the path, not directly at the next discrete waypoint. Steering
        /// straight at a fixed waypoint (what this bot did before) makes the
        /// aim point jump the instant the bot crosses the arrival radius,
        /// which reads as a jerky snap/overshoot right at corner entry —
        /// exactly the "disaster" symptom reported. This walks forward from
        /// the current target waypoint along the path, accumulating segment
        /// lengths, and returns the point <paramref name="lookaheadDistanceMeters"/>
        /// ahead (interpolated within whichever segment crosses that
        /// distance) — so the steering aim point slides smoothly forward
        /// instead of jumping waypoint to waypoint. Deliberately does NOT
        /// touch <see cref="FindNearestPathSegmentStartIndex"/>/
        /// <see cref="AdvanceWaypointIndex"/>/<see cref="HasReachedWaypoint"/>
        /// (waypoint arrival/lap-counting) or the cornering-speed geometry
        /// in <see cref="KartBotController"/> (still computed from the true
        /// discrete waypoints) — this only changes what point the STEERING
        /// aims at, nothing about progress tracking or braking.
        /// Bounded to at most one full lap of the path so a pathological
        /// lookahead distance (or a 1-2 point path) can't loop forever.
        /// </summary>
        public static Vector3 CalculateLookaheadSteeringTarget(
            IReadOnlyList<Vector3> path, int currentTargetIndex, float lookaheadDistanceMeters)
        {
            if (path == null || path.Count == 0)
            {
                return Vector3.zero;
            }

            if (path.Count == 1 || lookaheadDistanceMeters <= 0f)
            {
                return path[Mathf.Clamp(currentTargetIndex, 0, path.Count - 1)];
            }

            var index = Mathf.Clamp(currentTargetIndex, 0, path.Count - 1);
            var point = path[index];
            var remaining = lookaheadDistanceMeters;

            for (var step = 0; step < path.Count; step++)
            {
                var nextIndex = AdvanceWaypointIndex(index, path.Count);
                var next = path[nextIndex];
                var segmentLength = Vector3.Distance(point, next);
                if (segmentLength >= remaining)
                {
                    var t = segmentLength > 0.0001f ? remaining / segmentLength : 0f;
                    return Vector3.Lerp(point, next, t);
                }

                remaining -= segmentLength;
                point = next;
                index = nextIndex;
            }

            // Lookahead distance exceeds the whole path's length (e.g. a
            // tiny test path with a huge lookahead) — furthest reachable
            // point is as good as this can do.
            return point;
        }

        /// <summary>
        /// Maximum random steering error (degrees, applied once per
        /// waypoint) for a difficulty level. Hard = 0 (drives the ideal
        /// line every corner). Founder playtest feedback, 2026-08-20: "os
        /// bots continuam bem burros... nunca completam a volta" — the
        /// original values here (Easy 16, Medium 6) were sized without
        /// checking them against the track: some waypoint legs are 40+
        /// meters long, and the track is only ~7m wide, so a HELD bias of
        /// even a few degrees compounds into meters of drift on the long
        /// straights — enough to run off track before the bot ever
        /// self-corrects. These are ~4x smaller so "less precise" reads as
        /// a slightly wobblier line, not a bot that drives into the grass.
        /// </summary>
        public static float GetSteeringErrorDegrees(BotDifficulty difficulty)
        {
            switch (difficulty)
            {
                case BotDifficulty.Easy:
                    return 4f;
                case BotDifficulty.Medium:
                    return 1.5f;
                case BotDifficulty.Hard:
                    return 0f;
                default:
                    return 0f;
            }
        }

        /// <summary>Wider arrival radius = cuts corners/apexes less precisely at lower difficulty.</summary>
        public static float GetArrivalRadiusMeters(BotDifficulty difficulty)
        {
            switch (difficulty)
            {
                case BotDifficulty.Easy:
                    return 5.5f;
                case BotDifficulty.Medium:
                    return 4f;
                case BotDifficulty.Hard:
                    return 2.5f;
                default:
                    return 4f;
            }
        }

        /// <summary>
        /// Highest throttle input a bot at this difficulty will ever use.
        /// Founder playtest feedback, 2026-08-20: bots were capped at 0.85
        /// throttle regardless of difficulty while the player can reach
        /// 1.0, so "difficult" bots structurally could not outrun the
        /// player even with a perfect line. Hard now matches the player's
        /// ceiling; Easy/Medium stay lower so difficulty is still readable
        /// on the straights, not just in the corners.
        /// </summary>
        public static float GetMaxThrottle(BotDifficulty difficulty)
        {
            switch (difficulty)
            {
                case BotDifficulty.Easy:
                    return 0.72f;
                case BotDifficulty.Medium:
                    return 0.88f;
                case BotDifficulty.Hard:
                    return 1f;
                default:
                    return 0.85f;
            }
        }

        /// <summary>Lowest throttle a bot lifts to mid-corner; scales gently with difficulty alongside <see cref="GetMaxThrottle"/>.</summary>
        public static float GetMinThrottle(BotDifficulty difficulty)
        {
            switch (difficulty)
            {
                case BotDifficulty.Easy:
                    return 0.3f;
                case BotDifficulty.Medium:
                    return 0.35f;
                case BotDifficulty.Hard:
                    return 0.4f;
                default:
                    return 0.35f;
            }
        }

        /// <summary>
        /// How sharp the upcoming turn is, from 0 (straight on) to 1 (a
        /// hairpin/reversal), based on the heading change between the
        /// approach into <paramref name="target"/> and the departure toward
        /// <paramref name="afterTarget"/>. Lets a bot start slowing before
        /// it is already mid-corner, instead of only reacting to how hard
        /// it happens to be steering right now.
        /// </summary>
        public static float CalculateCornerSharpness01(Vector3 fromPosition, Vector3 target, Vector3 afterTarget)
        {
            var into = target - fromPosition;
            into.y = 0f;
            var outOf = afterTarget - target;
            outOf.y = 0f;

            if (into.sqrMagnitude < 0.0001f || outOf.sqrMagnitude < 0.0001f)
            {
                return 0f;
            }

            into.Normalize();
            outOf.Normalize();

            var dot = Mathf.Clamp(Vector3.Dot(into, outOf), -1f, 1f);
            var turnAngleDegrees = Mathf.Acos(dot) * Mathf.Rad2Deg;
            // Treat a 110-degree-or-sharper heading change as "as sharp as it gets".
            return Mathf.Clamp01(turnAngleDegrees / 110f);
        }

        /// <summary>
        /// Founder playtest feedback, 2026-08-20: bots ran off track in
        /// corners even before this fix reduced the steering error above —
        /// they were only ever lifting throttle in reaction to already
        /// steering hard, with no anticipation and no braking at all. This
        /// takes the lower of the reactive (steering-based) and anticipatory
        /// (upcoming corner sharpness) throttle so a sharp turn slows the
        /// bot down before it arrives, not just once it is already sliding.
        /// </summary>
        public static float CalculateCornerAwareThrottle(
            float absSteering01, float cornerSharpness01, float minThrottle, float maxThrottle)
        {
            var reactiveThrottle = CalculateThrottle(absSteering01, minThrottle, maxThrottle);
            var anticipatoryThrottle = CalculateThrottle(cornerSharpness01, minThrottle, maxThrottle);
            return Mathf.Min(reactiveThrottle, anticipatoryThrottle);
        }

        /// <summary>
        /// Light anticipatory braking, only when both going fast AND
        /// approaching a sharp corner — avoids braking hard on gentle kinks
        /// or while already slow.
        /// </summary>
        public static float CalculateCornerBrake(float cornerSharpness01, float speedRatio01, float maxBrakeInput)
        {
            var demand = Mathf.Clamp01(cornerSharpness01) * Mathf.Clamp01(speedRatio01);
            return demand * Mathf.Clamp01(maxBrakeInput);
        }

        /// <summary>
        /// Adds a held per-waypoint steering error (in degrees, converted
        /// using the same 60-degrees-to-full-lock scale as
        /// <see cref="CalculateSteeringToTarget"/>) to the ideal steering
        /// value, then re-clamps to the allowed magnitude.
        /// </summary>
        public static float ApplySteeringError(float baseSteering, float errorDegrees, float maxSteeringMagnitude)
        {
            var errorNormalized = errorDegrees / 60f;
            var clampedMagnitude = Mathf.Clamp01(maxSteeringMagnitude);
            return Mathf.Clamp(baseSteering + errorNormalized, -clampedMagnitude, clampedMagnitude);
        }

        /// <summary>True when the bot has moved less than the threshold since the last progress check — used to detect it wedged against track geometry.</summary>
        public static bool HasMadeInsufficientProgress(float distanceMovedMeters, float minimumProgressMeters)
        {
            return distanceMovedMeters < Mathf.Max(0f, minimumProgressMeters);
        }

        /// <summary>Steps backward in a closed loop of waypoints. Pair to <see cref="AdvanceWaypointIndex"/>.</summary>
        public static int PreviousWaypointIndex(int currentIndex, int waypointCount)
        {
            if (waypointCount <= 0)
            {
                return 0;
            }

            return (currentIndex - 1 + waypointCount) % waypointCount;
        }

        // Founder playtest feedback, 2026-08-20 (round 14): "os bots
        // continuam sem competitividade... nunca conseguem ganhar de mim...
        // hoje nao tem graça brincar pq corro com bots porém sem
        // adversário". The old throttle model (CalculateCornerAwareThrottle
        // above) scaled speed off an arbitrary 0..1 "sharpness" heuristic
        // completely disconnected from the actual traction-limited
        // cornering physics the PLAYER is racing against (see
        // KartDynamicsMath.LimitYawRateToAvailableGrip, round 10) — so even
        // a "Hard" bot with a perfect line was capped well below what the
        // track's grip actually allows in most corners. The functions below
        // instead derive a real cornering speed budget (v = sqrt(radius *
        // grip), the same formula that governs the player's own traction
        // limit) directly from the upcoming waypoints' geometry, so bot
        // pace scales correctly with grip/tuning and with ANY future track
        // layout, not just this oval.
        // Founder playtest feedback, 2026-08-20 (round 16): "eles nao tem
        // inteligencia alguma para finalizar a prova eles simplismente vao
        // reto e param na parede e fica batendo na parede como se quisesse
        // atravessar". Root-caused this time with an actual dynamics
        // simulation (not just formula inspection) reproducing the bug —
        // running the full sequential waypoint-following loop, not just
        // inspecting one corner's formula in isolation, is what actually
        // caught it. CalculateCornerRadiusMeters's circumradius-through-3-
        // waypoints computed a lazy 25m for the oval's NE apex — on its
        // own that number isn't absurd, but fed through the full lap (this
        // corner is one of three ~37° waypoint-level turns the oval uses
        // to approximate its wider east-side bend, each with a fairly
        // short approach leg) it still asked the kart to arrive faster
        // than it could actually shed speed for and turn through in the
        // real distance available before the outer wall — and margins
        // that looked safe from the formula alone (~0.55-0.7) turned out
        // to trap the bot in a stable circling orbit around the waypoint
        // instead, forever short of the arrival radius. Replaced with a
        // formula grounded in what actually has to happen physically:
        // given the REQUIRED heading change (turn angle) and the REAL
        // distance
        // available to complete it, what's the fastest entry speed a kart
        // with this much grip can arrive at without running out of road?
        // (v² = a·d/θ — same derivation as v²=a·r, but r is replaced by
        // d/θ, the arc radius implied by actually needing to turn θ
        // radians within d meters, which is the quantity that actually
        // matters here). Margins below were then found by sweeping the
        // simulation (not guessed): the "obvious" 0.8/0.62/0.5 chosen in
        // round 14/15 by inspection was still enough to make Hard and
        // Medium run wide into the wall, AND a narrow "obviously safer"
        // band (~0.65-0.7) turned out to trap the bot in a stable orbit
        // around the corner waypoint forever (never quite reaching arrival
        // radius) — neither failure mode was obvious from the math alone.
        // Values here have real margin below the sweep's observed safe
        // threshold, including PD yaw-response lag (KartDynamics uses a
        // YawResponse/YawDamping controller, not instant yaw — the earlier
        // sweep used instant yaw and was too optimistic) and steering
        // error at Easy/Medium.
        private const float StraightSpeedSentinelMetersPerSecond = 1000f; // effectively "no cornering limit"

        /// <summary>
        /// Heading change (radians, 0..π) between the "before→at" and
        /// "at→after" legs of the path in the XZ plane. 0 = straight
        /// through; π = a full reversal (hairpin). This is the quantity
        /// that actually determines how tight a corner is — see the
        /// round-16 comment above for why a circumradius through the same
        /// three points was misleading.
        /// </summary>
        public static float CalculateTurnAngleRadians(Vector3 before, Vector3 at, Vector3 after)
        {
            var into = new Vector2(at.x - before.x, at.z - before.z);
            var outOf = new Vector2(after.x - at.x, after.z - at.z);

            if (into.sqrMagnitude < 0.0001f || outOf.sqrMagnitude < 0.0001f)
            {
                return 0f;
            }

            var dot = Mathf.Clamp(Vector2.Dot(into.normalized, outOf.normalized), -1f, 1f);
            return Mathf.Acos(dot);
        }

        /// <summary>
        /// Physically-grounded max cornering speed (m/s): the fastest a
        /// kart with <paramref name="maxLateralAcceleration"/> of available
        /// grip can be going and still complete a
        /// <paramref name="turnAngleRadians"/> heading change within
        /// <paramref name="availableDistanceMeters"/> of travel — derived
        /// from v² = a·(d/θ), the same v²=a·r relationship as the player's
        /// own traction limit, with the corner's implied arc radius (d/θ)
        /// grounded in the real turn angle and real distance available
        /// rather than a circumradius through sparse waypoints.
        /// <paramref name="safetyMargin01"/> backs off from the theoretical
        /// limit — see <see cref="GetCorneringSafetyMargin01"/>.
        /// </summary>
        public static float CalculateMaxCorneringSpeedMetersPerSecond(
            float turnAngleRadians, float availableDistanceMeters, float maxLateralAcceleration, float safetyMargin01)
        {
            if (turnAngleRadians < 0.02f)
            {
                return StraightSpeedSentinelMetersPerSecond; // essentially straight — no real limit
            }

            var distance = Mathf.Max(0.1f, availableDistanceMeters);
            var accel = Mathf.Max(0.1f, maxLateralAcceleration);
            var margin = Mathf.Clamp01(safetyMargin01);
            return Mathf.Sqrt(distance * accel / turnAngleRadians) * margin;
        }

        /// <summary>How close to the theoretical grip limit a bot drives corners. Hard bots drive near the edge; Easy bots leave a wide margin.</summary>
        public static float GetCorneringSafetyMargin01(BotDifficulty difficulty)
        {
            switch (difficulty)
            {
                case BotDifficulty.Easy:
                    return 0.3f;
                case BotDifficulty.Medium:
                    return 0.38f;
                case BotDifficulty.Hard:
                    return 0.45f;
                default:
                    return 0.38f;
            }
        }

        private const float ThrottleRampWindowMetersPerSecond = 3f;
        private const float BrakeRampWindowMetersPerSecond = 2f;

        /// <summary>
        /// Throttle blended by how far current speed is below the target
        /// cornering speed — full throttle once 3+ m/s under, ZERO (not
        /// minThrottle) once at or above target. KartDynamics applies
        /// throttle and brake as independent simultaneous forces, so any
        /// nonzero throttle here would fight the brake exactly when it
        /// matters most — see the round-15 comment above
        /// GetCorneringSafetyMargin01.
        /// </summary>
        public static float CalculateThrottleForTargetSpeed(
            float currentSpeedMps, float targetSpeedMps, float minThrottle, float maxThrottle)
        {
            if (currentSpeedMps >= targetSpeedMps)
            {
                return 0f;
            }

            var errorMps = targetSpeedMps - currentSpeedMps;
            var t = Mathf.InverseLerp(0f, ThrottleRampWindowMetersPerSecond, errorMps);
            return Mathf.Lerp(minThrottle, maxThrottle, Mathf.Clamp01(t));
        }

        /// <summary>Brake ramped in over a 2 m/s overspeed window once current speed exceeds the target cornering speed — smooth, not on/off.</summary>
        public static float CalculateBrakeForTargetSpeed(
            float currentSpeedMps, float targetSpeedMps, float maxBrakeInput)
        {
            var errorMps = currentSpeedMps - targetSpeedMps;
            var t = Mathf.InverseLerp(0f, BrakeRampWindowMetersPerSecond, errorMps);
            return Mathf.Clamp01(t) * Mathf.Clamp01(maxBrakeInput);
        }

        // Round 40 (2026-08-26) founder feedback: bots reported "loucos"
        // (crazy), and a documented open gap from round 23 remained: "a
        // real 'bateu na traseira' cause is probably a bot simply having
        // no notion of a slow/stationary kart directly ahead at all -- no
        // obstacle-avoidance/braking-for-rival logic exists yet. Left as
        // an open problem for a dedicated round rather than guessing
        // again." This is that dedicated round: a simple following-
        // distance cap, the same idea as a car's adaptive cruise control
        // -- a bot never accelerates past a rival directly ahead once
        // inside a caution zone, and actively brakes once inside the
        // minimum safety gap. Deliberately produces only an additional
        // speed CAP (like the existing cornering targetSpeedMps) rather
        // than its own throttle/brake formulas, so KartBotController can
        // combine it with the cornering cap via a plain Mathf.Min and
        // reuse CalculateThrottleForTargetSpeed/CalculateBrakeForTargetSpeed
        // unchanged for both.
        /// <summary>
        /// Maximum speed a bot should allow itself given a rival directly
        /// ahead, or <see cref="float.PositiveInfinity"/> if no rival is
        /// close/aligned enough to matter (no cap). Returns infinity when
        /// <paramref name="distanceAheadMeters"/> is zero or negative (the
        /// sentinel for "no rival found"), when the rival is not roughly
        /// in the same lane (<paramref name="lateralOffsetMeters"/> beyond
        /// <paramref name="laneHalfWidthMeters"/>), or when it is already
        /// further away than <paramref name="cautionZoneMeters"/>. Inside
        /// the caution zone, caps at the rival's own speed (never
        /// accelerate past a kart directly ahead); inside the tighter
        /// <paramref name="minFollowGapMeters"/>, caps BELOW the rival's
        /// speed proportionally to how deep inside that gap the bot
        /// already is, so it actively brakes and opens the gap back up
        /// instead of just holding station bumper-to-bumper.
        /// </summary>
        public static float CalculateFollowingSafeSpeedMetersPerSecond(
            float distanceAheadMeters, float lateralOffsetMeters, float rivalSpeedMps,
            float laneHalfWidthMeters, float minFollowGapMeters, float cautionZoneMeters)
        {
            if (distanceAheadMeters <= 0f || distanceAheadMeters >= cautionZoneMeters)
            {
                return float.PositiveInfinity;
            }

            if (Mathf.Abs(lateralOffsetMeters) > laneHalfWidthMeters)
            {
                return float.PositiveInfinity;
            }

            var rivalSpeed = Mathf.Max(0f, rivalSpeedMps);

            if (distanceAheadMeters <= minFollowGapMeters)
            {
                var closeness = distanceAheadMeters / Mathf.Max(minFollowGapMeters, 0.01f);
                return rivalSpeed * closeness;
            }

            // Between the minimum gap and the caution zone: simple "never
            // accelerate past a kart directly ahead" cap -- the outer band
            // of a real adaptive cruise control.
            return rivalSpeed;
        }

        /// <summary>
        /// True once the bot is close enough to its target waypoint that
        /// the upcoming corner's speed limit is actually relevant — a
        /// distant corner (still several seconds away at current speed)
        /// should not throttle back a bot that is still on a straight.
        /// Lookahead distance scales with current speed, like a real
        /// braking point does.
        /// </summary>
        public static bool IsWithinCorneringLookahead(
            float distanceToTargetMeters, float currentSpeedMps, float lookaheadSeconds)
        {
            var lookaheadDistance = Mathf.Max(1f, currentSpeedMps * Mathf.Max(0.1f, lookaheadSeconds));
            return distanceToTargetMeters <= lookaheadDistance;
        }

        /// <summary>
        /// Recovery input while stuck: reverse and steer away from whatever
        /// direction it was last trying to turn, so it swings its nose clear
        /// instead of just backing straight into the same obstacle again.
        /// </summary>
        public static float CalculateRecoverySteering(float lastSteeringInput)
        {
            return Mathf.Approximately(lastSteeringInput, 0f) ? 1f : -Mathf.Sign(lastSteeringInput);
        }

        // Founder playtest feedback, 2026-08-20 (round 18): "nao tem nada de
        // pilotagem agressiva, bloquear, tentar se manter na frente parece
        // muito mecanico". Everything above only ever reasons about the
        // track (waypoints, corner geometry) — a bot has zero notion that
        // any other kart exists. The functions below add that: a bot with a
        // rival close behind biases its steering AIM POINT sideways to
        // cover the side the rival has committed to (defend), and a bot
        // with a rival close ahead biases toward a gap to pull alongside
        // (attack). Deliberately kept as a lateral offset applied on top of
        // the existing waypoint aim point — never touches cornering speed,
        // throttle or brake, so it cannot reintroduce the wall-crash class
        // of bug the last two rounds were spent fixing. KartBotController
        // only applies it outside the cornering-braking window (see
        // IsWithinCorneringLookahead) for the same reason.
        private const float RacecraftSideCommitmentThresholdMeters = 0.75f;
        private const float RacecraftDefenseCommitmentRangeMeters = 1.5f;

        /// <summary>How assertively a bot fights for track position. 0 = pure waypoint-follower (no racecraft at all), matching the original prototype behavior.</summary>
        public static float GetRacecraftAggressiveness01(BotDifficulty difficulty)
        {
            switch (difficulty)
            {
                case BotDifficulty.Easy:
                    return 0f;
                case BotDifficulty.Medium:
                    return 0.45f;
                case BotDifficulty.Hard:
                    return 0.85f;
                default:
                    return 0.45f;
            }
        }

        /// <summary>
        /// Round-22 founder feedback: "no médio/difícil os bots ficam
        /// confusos, no fácil não" (confirmed: Easy drives fine, Medium/Hard
        /// get lost — worse the more bots on track). <see cref="GetRacecraftAggressiveness01"/>
        /// is 0 at Easy and rises at Medium/Hard, which points straight at
        /// the racecraft lateral-offset logic below as the cause.
        ///
        /// The existing "don't bias mid-corner" gate (isCorneringPhase, a
        /// SPEED-based lookahead — see IsWithinCorneringLookahead) was
        /// tuned for tracks with a few sharp, localized corners: it only
        /// trips when the very next waypoint demands real braking. The
        /// round-20/21 stadium track's turns are one continuous gentle
        /// curve spread across many short waypoint segments (12 per
        /// semicircle end) — no single segment's speed drop is sharp
        /// enough to trip that gate, so racecraft kept applying its full
        /// lateral bias (up to 1.6m, more at higher aggressiveness) all the
        /// way around the track's narrowest, most curved sections, where a
        /// bot has the least real room to be shoved sideways — reads
        /// exactly like the reported "confusion", and explains why it gets
        /// worse with more bots (more simultaneous nearby rivals to react
        /// to) and worse at higher difficulty (more aggressive offset).
        ///
        /// This adds a second, purely geometric gate on top of the existing
        /// one: skip racecraft whenever the immediate path bend — the same
        /// <paramref name="turnAngleRadians"/> already computed for
        /// cornering speed, reused here rather than recomputed — is past a
        /// small threshold, regardless of speed. On this track a true
        /// straight has ~0 turn angle between consecutive waypoints while
        /// every arc segment has a consistent ~15° turn (180°/12 segments
        /// per end) — a small threshold cleanly separates "actually on a
        /// straight" from "anywhere on the curve", is track-shape
        /// independent (works the same on a future track with different
        /// segment counts/angles), and costs nothing extra since the value
        /// is already computed by the caller for the speed-limit logic.
        /// </summary>
        public static bool ShouldApplyRacecraftBias(bool isCorneringPhase, float turnAngleRadians, float maxBendRadiansForRacecraft)
        {
            if (isCorneringPhase)
            {
                return false;
            }

            return Mathf.Abs(turnAngleRadians) <= Mathf.Max(0f, maxBendRadiansForRacecraft);
        }

        // Round-23: tried a HasClearedStartingGrid gate here (suppress
        // racecraft for the first 25m from spawn) to fix "bateu na traseira
        // do meu carro na largada". Reverted the same round — it caused a
        // worse regression (every Hard bot piling into the grass at the
        // first corner, see KartBotController's round-23 comment for the
        // full diagnosis). Function removed rather than left dead; if this
        // exact approach is revisited later, `git log` has the original.

        /// <summary>
        /// Lateral aim-point offset (meters, +right/-left) to defend against
        /// a rival closing in from behind. Only reacts once the rival has
        /// actually committed to a side (<paramref name="rivalLateralOffsetMeters"/>
        /// past <see cref="RacecraftSideCommitmentThresholdMeters"/> is
        /// still allowed to ramp in below that, via
        /// <see cref="RacecraftDefenseCommitmentRangeMeters"/>) — a rival
        /// sitting dead center behind isn't blockable yet because there's no
        /// side to cover.
        /// </summary>
        public static float CalculateDefensiveLateralOffsetMeters(
            float rivalDistanceBehindMeters, float rivalLateralOffsetMeters,
            float engagementRangeMeters, float maxOffsetMeters, float aggressiveness01)
        {
            if (aggressiveness01 <= 0f || engagementRangeMeters <= 0f)
            {
                return 0f;
            }

            if (rivalDistanceBehindMeters <= 0f || rivalDistanceBehindMeters > engagementRangeMeters)
            {
                return 0f;
            }

            if (Mathf.Approximately(rivalLateralOffsetMeters, 0f))
            {
                return 0f;
            }

            var proximity01 = 1f - Mathf.Clamp01(rivalDistanceBehindMeters / engagementRangeMeters);
            var commitment01 = Mathf.Clamp01(Mathf.Abs(rivalLateralOffsetMeters) / RacecraftDefenseCommitmentRangeMeters);
            var side = Mathf.Sign(rivalLateralOffsetMeters);
            return side * Mathf.Max(0f, maxOffsetMeters) * proximity01 * commitment01 * Mathf.Clamp01(aggressiveness01);
        }

        /// <summary>
        /// Lateral aim-point offset (meters, +right/-left) to pull alongside
        /// a rival close ahead for an overtake attempt. Aims for the
        /// rival's open side if they've already committed to one, otherwise
        /// commits to <paramref name="preferredPassSideSign"/> (a fixed
        /// per-bot choice, so a bot doesn't waffle side to side against a
        /// centered rival).
        /// </summary>
        public static float CalculateOvertakingLateralOffsetMeters(
            float rivalDistanceAheadMeters, float rivalLateralOffsetMeters,
            float engagementRangeMeters, float maxOffsetMeters, float aggressiveness01, float preferredPassSideSign)
        {
            if (aggressiveness01 <= 0f || engagementRangeMeters <= 0f)
            {
                return 0f;
            }

            if (rivalDistanceAheadMeters <= 0f || rivalDistanceAheadMeters > engagementRangeMeters)
            {
                return 0f;
            }

            var proximity01 = 1f - Mathf.Clamp01(rivalDistanceAheadMeters / engagementRangeMeters);
            var side = Mathf.Abs(rivalLateralOffsetMeters) > RacecraftSideCommitmentThresholdMeters
                ? -Mathf.Sign(rivalLateralOffsetMeters)
                : (preferredPassSideSign >= 0f ? 1f : -1f);
            return side * Mathf.Max(0f, maxOffsetMeters) * proximity01 * Mathf.Clamp01(aggressiveness01);
        }
    }
}
