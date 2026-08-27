using System.Collections.Generic;
using UnityEngine;

namespace RKW.Physics
{
    /// <summary>
    /// Minimal waypoint-following AI driver for a demo bot kart (founder
    /// playtest feedback, 2026-08-19). Follows
    /// <see cref="RKW.Track.TrackConfigurationSO.BotPathPoints"/> around the
    /// loop. This is a prototype "something to race against" for playtest
    /// excitement, not the M4 bot AI milestone.
    ///
    /// Two follow-up fixes from the same feedback round:
    /// - Difficulty (<see cref="BotDifficulty"/>) changes cornering
    ///   precision (steering error + arrival radius), never speed.
    /// - Stuck detection: "o bot ficou parado no primeiro obstáculo" — a
    ///   plain waypoint-follower has no way to get itself unstuck once
    ///   physically blocked by track geometry (the chicane walls in this
    ///   case). If the bot makes almost no progress for
    ///   <see cref="StuckDetectionSeconds"/>, it reverses and steers away
    ///   for a moment before resuming normal driving.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(KartDynamics))]
    public sealed class KartBotController : MonoBehaviour
    {
        [SerializeField] private float maxSteeringMagnitude = 1f;
        [SerializeField] private float maxCornerBrake = 0.6f;

        // Founder playtest feedback, 2026-08-20: "o modo dificil nao me
        // parece tao dificil assim meu carro parece que corre mais que os
        // bots" — throttle range is now driven by difficulty (see
        // KartBotMath.GetMinThrottle/GetMaxThrottle) instead of a single
        // fixed pair, so Hard bots can actually match the player's pace.
        // Set by SetDifficulty; the values below are just safe defaults
        // before Configure runs.
        private float minThrottle = 0.35f;
        private float maxThrottle = 0.85f;

        // Founder playtest feedback, 2026-08-20 (round 14): "nao tem graça
        // brincar pq corro com bots porém sem adversário". How many seconds
        // ahead (at current speed) the bot starts reacting to an upcoming
        // corner's speed limit — see KartBotMath.IsWithinCorneringLookahead.
        // Roughly matches a real driver's braking-point reaction window.
        // Round 15 follow-up ("seguiram reto... não tem noção de fazer
        // curva"): bumped from 1.6 to 2.4s — the throttle/brake dilution
        // bug (see KartBotMath.CalculateThrottleForTargetSpeed) meant the
        // old window left too little real margin once the actual, diluted
        // braking distance is accounted for.
        private const float CorneringLookaheadSeconds = 2.4f;

        private const float StuckDetectionSeconds = 1f;
        // Founder playtest feedback, 2026-08-20: "travando toda vez que
        // chegava antes da curva como se tivesse uma barreira ou um delay
        // bem grande" — 1.5m/s (5.4 km/h) was easily crossed by the new
        // anticipatory corner braking legitimately slowing the bot down for
        // a tight turn, so a normal, deliberate slow-down was being
        // misread as "stuck" and triggering the reverse-and-steer-away
        // recovery every time — which looks exactly like hitting an
        // invisible wall. A genuinely wedged bot moves next to nothing;
        // 0.4m/s only fires for that.
        private const float StuckDistanceThresholdMeters = 0.4f;
        private const float RecoveryDurationSeconds = 0.9f;

        // Round 25 (2026-08-24) founder feedback: "talvez um mecanismo
        // para recentralizar o kart se caso ele travar ou tiver voltando
        // pra trás, pq ele está sem referência total". The reverse-and-
        // steer-away recovery below only ever fixes "physically wedged
        // against geometry" — it does nothing for a bot whose _targetIndex
        // itself no longer makes sense (knocked off course by a collision,
        // spun around by an overshot recovery). LostDistanceThresholdMeters
        // is deliberately generous (well past the ~10.5m pavement edge and
        // the ~20.5m outer barrier) so running wide onto grass — normal,
        // not a bug — never trips it; only genuinely "off in the infield/
        // past the wall" counts as lost. MaxConsecutiveStuckBeforeResnap:
        // if the escalating reverse maneuver has failed this many times in
        // a row, the obstruction probably isn't physical at all, it's a
        // stale target — resnap instead of reversing longer and longer.
        private const float LostDistanceThresholdMeters = 30f;
        private const int MaxConsecutiveStuckBeforeResnap = 3;
        private const int MinConsecutiveBackwardFacingChecksBeforeResnap = 2;

        // Founder playtest feedback, 2026-08-20 (round 17): "uns ainda ficam
        // parados batendo no muro". A single fixed 0.9s reverse was enough
        // to unwedge a bot from open track, but not from a spot where the
        // geometry (or another kart) puts it right back against the same
        // wall the moment it resumes — it would get flagged stuck again a
        // second later and repeat the same short reverse forever, which
        // reads exactly like "ramming the wall". If a bot gets stuck again
        // within RecoveryEscalationWindowSeconds of its last recovery
        // ending, each retry now reverses longer (capped at
        // MaxRecoveryDurationSeconds) to actually clear the obstruction
        // instead of nudging the same few centimeters every time.
        private const float RecoveryEscalationWindowSeconds = 4f;
        private const float RecoveryDurationStepSeconds = 0.6f;
        private const float MaxRecoveryDurationSeconds = 2.4f;

        // Founder playtest feedback, 2026-08-20 (round 18): "pilotagem
        // agressiva, bloquear, tentar se manter na frente". How far ahead/
        // behind (meters) another kart has to be before a bot reacts to it
        // at all, and how far it will ever bias its aim point sideways to
        // defend or attack — see KartBotMath.CalculateDefensiveLateralOffsetMeters
        // / CalculateOvertakingLateralOffsetMeters. Track is 7m wide (see
        // TrackConfigurationSO.trackWidthMeters) and the kart's own
        // BoxCollider is 1m wide, so 1.6m of bias plus half the kart's own
        // width still leaves real margin before the track edge.
        private const float RacecraftEngagementRangeMeters = 12f;
        private const float RacecraftMaxLateralOffsetMeters = 1.6f;

        // Round 40 (2026-08-26): "adaptive cruise control" style following
        // cap -- see KartBotMath.CalculateFollowingSafeSpeedMetersPerSecond's
        // doc for the full reasoning (closes the round-23 documented gap:
        // bots had no notion of a slow/stationary kart directly ahead).
        // Reuses RacecraftEngagementRangeMeters above as the caution-zone
        // distance -- same "how far do we care about another kart" scale
        // already established in this class, no reason for a second one.
        // Lane half-width: kart's own BoxCollider is 1m wide (see
        // CreateKartInstance), so 1.3m comfortably covers "roughly in the
        // same lane" without reacting to a rival already well alongside.
        private const float RivalFollowLaneHalfWidthMeters = 1.3f;
        private const float RivalMinFollowGapMeters = 3.5f;

        // Round-22 founder feedback: "no médio/difícil os bots ficam
        // confusos, no fácil não" — see KartBotMath.ShouldApplyRacecraftBias
        // XML doc for the full diagnosis (the speed-based cornering gate
        // above doesn't reliably catch this track's continuous gentle
        // curve). This track's straights have ~0° turn between consecutive
        // waypoints; every arc segment has a consistent ~15° turn
        // (180°/StadiumArcSegmentsPerEnd=12 in KartPhysicsPrototypeBootstrap).
        // 5° comfortably separates "on a straight" from "anywhere on the
        // curve" without being so tight that floating-point noise on a
        // truly straight segment could false-negative it.
        // Mathf.Deg2Rad is not a compile-time constant, so this can't be a
        // `const` like the fields above it — `static readonly` is the
        // idiomatic Unity equivalent for a value computed once at startup.
        private static readonly float MaxBendRadiansForRacecraft = 5f * Mathf.Deg2Rad;

        // Round-21 founder feedback: "a inteligencia do bot controlada por
        // matemática parece um desastre kkk" — researched how real racing
        // games steer bots (see docs/30-founder-playtest-log.md round 21):
        // the standard fix is "pure pursuit" lookahead steering instead of
        // aiming straight at the next discrete waypoint (see
        // KartBotMath.CalculateLookaheadSteeringTarget for the full
        // rationale). Lookahead distance scales with current speed, same
        // pattern as CorneringLookaheadSeconds above (a faster kart needs to
        // look further ahead to steer smoothly), clamped so a stationary/
        // very slow bot still gets a usable lookahead and a very fast one
        // doesn't aim so far ahead it starts cutting corners.
        private const float SteeringLookaheadSeconds = 0.5f;
        private const float MinSteeringLookaheadMeters = 3f;
        private const float MaxSteeringLookaheadMeters = 10f;

        // Round-23 founder feedback (testing Hard difficulty with 9 bots):
        // "ele automaticamente bateu na traseira do meu carro na largada" —
        // tried gating racecraft off entirely for the first 25m from spawn
        // (KartBotMath.HasClearedStartingGrid). Reverted the same round,
        // after the founder retested and reported a WORSE regression: every
        // Hard bot missing the first corner and piling into the grass.
        // Diagnosis: racecraft's lateral bias was the only thing giving
        // bots any sideways separation on the approach straight (Hard has
        // zero steering-error variance, see MinPaceMultiplier below) —
        // disabling it for the whole pre-corner straight let the pack stay
        // perfectly stacked all the way to the corner entry, where they
        // collided with each other instead of turning in. Also, racecraft
        // only ever biases STEERING, never throttle/brake, so it's
        // unlikely to have caused a forward ram in the first place — the
        // real "bateu na traseira" cause is probably a bot simply having no
        // notion of a slow/stationary kart directly ahead at all (no
        // obstacle-avoidance/braking-for-rival logic exists yet). Left as
        // an open problem for a dedicated round rather than guessing again
        // — see docs/30-founder-playtest-log.md round 23 follow-up.

        // Round-23 founder feedback (same test): "passou direto na primeira
        // curva e voltou pra trás" — on Hard, GetSteeringErrorDegrees is
        // exactly 0 (by design: "drives the ideal line every corner"), so
        // every Hard bot follows the identical line at the identical speed
        // with zero randomization between them. Nothing on a straight ever
        // separates two karts with truly identical throttle/steering — the
        // whole pack would stay bumper-to-bumper for the entire race, not
        // just at the start, keeping racecraft constantly engaged even well
        // past the starting grid (e.g. right at the first real corner, with
        // several Hard bots still shoulder-to-shoulder trying to
        // defend/overtake each other at the exact moment they need to turn
        // in — a plausible cause of missing the corner entirely). A small
        // fixed-per-bot throttle ceiling, assigned once at Configure and
        // NEVER exceeding the difficulty's own tuned maximum, gives real
        // pace diversity so the pack naturally spreads out over a straight
        // — same intent as the per-bot steering error already used for
        // cornering, just applied to overall pace instead. Deliberately
        // does NOT touch cornering precision/steering error, so "Hard =
        // ideal line" (the explicit round-19/20 design intent) stays true —
        // this only humanizes pace, never accuracy.
        private const float MinPaceMultiplier = 0.94f;

        private KartDynamics _dynamics;
        private IReadOnlyList<Vector3> _path;
        private int _targetIndex;
        private float _paceMultiplier = 1f;
        private bool _inputEnabled = true;
        private BotDifficulty _difficulty = BotDifficulty.Medium;
        private float _arrivalRadiusMeters = 4f;
        private float _steeringErrorDegrees;
        private float _currentSteeringError;

        private float _stuckCheckTimer;
        private Vector3 _stuckCheckPosition;
        private float _recoveryTimer;
        private float _lastSteering;
        private int _consecutiveStuckCount;
        private float _timeSinceRecoveryEnded;
        // Round 25: consecutive stuck-check ticks (see StuckDetectionSeconds)
        // the bot has been facing against the track's own direction of
        // travel — see KartBotMath.IsFacingAgainstPathDirection.
        private int _consecutiveBackwardFacingChecks;
        // Which side this bot commits to when overtaking a centered rival —
        // fixed per bot for the whole race so it doesn't waffle side to
        // side; see CalculateOvertakingLateralOffsetMeters.
        private float _preferredPassSideSign = 1f;

        public BotDifficulty Difficulty => _difficulty;

        /// <summary>
        /// Founder playtest feedback, 2026-08-20 (round 16): "talvez seja
        /// legal colocar numeros nos carros de forma aleatório". Set once
        /// per race by KartPhysicsPrototypeBootstrap.SpawnBots; 0 means
        /// "not assigned" (falls back to not showing a number).
        /// </summary>
        public int RaceNumber { get; private set; }

        public void SetRaceNumber(int raceNumber)
        {
            RaceNumber = raceNumber;
        }

        /// <summary>Founder playtest feedback, 2026-08-20 (round 8): "quero ver a classificação durante a corrida" — full laps completed around <see cref="_path"/>, for RaceStandingsHud.</summary>
        public int LapsCompleted { get; private set; }

        // Round 25 (2026-08-24) founder feedback: "seria legal ter o tempo
        // do bot a comparacao dele em todas as voltas" — one entry per
        // completed lap, in order, mirroring how TimingManagerLite times
        // the player except this is a plain in-memory list (not persisted)
        // since it only needs to survive for RaceManager's finish-screen
        // comparison table, never across races. Timed the same way as
        // TimingManagerLite's HandleStartFinishCrossing: Time.time minus
        // the timestamp of the previous waypoint-0 crossing (or Configure,
        // for the first lap) — not gated on checkpoint order/validity like
        // the player's timing, since bots always follow the path in order
        // by construction.
        private readonly List<float> _lapTimes = new List<float>();
        private float _currentLapStartTime;

        public IReadOnlyList<float> LapTimes => _lapTimes;

        public void Configure(KartDynamics dynamics, IReadOnlyList<Vector3> path, BotDifficulty difficulty = BotDifficulty.Medium)
        {
            _dynamics = dynamics;
            _path = path;
            // Round-20 founder feedback: "os bots nao entenderam a pista
            // automaticamente na largada foram para o lado" — hardcoding
            // waypoint index 0 as the first target only worked by
            // coincidence on tracks where waypoint 0 happened to sit near
            // the grid; on the round-20 stadium track it's the far end of
            // the south straight, BEHIND every grid slot, so bots steered
            // toward a point behind them at the green light. Snap onto the
            // nearest point of the path instead (see
            // KartBotMath.FindNearestPathSegmentStartIndex) and target one
            // step ahead of that — works for any spawn position on any
            // closed-loop path. Round 25: this is the exact same lookup
            // ResnapToNearestPathPoint uses mid-race for a lost/stuck bot,
            // factored out below so both call sites can't drift apart.
            ResnapToNearestPathPoint();
            LapsCompleted = 0;
            _lapTimes.Clear();
            _currentLapStartTime = Time.time;
            SetDifficulty(difficulty);
            _stuckCheckPosition = dynamics != null ? dynamics.transform.position : Vector3.zero;
            _stuckCheckTimer = 0f;
            _recoveryTimer = 0f;
            _consecutiveStuckCount = 0;
            _consecutiveBackwardFacingChecks = 0;
            _timeSinceRecoveryEnded = RecoveryEscalationWindowSeconds + 1f;
            _preferredPassSideSign = Random.value < 0.5f ? -1f : 1f;
            // Round-23: fixed for the whole race, like _preferredPassSideSign
            // above — never above the difficulty's own tuned ceiling (see
            // MinPaceMultiplier's comment), only ever a little below it.
            _paceMultiplier = Random.Range(MinPaceMultiplier, 1f);
        }

        /// <summary>
        /// Snaps <see cref="_targetIndex"/> onto the path segment nearest
        /// the bot's CURRENT position (not necessarily where it spawned) —
        /// gives it a fresh, correct reference regardless of how it got to
        /// where it is now. Used at spawn (<see cref="Configure"/>) and,
        /// round 25 on, mid-race whenever <see cref="UpdateStuckDetection"/>
        /// decides the bot is lost rather than merely blocked. Uses
        /// <see cref="_dynamics"/>'s transform if set, otherwise this
        /// component's own — both are the same GameObject in every real
        /// call site, this just tolerates Configure running before Awake.
        /// </summary>
        private void ResnapToNearestPathPoint()
        {
            if (_path == null || _path.Count == 0)
            {
                _targetIndex = 0;
                return;
            }

            var position = _dynamics != null ? _dynamics.transform.position : transform.position;
            var nearestSegmentStart = KartBotMath.FindNearestPathSegmentStartIndex(position, _path);
            _targetIndex = KartBotMath.AdvanceWaypointIndex(nearestSegmentStart, _path.Count);
        }

        public void SetDifficulty(BotDifficulty difficulty)
        {
            _difficulty = difficulty;
            _arrivalRadiusMeters = KartBotMath.GetArrivalRadiusMeters(difficulty);
            _steeringErrorDegrees = KartBotMath.GetSteeringErrorDegrees(difficulty);
            _currentSteeringError = Random.Range(-_steeringErrorDegrees, _steeringErrorDegrees);
            minThrottle = KartBotMath.GetMinThrottle(difficulty);
            maxThrottle = KartBotMath.GetMaxThrottle(difficulty);
        }

        public void SetInputEnabled(bool inputEnabled)
        {
            _inputEnabled = inputEnabled;
        }

        private void Awake()
        {
            if (_dynamics == null)
            {
                _dynamics = GetComponent<KartDynamics>();
            }
        }

        private void Update()
        {
            if (_dynamics == null || _path == null || _path.Count == 0)
            {
                return;
            }

            if (!_inputEnabled)
            {
                _dynamics.SetInput(0f, 0f, 0f);
                return;
            }

            if (_recoveryTimer > 0f)
            {
                _recoveryTimer -= Time.deltaTime;
                if (_recoveryTimer <= 0f)
                {
                    _timeSinceRecoveryEnded = 0f;
                }
                var recoverySteering = KartBotMath.CalculateRecoverySteering(_lastSteering);
                _dynamics.SetInput(recoverySteering, 0f, 1f); // reverse + steer away
                return;
            }

            UpdateStuckDetection();

            var target = _path[_targetIndex];
            if (KartBotMath.HasReachedWaypoint(transform.position, target, _arrivalRadiusMeters))
            {
                var previousIndex = _targetIndex;
                _targetIndex = KartBotMath.AdvanceWaypointIndex(_targetIndex, _path.Count);
                if (_targetIndex == 0 && previousIndex == _path.Count - 1)
                {
                    LapsCompleted++;
                    _lapTimes.Add(Time.time - _currentLapStartTime);
                    _currentLapStartTime = Time.time;
                }
                target = _path[_targetIndex];
                // Re-roll the held steering error on every new waypoint so a
                // low-difficulty bot misjudges each corner independently
                // instead of drifting the same way for the whole lap.
                _currentSteeringError = Random.Range(-_steeringErrorDegrees, _steeringErrorDegrees);
            }

            var currentSpeedMps = _dynamics.SpeedKph / 3.6f;
            var distanceToTarget = Vector3.Distance(transform.position, target);
            var isCorneringPhase = KartBotMath.IsWithinCorneringLookahead(
                distanceToTarget, currentSpeedMps, CorneringLookaheadSeconds);

            // Round-16 corner geometry (turn angle from the true waypoints),
            // moved up from where it used to be computed (right before the
            // throttle/brake section below) so the round-22 racecraft gate
            // just below can reuse it without recomputing anything. Corner
            // geometry is always from the true `target`, never the
            // racecraft-biased aim point — cornering speed must never
            // depend on where a rival happens to be standing.
            var beforeIndex = KartBotMath.PreviousWaypointIndex(_targetIndex, _path.Count);
            var afterTargetIndex = KartBotMath.AdvanceWaypointIndex(_targetIndex, _path.Count);
            var before = _path[beforeIndex];
            var afterTarget = _path[afterTargetIndex];
            var turnAngleRadians = KartBotMath.CalculateTurnAngleRadians(before, target, afterTarget);

            // Founder playtest feedback, 2026-08-20 (round 18): "nao tem
            // nada de pilotagem agressiva, bloquear, tentar se manter na
            // frente parece muito mecanico... quero competitividade". Bias
            // the steering AIM POINT sideways to defend against a rival
            // closing in from behind, or to pull alongside one just ahead
            // for an overtake — never the underlying waypoint, cornering
            // speed, throttle or brake, so it can't reintroduce the
            // wall-crash bugs the last two rounds fixed. Deliberately
            // skipped inside the cornering-braking window (same flag the
            // throttle/brake logic below uses) — racecraft only matters on
            // the straights, and biasing the aim point mid-corner-approach
            // is exactly what nearly ran bots into the outer wall in round
            // 16.
            // Round-21: steer at a point ahead on the path (pure-pursuit
            // style), not directly at the discrete waypoint — see
            // KartBotMath.CalculateLookaheadSteeringTarget. Everything else
            // below (arrival/lap counting above, cornering speed/braking
            // below) still uses the true discrete `target`, unchanged.
            // Round-22 founder feedback: "no médio/difícil os bots ficam
            // confusos, no fácil não" — the speed-based isCorneringPhase
            // gate above didn't reliably catch this track's continuous
            // gentle curve (see KartBotMath.ShouldApplyRacecraftBias XML
            // doc for the full diagnosis). Added a geometric gate on top:
            // skip racecraft on any real bend, not just a sharp
            // speed-limited one, regardless of how far away the next
            // waypoint's speed limit kicks in.
            var lookaheadMeters = Mathf.Clamp(
                currentSpeedMps * SteeringLookaheadSeconds, MinSteeringLookaheadMeters, MaxSteeringLookaheadMeters);
            var aimPoint = KartBotMath.CalculateLookaheadSteeringTarget(_path, _targetIndex, lookaheadMeters);
            var aggressiveness = KartBotMath.GetRacecraftAggressiveness01(_difficulty);
            var canApplyRacecraft = KartBotMath.ShouldApplyRacecraftBias(
                isCorneringPhase, turnAngleRadians, MaxBendRadiansForRacecraft);

            // Round 40: moved out of the racecraft-only gate below so the
            // ahead-rival distance/lateral/speed are also available for
            // the following-speed cap further down, regardless of
            // difficulty/cornering phase (a bot must never ram a rival
            // ahead even with racecraft disabled, e.g. Easy difficulty).
            FindNearestRivals(
                out var distanceBehindMeters, out var lateralOffsetBehindMeters,
                out var distanceAheadMeters, out var lateralOffsetAheadMeters,
                out var aheadSpeedKph);

            if (canApplyRacecraft && aggressiveness > 0f)
            {
                var defensiveOffset = KartBotMath.CalculateDefensiveLateralOffsetMeters(
                    distanceBehindMeters, lateralOffsetBehindMeters,
                    RacecraftEngagementRangeMeters, RacecraftMaxLateralOffsetMeters, aggressiveness);
                var overtakingOffset = KartBotMath.CalculateOvertakingLateralOffsetMeters(
                    distanceAheadMeters, lateralOffsetAheadMeters,
                    RacecraftEngagementRangeMeters, RacecraftMaxLateralOffsetMeters, aggressiveness, _preferredPassSideSign);
                var lateralOffset = Mathf.Clamp(
                    defensiveOffset + overtakingOffset, -RacecraftMaxLateralOffsetMeters, RacecraftMaxLateralOffsetMeters);
                // Bias the lookahead point sideways, not the raw waypoint —
                // keeps the round-21 pure-pursuit smoothing even while a
                // bot is defending/overtaking.
                aimPoint += transform.right * lateralOffset;
            }

            var idealSteering = KartBotMath.CalculateSteeringToTarget(
                transform.position, transform.forward, aimPoint, maxSteeringMagnitude);
            var steering = KartBotMath.ApplySteeringError(idealSteering, _currentSteeringError, maxSteeringMagnitude);

            // Founder playtest feedback, 2026-08-20 (round 14/15/16): "os
            // bots continuam sem competitividade... nao tem graça brincar",
            // then "seguiram reto", then "não tem inteligência alguma para
            // finalizar a prova". Target speed comes from the corner's real
            // geometry (before/at/after waypoints, computed further up —
            // see the round-22 comment there) and the kart's real,
            // currently-available grip — the same physics the player's own
            // cornering is limited by. Round 16: replaced a circumradius
            // approximation (KartBotMath used to compute this from a
            // 3-point circumradius) with the actual required turn angle and
            // the real distance available to make it — the circumradius
            // badly understated how tight sharp corners really are; see
            // KartBotMath's comment above CalculateTurnAngleRadians for the
            // full simulation-verified reasoning. Note: corner geometry is
            // always computed from the true waypoint (target), not the
            // racecraft-biased aimPoint — cornering speed must never
            // depend on where a rival happens to be standing.
            var availableDistanceMeters = Vector3.Distance(before, target);
            var safetyMargin = KartBotMath.GetCorneringSafetyMargin01(_difficulty);
            var targetSpeedMps = KartBotMath.CalculateMaxCorneringSpeedMetersPerSecond(
                turnAngleRadians, availableDistanceMeters, _dynamics.MaxLateralAcceleration, safetyMargin);

            // Round 40: following-speed cap from the rival directly ahead
            // (see KartBotMath.CalculateFollowingSafeSpeedMetersPerSecond)
            // -- infinity (no cap) when there is no rival close/aligned
            // enough to matter. Combined with the cornering cap via a
            // plain minimum, so whichever is more restrictive wins.
            var followSafeSpeedMps = KartBotMath.CalculateFollowingSafeSpeedMetersPerSecond(
                distanceAheadMeters, lateralOffsetAheadMeters, aheadSpeedKph / 3.6f,
                RivalFollowLaneHalfWidthMeters, RivalMinFollowGapMeters, RacecraftEngagementRangeMeters);
            var hasFollowCap = !float.IsPositiveInfinity(followSafeSpeedMps);

            float throttle;
            float brake;
            if (isCorneringPhase || hasFollowCap)
            {
                // Close enough to the corner that its speed limit matters,
                // and/or a rival directly ahead limits how fast it is safe
                // to go: accelerate toward the more restrictive of the two
                // if under, brake toward it if over. Outside a corner
                // approach, targetSpeedMps itself is not meaningful yet,
                // so the follow cap alone (when present) is used.
                var effectiveTargetSpeedMps = isCorneringPhase
                    ? Mathf.Min(targetSpeedMps, followSafeSpeedMps)
                    : followSafeSpeedMps;
                throttle = KartBotMath.CalculateThrottleForTargetSpeed(currentSpeedMps, effectiveTargetSpeedMps, minThrottle, maxThrottle);
                brake = KartBotMath.CalculateBrakeForTargetSpeed(currentSpeedMps, effectiveTargetSpeedMps, maxCornerBrake);
            }
            else
            {
                // Corner is still several seconds away at this speed and
                // no rival ahead limits our pace — no reason to lift yet.
                throttle = maxThrottle;
                brake = 0f;
            }

            // Round-23: personal pace ceiling (see MinPaceMultiplier's
            // comment) — applied uniformly to whichever branch above set
            // throttle, never to brake (a bot should still brake for a
            // corner exactly as hard as its difficulty requires; only the
            // "how fast when nothing else is limiting it" ceiling is
            // personalized).
            throttle *= _paceMultiplier;

            _dynamics.SetInput(steering, throttle, brake);
            _lastSteering = steering;
        }

        /// <summary>
        /// Nearest other active kart behind and ahead of this one, in this
        /// kart's own local space (via <see cref="KartDynamics.AllActiveKarts"/>,
        /// the same registry <see cref="KartDynamics"/> uses for
        /// slipstream) — distance is always positive/zero, lateral offset is
        /// signed (+right/-left). 0 distance means "none found" (matches
        /// how <see cref="KartBotMath.CalculateDefensiveLateralOffsetMeters"/>
        /// / <see cref="KartBotMath.CalculateOvertakingLateralOffsetMeters"/>
        /// treat a non-positive distance as "no rival").
        /// </summary>
        private void FindNearestRivals(
            out float distanceBehindMeters, out float lateralOffsetBehindMeters,
            out float distanceAheadMeters, out float lateralOffsetAheadMeters,
            out float aheadSpeedKph)
        {
            var closestBehind = float.MaxValue;
            var closestBehindLateral = 0f;
            var closestAhead = float.MaxValue;
            var closestAheadLateral = 0f;
            var closestAheadSpeedKph = 0f;

            var activeKarts = KartDynamics.AllActiveKarts;
            for (var i = 0; i < activeKarts.Count; i++)
            {
                var other = activeKarts[i];
                if (other == null || other == _dynamics)
                {
                    continue;
                }

                var local = transform.InverseTransformPoint(other.transform.position);
                if (local.z < 0f)
                {
                    var behind = -local.z;
                    if (behind < closestBehind)
                    {
                        closestBehind = behind;
                        closestBehindLateral = local.x;
                    }
                }
                else if (local.z > 0f && local.z < closestAhead)
                {
                    closestAhead = local.z;
                    closestAheadLateral = local.x;
                    closestAheadSpeedKph = other.SpeedKph;
                }
            }

            distanceBehindMeters = closestBehind < float.MaxValue ? closestBehind : 0f;
            lateralOffsetBehindMeters = closestBehindLateral;
            distanceAheadMeters = closestAhead < float.MaxValue ? closestAhead : 0f;
            lateralOffsetAheadMeters = closestAheadLateral;
            aheadSpeedKph = closestAheadSpeedKph;
        }

        private void UpdateStuckDetection()
        {
            _timeSinceRecoveryEnded += Time.deltaTime;

            _stuckCheckTimer += Time.deltaTime;
            if (_stuckCheckTimer < StuckDetectionSeconds)
            {
                return;
            }

            var moved = Vector3.Distance(transform.position, _stuckCheckPosition);
            if (KartBotMath.HasMadeInsufficientProgress(moved, StuckDistanceThresholdMeters))
            {
                // Getting stuck again shortly after the last recovery ended
                // means that recovery didn't actually clear whatever's in
                // the way — escalate instead of repeating the same short
                // reverse.
                _consecutiveStuckCount = _timeSinceRecoveryEnded <= RecoveryEscalationWindowSeconds
                    ? _consecutiveStuckCount + 1
                    : 1;
                _recoveryTimer = Mathf.Min(
                    RecoveryDurationSeconds + RecoveryDurationStepSeconds * (_consecutiveStuckCount - 1),
                    MaxRecoveryDurationSeconds);
            }

            // Round 25 founder feedback: "talvez um mecanismo para
            // recentralizar o kart se caso ele travar ou tiver voltando
            // pra trás, pq ele está sem referência total". Two signals,
            // neither caught by the reverse-recovery above: badly off the
            // actual track (isLost — a collision-knockback case, not a
            // normal wide corner exit), and facing the wrong way along it
            // (isFacingBackward — a spun-around case). Either one means
            // _targetIndex itself is the problem, not a physical
            // obstruction, so the fix is a fresh reference, not another
            // reverse. Also resnaps once the escalating reverse maneuver
            // has failed MaxConsecutiveStuckBeforeResnap times in a row —
            // at that point it probably isn't physically wedged either.
            var isLost = KartBotMath.DistanceToNearestPathPointMeters(transform.position, _path) > LostDistanceThresholdMeters;
            var isFacingBackward = KartBotMath.IsFacingAgainstPathDirection(transform.position, transform.forward, _path);
            _consecutiveBackwardFacingChecks = isFacingBackward ? _consecutiveBackwardFacingChecks + 1 : 0;
            var isPersistentlyBackward = _consecutiveBackwardFacingChecks >= MinConsecutiveBackwardFacingChecksBeforeResnap;

            if (isLost || isPersistentlyBackward || _consecutiveStuckCount >= MaxConsecutiveStuckBeforeResnap)
            {
                ResnapToNearestPathPoint();
                _consecutiveStuckCount = 0;
                _consecutiveBackwardFacingChecks = 0;
            }

            _stuckCheckTimer = 0f;
            _stuckCheckPosition = transform.position;
        }
    }
}
