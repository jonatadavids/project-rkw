using System.Collections.Generic;
using UnityEngine;

namespace RKW.Physics
{
    /// <summary>
    /// One recorded ghost sample: where the kart was and which way it was
    /// facing (yaw only — this prototype's karts never pitch/roll enough to
    /// matter, same convention as <see cref="RKW.Track.GridSlot.YawDegrees"/>
    /// and <see cref="KartPhysicsPrototypeBootstrap"/>'s kart spawning).
    /// </summary>
    public readonly struct GhostSample
    {
        public readonly Vector3 Position;
        public readonly float YawDegrees;

        public GhostSample(Vector3 position, float yawDegrees)
        {
            Position = position;
            YawDegrees = yawDegrees;
        }
    }

    /// <summary>
    /// Founder request, 2026-08-24: "vamos tentar seguir talvez o fantasma
    /// fique mais legal... podemos ir pra m4 que mexe com o fantasma acho
    /// que vai ficar mais legal" — pure interpolation/lookup logic for ghost
    /// kart playback, no Unity lifecycle, no PlayerPrefs (see
    /// <see cref="GhostController"/>/<see cref="GhostRecordStore"/> for
    /// those). Quick, fun-first version of M4-T06/T13's formal ghost
    /// system — see tasks.md for that fuller spec (sectors, 50KB budget,
    /// property tests), deliberately deferred in favor of something
    /// testable today.
    ///
    /// Samples are recorded at a FIXED interval (see
    /// GhostController.SampleIntervalSeconds), so sample index * interval
    /// IS that sample's timestamp — no need to store time explicitly.
    ///
    /// Founder follow-up, same day: "nao ficar relargando a cada volta pq
    /// fica meio sem sentido... nunca sei se completei as 3 voltas melhor
    /// que o fantasma" — an earlier version of this feature recorded/played
    /// back a single best LAP, restarting the ghost's clock every lap. That
    /// briefly tried to fix a "waits then bolts" symptom by splitting the
    /// saved best by standing-start-vs-rolling-start lap kind, but it never
    /// addressed the founder's real ask: comparing a FULL RACE, not one lap
    /// repeated. <see cref="GhostController"/> now records/plays back
    /// continuously across the WHOLE race (elapsed time since the race
    /// began, never reset mid-race), which subsumes that fix for free — the
    /// standing start is just however the recording happens to begin, and
    /// every lap transition replays exactly as it was recorded, because
    /// it's all one continuous timeline. <see cref="GhostRecordStore"/>
    /// keeps one best-race recording per track PER CONFIGURED LAP COUNT
    /// (1/3/5), per the founder's original suggestion — which turns out to
    /// be exactly correct once the thing being compared is the whole race
    /// rather than a single lap.
    /// </summary>
    public static class GhostMath
    {
        /// <summary>
        /// Interpolated ghost pose at <paramref name="elapsedSeconds"/> into
        /// the recorded race. Clamps to the first/last sample when
        /// <paramref name="elapsedSeconds"/> falls outside the recorded
        /// range (ghost holds its start pose before recording begins, and
        /// its finish pose once the recorded race is over) rather than
        /// extrapolating. Returns false only when there are zero samples to
        /// interpolate from at all.
        /// </summary>
        public static bool TrySamplePose(
            IReadOnlyList<GhostSample> samples, float sampleIntervalSeconds, float elapsedSeconds,
            out Vector3 position, out float yawDegrees)
        {
            if (samples == null || samples.Count == 0 || sampleIntervalSeconds <= 0f)
            {
                position = Vector3.zero;
                yawDegrees = 0f;
                return false;
            }

            if (samples.Count == 1 || elapsedSeconds <= 0f)
            {
                position = samples[0].Position;
                yawDegrees = samples[0].YawDegrees;
                return true;
            }

            var rawIndex = elapsedSeconds / sampleIntervalSeconds;
            var lastIndex = samples.Count - 1;
            if (rawIndex >= lastIndex)
            {
                position = samples[lastIndex].Position;
                yawDegrees = samples[lastIndex].YawDegrees;
                return true;
            }

            var indexA = Mathf.FloorToInt(rawIndex);
            var indexB = indexA + 1;
            var t = rawIndex - indexA;
            var a = samples[indexA];
            var b = samples[indexB];
            position = Vector3.Lerp(a.Position, b.Position, t);
            yawDegrees = Mathf.LerpAngle(a.YawDegrees, b.YawDegrees, t);
            return true;
        }
    }
}
