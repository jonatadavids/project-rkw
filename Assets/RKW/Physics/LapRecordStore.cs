using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace RKW.Physics
{
    /// <summary>
    /// PlayerPrefs-backed history of completed laps, for the founder's
    /// requested post-race leaderboard. This is a local, single-device
    /// prototype — no backend/account involved, so PlayerPrefs (survives
    /// scene reloads and app relaunches) is enough. Selection logic lives
    /// in the pure, testable <see cref="LapRecordMath"/>; this class only
    /// knows how to read/write the encoded string.
    /// </summary>
    public static class LapRecordStore
    {
        // Round 33 (2026-08-24) founder request: "reiniciar os tempos" after
        // rebalancing kart top speeds (School 55 / RentalSport 70 /
        // SportPlus 85 km/h, was 55/60/80) — old times were set under
        // different, now-stale speed tunings and would be misleading to
        // keep. Same technique already used once before (see the Decode
        // comment below about round 23): changing this key orphans every
        // previously saved entry (still on the device, just never read
        // again) and starts a clean history from here on, without needing
        // any on-device file access.
        private const string HistoryKey = "RKW_LapRecordHistory_v3";
        private const int MaxEntries = 200;
        private const char EntrySeparator = ';';
        private const char FieldSeparator = ',';

        /// <summary>Appends a lap to the persisted history, trimming the oldest entries beyond <see cref="MaxEntries"/>. The supplied scope prevents comparisons across tracks or kart categories.</summary>
        public static void RecordLap(float lapTimeSeconds, long unixTimestampSeconds, string playerName,
            PrototypeCompetitiveScope scope)
        {
            var history = new List<LapRecord>(LoadHistory())
            {
                new LapRecord(lapTimeSeconds, unixTimestampSeconds, playerName,
                    scope.TrackSignature, scope.KartCategoryId)
            };

            if (history.Count > MaxEntries)
            {
                history.RemoveRange(0, history.Count - MaxEntries);
            }

            PlayerPrefs.SetString(HistoryKey, Encode(history));
            PlayerPrefs.Save();
        }

        /// <summary>All persisted lap records, oldest first. Empty (never null) if nothing has been recorded yet.</summary>
        public static LapRecord[] LoadHistory()
        {
            var raw = PlayerPrefs.GetString(HistoryKey, string.Empty);
            return Decode(raw);
        }

        internal static string Encode(List<LapRecord> records)
        {
            var entries = new string[records.Count];
            for (var i = 0; i < records.Count; i++)
            {
                var record = records[i];
                // Invariant culture so the decimal point is always '.' —
                // never ',', which would collide with the field separator
                // on a device set to a comma-decimal locale (e.g. pt-BR).
                // TrackSignature is programmatically generated (e.g. "239m")
                // so it never contains a separator; PlayerName already has
                // ',' and ';' stripped by PlayerNameStore before it ever
                // gets here. PlayerName stays LAST so it can safely be the
                // "everything remaining" field on decode.
                entries[i] = record.LapTimeSeconds.ToString(CultureInfo.InvariantCulture)
                    + FieldSeparator + record.UnixTimestampSeconds.ToString(CultureInfo.InvariantCulture)
                    + FieldSeparator + record.TrackSignature
                    + FieldSeparator + record.KartCategoryId
                    + FieldSeparator + record.PlayerName;
            }

            return string.Join(EntrySeparator.ToString(), entries);
        }

        internal static LapRecord[] Decode(string raw)
        {
            if (string.IsNullOrEmpty(raw))
            {
                return System.Array.Empty<LapRecord>();
            }

            var entryStrings = raw.Split(EntrySeparator);
            var records = new List<LapRecord>(entryStrings.Length);
            foreach (var entry in entryStrings)
            {
                if (string.IsNullOrEmpty(entry))
                {
                    continue;
                }

                // Split into at most 5 fields — the name itself is never
                // expected to contain a comma (PlayerNameStore strips them),
                // but be defensive rather than silently drop an entry if it
                // somehow does.
                var fields = entry.Split(new[] { FieldSeparator }, 5);
                // Round 7 wrote a 2-field format (no name); round 23
                // (2026-08-24) added a 4th field (TrackSignature, see
                // LapRecord) ahead of the name — skip anything that isn't
                // today's 5-field format rather than crash. The fifth field
                // makes kart category explicit; older records cannot be
                // migrated safely because their originating category was
                // never persisted, so they are intentionally ignored.
                // every record written before this change is silently
                // dropped on first load after updating — exactly the
                // "reiniciar o melhor tempo" the founder asked for, as a
                // one-time side effect of the fix, not something to redo
                // every track change afterwards.
                if (fields.Length != 5 || string.IsNullOrEmpty(fields[2]) || string.IsNullOrEmpty(fields[3]))
                {
                    continue;
                }

                if (!float.TryParse(fields[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var lapTime))
                {
                    continue;
                }

                if (!long.TryParse(fields[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var timestamp))
                {
                    continue;
                }

                records.Add(new LapRecord(lapTime, timestamp, fields[4], fields[2], fields[3]));
            }

            return records.ToArray();
        }
    }
}
