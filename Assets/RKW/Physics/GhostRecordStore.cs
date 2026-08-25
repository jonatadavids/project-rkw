using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace RKW.Physics
{
    /// <summary>
    /// PlayerPrefs-backed storage for the best-RACE ghost recordings, one
    /// per track, kart category AND configured lap count (1/3/5). Founder feedback,
    /// 2026-08-24 (two rounds): first asked for the ghost to be split by
    /// how many laps the race was configured for ("quando coloca 1 volta é
    /// um fantasma, quando coloca 3 volta é outro fantasma e 5 outra
    /// fantasma"), then clarified why — "nao ficar relargando a cada volta
    /// pq fica meio sem sentido... nunca sei se completei as 3 voltas
    /// melhor que o fantasma". <see cref="GhostController"/> now records
    /// one continuous take of the WHOLE race (not a single lap replayed on
    /// a loop), so lap count is exactly the right key: a 3-lap race and a
    /// 5-lap race are different races to compare against, and a 1-lap race
    /// naturally degrades to "best single lap" (same as before this
    /// change) since a full race IS one lap in that case. Track-signature
    /// scoping is the same reason <see cref="LapRecord.TrackSignature"/>
    /// was added the same day — otherwise a ghost recorded on an old track
    /// layout would replay in the wrong place once the track changes.
    /// Local, single-device prototype only, same as
    /// <see cref="LapRecordStore"/> — no compression/50KB budget like the
    /// formal M4-T06 spec (tasks.md); this is the quick, fun-first version.
    /// </summary>
    public static class GhostRecordStore
    {
        // v3 adds the kart category to the persisted comparison scope. v2
        // recordings are intentionally ignored because their source category
        // was never stored and cannot be inferred safely.
        private const string KeyPrefix = "RKW_GhostBest_v3_";
        private const char SampleSeparator = ';';
        private const char FieldSeparator = ',';

        /// <summary>Overwrites any previously saved ghost for this track+category+lap-count — callers only save on a new personal best full-race time, so "overwrite" always means "got faster".</summary>
        public static void SaveBestGhost(PrototypeCompetitiveScope scope, int lapCount,
            float raceTimeSeconds, IReadOnlyList<GhostSample> samples)
        {
            if (samples == null || samples.Count == 0)
            {
                return;
            }

            PlayerPrefs.SetString(BuildStorageKey(scope, lapCount), Encode(raceTimeSeconds, samples));
            PlayerPrefs.Save();
        }

        /// <summary>True and populates the out params if a readable ghost is saved for this track+category+lap-count; false if there is none or it is corrupt/an old format.</summary>
        public static bool TryLoadBestGhost(PrototypeCompetitiveScope scope, int lapCount,
            out float raceTimeSeconds, out List<GhostSample> samples)
        {
            raceTimeSeconds = 0f;
            samples = null;

            var raw = PlayerPrefs.GetString(BuildStorageKey(scope, lapCount), string.Empty);
            return Decode(raw, out raceTimeSeconds, out samples);
        }

        internal static string BuildStorageKey(PrototypeCompetitiveScope scope, int lapCount)
        {
            return KeyPrefix + scope.ToStorageKeySegment() + "_" + Mathf.Max(1, lapCount) + "laps";
        }

        private static string Encode(float raceTimeSeconds, IReadOnlyList<GhostSample> samples)
        {
            // Invariant culture, same reasoning as LapRecordStore.Encode —
            // the decimal point must always be '.', never a locale comma.
            var builder = new StringBuilder();
            builder.Append(raceTimeSeconds.ToString(CultureInfo.InvariantCulture));
            for (var i = 0; i < samples.Count; i++)
            {
                var sample = samples[i];
                builder.Append(SampleSeparator);
                builder.Append(sample.Position.x.ToString("F2", CultureInfo.InvariantCulture)).Append(FieldSeparator);
                builder.Append(sample.Position.y.ToString("F2", CultureInfo.InvariantCulture)).Append(FieldSeparator);
                builder.Append(sample.Position.z.ToString("F2", CultureInfo.InvariantCulture)).Append(FieldSeparator);
                builder.Append(sample.YawDegrees.ToString("F1", CultureInfo.InvariantCulture));
            }

            return builder.ToString();
        }

        private static bool Decode(string raw, out float raceTimeSeconds, out List<GhostSample> samples)
        {
            raceTimeSeconds = 0f;
            samples = null;

            if (string.IsNullOrEmpty(raw))
            {
                return false;
            }

            var parts = raw.Split(SampleSeparator);
            if (parts.Length < 2)
            {
                return false;
            }

            if (!float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out raceTimeSeconds))
            {
                return false;
            }

            var result = new List<GhostSample>(parts.Length - 1);
            for (var i = 1; i < parts.Length; i++)
            {
                var fields = parts[i].Split(FieldSeparator);
                if (fields.Length != 4)
                {
                    continue;
                }

                if (!float.TryParse(fields[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) ||
                    !float.TryParse(fields[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y) ||
                    !float.TryParse(fields[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var z) ||
                    !float.TryParse(fields[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var yaw))
                {
                    continue;
                }

                result.Add(new GhostSample(new Vector3(x, y, z), yaw));
            }

            if (result.Count == 0)
            {
                return false;
            }

            samples = result;
            return true;
        }
    }
}
