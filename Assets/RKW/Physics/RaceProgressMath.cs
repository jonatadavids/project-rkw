using System.Collections.Generic;
using UnityEngine;

namespace RKW.Physics
{
    /// <summary>
    /// Founder playtest feedback, 2026-08-20 (round 8): "nao mostrou a
    /// classificacao... nem durante a corrida nem o nome dos bots". A
    /// cheap, display-only "how far around the lap is this kart" proxy
    /// shared by the player and every bot, so <see cref="RaceStandingsHud"/>
    /// can rank them against a single yardstick even though the player
    /// isn't driven by <see cref="KartBotController"/>'s waypoint follower.
    /// Deliberately NOT used for gameplay/physics — only for the standings
    /// list, so its coarseness (nearest of ~8 waypoints) doesn't matter.
    /// </summary>
    public static class RaceProgressMath
    {
        /// <summary>Index of the waypoint in <paramref name="path"/> closest to <paramref name="position"/> (XZ plane). 0 for a null/empty path.</summary>
        public static int FindNearestWaypointIndex(Vector3 position, IReadOnlyList<Vector3> path)
        {
            if (path == null || path.Count == 0)
            {
                return 0;
            }

            var bestIndex = 0;
            var bestSqrDistance = float.MaxValue;
            for (var i = 0; i < path.Count; i++)
            {
                var diff = path[i] - position;
                diff.y = 0f;
                var sqrDistance = diff.sqrMagnitude;
                if (sqrDistance < bestSqrDistance)
                {
                    bestSqrDistance = sqrDistance;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        /// <summary>
        /// True if (lapsA, waypointIndexA) represents further progress than
        /// (lapsB, waypointIndexB) — more laps wins outright, waypoint index
        /// only breaks a tie within the same lap.
        /// </summary>
        public static bool IsAheadOf(int lapsA, int waypointIndexA, int lapsB, int waypointIndexB)
        {
            if (lapsA != lapsB)
            {
                return lapsA > lapsB;
            }

            return waypointIndexA > waypointIndexB;
        }

        /// <summary>
        /// Round 25 (2026-08-24) founder feedback: "outra coisa seria legal
        /// ter o tempo do bot a comparacao dele em todas as voltas" —
        /// RaceManager.SelectComparisonBot uses this to pick which bot gets
        /// shown in the finish screen's lap-by-lap table when several bots
        /// raced. More laps completed wins outright (that bot got further,
        /// full stop); a tie is broken by whichever bot's laps-so-far add
        /// up to less total time (the quicker of two bots that got equally
        /// far). Same "more progress wins, time only breaks a tie" shape as
        /// <see cref="IsAheadOf"/> above, just laps+time instead of
        /// laps+waypoint.
        /// </summary>
        public static bool IsBetterComparisonCandidate(
            int candidateLaps, float candidateTotalTimeSeconds, int bestLaps, float bestTotalTimeSeconds)
        {
            if (candidateLaps != bestLaps)
            {
                return candidateLaps > bestLaps;
            }

            return candidateTotalTimeSeconds < bestTotalTimeSeconds;
        }
    }
}
