using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace RKW.Physics
{
    /// <summary>
    /// PlayerPrefs-backed history of completed FULL RACES (total time, not
    /// a single lap), for the main menu's "melhor tempo nas 3 categorias
    /// 1 3 5" founder request (Rodada 46, 2026-09-01). Same technique as
    /// <see cref="LapRecordStore"/> (a local, single-device prototype, no
    /// backend involved) -- see that class's own doc comment. Selection
    /// logic lives in the pure, testable <see cref="RaceRecordMath"/>;
    /// this class only knows how to read/write the encoded string.
    /// </summary>
    public static class RaceRecordStore
    {
        private const string HistoryKey = "RKW_RaceRecordHistory_v1";
        private const int MaxEntries = 200;
        private const char EntrySeparator = ';';
        private const char FieldSeparator = ',';

        /// <summary>Appends a finished race to the persisted history, trimming the oldest entries beyond <see cref="MaxEntries"/>.</summary>
        public static void RecordRace(int laps, float totalTimeSeconds, long unixTimestampSeconds, string playerName,
            PrototypeCompetitiveScope scope)
        {
            var history = new List<RaceRecord>(LoadHistory())
            {
                new RaceRecord(laps, totalTimeSeconds, unixTimestampSeconds, playerName,
                    scope.TrackSignature, scope.KartCategoryId)
            };

            if (history.Count > MaxEntries)
            {
                history.RemoveRange(0, history.Count - MaxEntries);
            }

            PlayerPrefs.SetString(HistoryKey, Encode(history));
            PlayerPrefs.Save();
        }

        /// <summary>All persisted race records, oldest first. Empty (never null) if nothing has been recorded yet.</summary>
        public static RaceRecord[] LoadHistory()
        {
            var raw = PlayerPrefs.GetString(HistoryKey, string.Empty);
            return Decode(raw);
        }

        internal static string Encode(List<RaceRecord> records)
        {
            var entries = new string[records.Count];
            for (var i = 0; i < records.Count; i++)
            {
                var record = records[i];
                // Same invariant-culture / field-order reasoning as
                // LapRecordStore.Encode -- PlayerName stays LAST so it can
                // safely be the "everything remaining" field on decode.
                entries[i] = record.Laps.ToString(CultureInfo.InvariantCulture)
                    + FieldSeparator + record.TotalTimeSeconds.ToString(CultureInfo.InvariantCulture)
                    + FieldSeparator + record.UnixTimestampSeconds.ToString(CultureInfo.InvariantCulture)
                    + FieldSeparator + record.TrackSignature
                    + FieldSeparator + record.KartCategoryId
                    + FieldSeparator + record.PlayerName;
            }

            return string.Join(EntrySeparator.ToString(), entries);
        }

        internal static RaceRecord[] Decode(string raw)
        {
            if (string.IsNullOrEmpty(raw))
            {
                return System.Array.Empty<RaceRecord>();
            }

            var entryStrings = raw.Split(EntrySeparator);
            var records = new List<RaceRecord>(entryStrings.Length);
            foreach (var entry in entryStrings)
            {
                if (string.IsNullOrEmpty(entry))
                {
                    continue;
                }

                // 6 fields: laps, totalTime, timestamp, trackSignature,
                // kartCategoryId, playerName (last, greedy) -- one more
                // than LapRecordStore's 5, for the extra Laps field.
                var fields = entry.Split(new[] { FieldSeparator }, 6);
                if (fields.Length != 6 || string.IsNullOrEmpty(fields[3]) || string.IsNullOrEmpty(fields[4]))
                {
                    continue;
                }

                if (!int.TryParse(fields[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var laps))
                {
                    continue;
                }

                if (!float.TryParse(fields[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var totalTime))
                {
                    continue;
                }

                if (!long.TryParse(fields[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out var timestamp))
                {
                    continue;
                }

                records.Add(new RaceRecord(laps, totalTime, timestamp, fields[5], fields[3], fields[4]));
            }

            return records.ToArray();
        }
    }
}
