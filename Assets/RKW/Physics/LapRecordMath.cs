using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace RKW.Physics
{
    /// <summary>
    /// One completed lap, kept for the founder's requested post-race
    /// leaderboard. Round 8 feedback (2026-08-20) refined the original
    /// "melhor de todos/dia/semana" idea (which never actually rendered
    /// for him — see RaceManager's round 8 notes) into something more
    /// concrete: a named top-5, discarding the rest. <see cref="PlayerName"/>
    /// is what gets typed in via <see cref="PlayerNameStore"/>. Plain data —
    /// persistence lives in <see cref="LapRecordStore"/>, selection logic
    /// lives here so it stays EditMode testable without touching PlayerPrefs.
    /// </summary>
    public readonly struct LapRecord
    {
        public readonly float LapTimeSeconds;
        public readonly long UnixTimestampSeconds;
        public readonly string PlayerName;
        // Founder feedback, 2026-08-24: "depois que alteramos a pista a
        // melhor volta ficou travada na ultima deveria ser reiniciada toda
        // vez que a gente colocar uma pista nova" — see
        // CalculateClosedPathLengthMeters/FormatTrackSignature below for how
        // this is derived. Optional (defaults to empty) so every existing
        // call site in tests/production code compiles unchanged; empty is
        // treated as "unknown track" wherever this is filtered.
        public readonly string TrackSignature;

        public LapRecord(float lapTimeSeconds, long unixTimestampSeconds, string playerName, string trackSignature = "")
        {
            LapTimeSeconds = lapTimeSeconds;
            UnixTimestampSeconds = unixTimestampSeconds;
            PlayerName = playerName ?? string.Empty;
            TrackSignature = trackSignature ?? string.Empty;
        }
    }

    /// <summary>Pure selection logic for the lap-time leaderboard — no Unity lifecycle, no PlayerPrefs.</summary>
    public static class LapRecordMath
    {
        /// <summary>
        /// Founder feedback, 2026-08-24: "depois que alteramos a pista a
        /// melhor volta ficou travada na ultima deveria ser reiniciada toda
        /// vez que a gente colocar uma pista nova" — LapRecordStore was one
        /// single global history with no notion of which track geometry a
        /// lap was set on, so a fast lap from a short/old layout permanently
        /// "won" over anything set on a longer/different one after the
        /// track changed. Total closed-loop lap length (rounded to the
        /// nearest meter via FormatTrackSignature) tags every record
        /// instead — self-updating: any track edit that changes the lap
        /// distance (nearly every edit made so far, per
        /// docs/30-founder-playtest-log.md) buckets old and new records
        /// apart automatically, with no manual version number to remember
        /// to bump. Known limitation: an edit that leaves total lap length
        /// unchanged (e.g. only track width) would not be caught —
        /// acceptable for this prototype.
        /// </summary>
        public static float CalculateClosedPathLengthMeters(IReadOnlyList<Vector3> path)
        {
            if (path == null || path.Count < 2)
            {
                return 0f;
            }

            var total = 0f;
            for (var i = 0; i < path.Count; i++)
            {
                var next = path[(i + 1) % path.Count];
                total += Vector3.Distance(path[i], next);
            }

            return total;
        }

        /// <summary>Stable string key for <see cref="LapRecord.TrackSignature"/> — rounded to the nearest meter so tiny floating-point differences between runs never split what is really the same track.</summary>
        public static string FormatTrackSignature(float closedPathLengthMeters)
        {
            return Mathf.RoundToInt(closedPathLengthMeters).ToString(CultureInfo.InvariantCulture) + "m";
        }

        /// <summary>Only the records tagged with <paramref name="trackSignature"/> — see <see cref="LapRecord.TrackSignature"/> for why this matters. An empty/null signature matches nothing (never silently show unscoped history as if it were comparable).</summary>
        public static List<LapRecord> FilterByTrackSignature(IReadOnlyList<LapRecord> records, string trackSignature)
        {
            var result = new List<LapRecord>();
            if (records == null || string.IsNullOrEmpty(trackSignature))
            {
                return result;
            }

            for (var i = 0; i < records.Count; i++)
            {
                if (records[i].TrackSignature == trackSignature)
                {
                    result.Add(records[i]);
                }
            }

            return result;
        }

        /// <summary>
        /// Fastest lap among <paramref name="records"/> whose age (relative
        /// to <paramref name="nowUnixSeconds"/>) is within
        /// <paramref name="maxAgeSecondsOrNegativeForAllTime"/>. Pass a
        /// negative value to consider every record regardless of age.
        /// Returns null when no record qualifies (including an empty list).
        /// </summary>
        public static float? FindBestLapTimeSeconds(
            IReadOnlyList<LapRecord> records, long nowUnixSeconds, long maxAgeSecondsOrNegativeForAllTime)
        {
            if (records == null)
            {
                return null;
            }

            float? best = null;
            for (var i = 0; i < records.Count; i++)
            {
                var record = records[i];
                if (maxAgeSecondsOrNegativeForAllTime >= 0)
                {
                    var ageSeconds = nowUnixSeconds - record.UnixTimestampSeconds;
                    if (ageSeconds < 0 || ageSeconds > maxAgeSecondsOrNegativeForAllTime)
                    {
                        continue;
                    }
                }

                if (!best.HasValue || record.LapTimeSeconds < best.Value)
                {
                    best = record.LapTimeSeconds;
                }
            }

            return best;
        }

        /// <summary>
        /// The <paramref name="count"/> fastest laps in <paramref name="records"/>,
        /// fastest first, ties broken by whichever was set first (stable
        /// sort). Founder playtest feedback, 2026-08-20 (round 8): "talvez
        /// pegar os 5 [+] e o restante só descartar" — everything past
        /// <paramref name="count"/> is simply dropped, not returned.
        /// </summary>
        public static List<LapRecord> FindTopRecords(IReadOnlyList<LapRecord> records, int count)
        {
            var result = new List<LapRecord>();
            if (records == null || count <= 0)
            {
                return result;
            }

            var sorted = new List<LapRecord>(records);
            // Stable insertion sort by lap time — record counts here are
            // small (bounded by LapRecordStore.MaxEntries), so O(n^2) is
            // fine and keeps equal-time entries in their original order.
            for (var i = 1; i < sorted.Count; i++)
            {
                var current = sorted[i];
                var j = i - 1;
                while (j >= 0 && sorted[j].LapTimeSeconds > current.LapTimeSeconds)
                {
                    sorted[j + 1] = sorted[j];
                    j--;
                }
                sorted[j + 1] = current;
            }

            for (var i = 0; i < sorted.Count && i < count; i++)
            {
                result.Add(sorted[i]);
            }

            return result;
        }
    }
}
