using System.Collections.Generic;

namespace RKW.Physics
{
    /// <summary>
    /// One completed race's TOTAL time, for the main menu's "melhor tempo
    /// nas 3 categorias 1 3 5" founder request (Rodada 46, 2026-09-01).
    /// This is a SEPARATE history from <see cref="LapRecord"/>/
    /// <see cref="LapRecordStore"/> -- that one tracks the single fastest
    /// LAP ever driven (any race length mixed together, which is fine
    /// because one lap is always comparable to another lap); this one
    /// tracks the fastest FULL RACE for each specific lap count (1, 3, or
    /// 5 voltas), since a 1-lap race total and a 5-lap race total are not
    /// comparable to each other. <see cref="TrackSignature"/>/
    /// <see cref="KartCategoryId"/> are still captured (same convention as
    /// LapRecord, same PrototypeCompetitiveScope already computed at race
    /// setup) so the data stays available for a future track/category-
    /// scoped view, even though the first version of the menu display
    /// (see MainMenu.DrawRaceTimesRow) deliberately does NOT filter by
    /// them -- see that method's own comment for why.
    /// </summary>
    public readonly struct RaceRecord
    {
        public readonly int Laps;
        public readonly float TotalTimeSeconds;
        public readonly long UnixTimestampSeconds;
        public readonly string PlayerName;
        public readonly string TrackSignature;
        public readonly string KartCategoryId;

        public RaceRecord(int laps, float totalTimeSeconds, long unixTimestampSeconds, string playerName,
            string trackSignature, string kartCategoryId)
        {
            Laps = laps;
            TotalTimeSeconds = totalTimeSeconds;
            UnixTimestampSeconds = unixTimestampSeconds;
            PlayerName = playerName ?? string.Empty;
            TrackSignature = trackSignature ?? string.Empty;
            KartCategoryId = kartCategoryId ?? string.Empty;
        }
    }

    /// <summary>Pure selection logic for the per-lap-count race leaderboard -- no Unity lifecycle, no PlayerPrefs.</summary>
    public static class RaceRecordMath
    {
        /// <summary>
        /// Fastest total race time among records with exactly
        /// <paramref name="laps"/> laps, whose age (relative to
        /// <paramref name="nowUnixSeconds"/>) is within
        /// <paramref name="maxAgeSecondsOrNegativeForAllTime"/>. Pass a
        /// negative value to consider every record regardless of age --
        /// same signature convention as
        /// <see cref="LapRecordMath.FindBestLapTimeSeconds"/>. Returns null
        /// when no record qualifies (including an empty list).
        /// </summary>
        public static float? FindBestRaceTimeSeconds(
            IReadOnlyList<RaceRecord> records, int laps, long nowUnixSeconds, long maxAgeSecondsOrNegativeForAllTime)
        {
            if (records == null)
            {
                return null;
            }

            float? best = null;
            for (var i = 0; i < records.Count; i++)
            {
                var record = records[i];
                if (record.Laps != laps)
                {
                    continue;
                }

                if (maxAgeSecondsOrNegativeForAllTime >= 0)
                {
                    var ageSeconds = nowUnixSeconds - record.UnixTimestampSeconds;
                    if (ageSeconds < 0 || ageSeconds > maxAgeSecondsOrNegativeForAllTime)
                    {
                        continue;
                    }
                }

                if (!best.HasValue || record.TotalTimeSeconds < best.Value)
                {
                    best = record.TotalTimeSeconds;
                }
            }

            return best;
        }
    }
}
