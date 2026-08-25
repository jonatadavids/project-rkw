using System.Collections.Generic;
using UnityEngine;

namespace RKW.Physics
{
    /// <summary>
    /// Founder request, 2026-08-24: "vamos tentar seguir talvez o fantasma
    /// fique mais legal... podemos ir pra m4 que mexe com o fantasma acho
    /// que vai ficar mais legal" (said right after a frustrating round of
    /// Hard-difficulty bot debugging — the ask was explicitly for a quick,
    /// fun win, not the full formal M4 milestone). Records the player's OWN
    /// best FULL RACE (position + yaw at a fixed sample rate, from the
    /// moment the race is set up until the configured lap count is
    /// reached) and replays it as a ghost kart with zero physics/collision
    /// — a silent rival to chase, never something that can be hit or hit
    /// the player. Persisted locally so it survives app relaunches.
    ///
    /// Founder follow-up, same day, after confirming the ghost "funcionou
    /// super bem": "ele fica esperando o meu kart passar pra dar sequência
    /// quando a gente começa ele arranca e nas demais volta ele já começa
    /// a corrida correndo". First attempt fixed this by splitting the
    /// saved-best ghost into standing-start/rolling-start lap buckets, but
    /// the founder's next message clarified the real ask was different:
    /// "nao ficar relargando a cada volta pq fica meio sem sentido... nunca
    /// sei se completei as 3 voltas melhor que o fantasma" — he wanted to
    /// compare a whole race, not have the ghost restart every lap. This
    /// version records ONE continuous take of the entire race (elapsed
    /// time since the race began, never reset mid-race) and only saves it
    /// as a new best once the full configured lap count is completed. That
    /// subsumes the standing-start/rolling-start fix for free (the
    /// recording just contains whatever really happened at each lap
    /// transition) and directly answers "did I beat the ghost overall" —
    /// the ghost's own finish time IS the number to beat. One best
    /// recording is kept per track, kart category and configured lap count (1/3/5 — see
    /// <see cref="GhostRecordStore"/>), matching the founder's original
    /// suggestion, which turns out to be exactly correct once the thing
    /// being compared is the whole race.
    ///
    /// This is deliberately the "quick, fun-first" ghost, not the formal
    /// M4-T06/T13 system (separate RKW.Telemetry assembly, 50KB compressed
    /// budget, property tests — see tasks.md).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GhostController : MonoBehaviour
    {
        // 10 Hz: smooth enough not to look choppy at kart speeds (a kart at
        // 20 m/s only covers ~2m between samples, interpolated anyway by
        // GhostMath.TrySamplePose) while keeping the recorded data small
        // and cheap to persist as plain text.
        private const float SampleIntervalSeconds = 0.1f;

        private TimingManagerLite _timing;
        private Transform _playerTransform;
        private Transform _ghostVisual;
        private PrototypeCompetitiveScope _scope;
        private int _targetLaps = 1;

        private readonly List<GhostSample> _recordingBuffer = new List<GhostSample>();
        private float _raceStartTime;
        private float _nextSampleTime;
        private bool _raceValid;
        private bool _raceComplete;

        private List<GhostSample> _bestRaceSamples;
        private float _bestRaceTimeSeconds = float.MaxValue;

        public void Configure(TimingManagerLite timing, Transform playerTransform, Transform ghostVisual,
            PrototypeCompetitiveScope scope, int targetLaps)
        {
            if (_timing != null)
            {
                _timing.OnLapCompleted -= OnLapCompleted;
                _timing.OnLapInvalidated -= OnLapInvalidated;
            }

            _timing = timing;
            _playerTransform = playerTransform;
            _ghostVisual = ghostVisual;
            _scope = scope;
            _targetLaps = Mathf.Max(1, targetLaps);

            _recordingBuffer.Clear();
            _raceStartTime = Time.time;
            _nextSampleTime = 0f;
            _raceValid = true;
            _raceComplete = false;

            if (GhostRecordStore.TryLoadBestGhost(_scope, _targetLaps, out var loadedRaceTime, out var loadedSamples))
            {
                _bestRaceSamples = loadedSamples;
                _bestRaceTimeSeconds = loadedRaceTime;
            }
            else
            {
                _bestRaceSamples = null;
                _bestRaceTimeSeconds = float.MaxValue;
            }

            // Hidden until Update() decides there's actually a recorded
            // race to show — a ghost frozen at the world origin for even
            // one frame would look like a broken extra kart flashing into
            // existence.
            if (_ghostVisual != null)
            {
                _ghostVisual.gameObject.SetActive(false);
            }

            if (_timing != null)
            {
                _timing.OnLapCompleted += OnLapCompleted;
                _timing.OnLapInvalidated += OnLapInvalidated;
            }
        }

        private void OnDestroy()
        {
            if (_timing != null)
            {
                _timing.OnLapCompleted -= OnLapCompleted;
                _timing.OnLapInvalidated -= OnLapInvalidated;
            }
        }

        private void Update()
        {
            if (_timing == null || _playerTransform == null)
            {
                return;
            }

            RecordIfDue();
            PlayGhostIfPossible();
        }

        private void RecordIfDue()
        {
            if (_raceComplete)
            {
                // Race already finished (or was abandoned) — nothing more
                // to add to this take, whether or not it ends up saved.
                return;
            }

            var elapsedSeconds = Time.time - _raceStartTime;
            if (elapsedSeconds < _nextSampleTime)
            {
                return;
            }

            _recordingBuffer.Add(new GhostSample(_playerTransform.position, _playerTransform.eulerAngles.y));
            _nextSampleTime += SampleIntervalSeconds;
        }

        private void PlayGhostIfPossible()
        {
            if (_ghostVisual == null)
            {
                return;
            }

            if (_bestRaceSamples == null)
            {
                _ghostVisual.gameObject.SetActive(false);
                return;
            }

            var elapsedSeconds = Time.time - _raceStartTime;
            if (!GhostMath.TrySamplePose(_bestRaceSamples, SampleIntervalSeconds, elapsedSeconds,
                out var position, out var yawDegrees))
            {
                _ghostVisual.gameObject.SetActive(false);
                return;
            }

            _ghostVisual.gameObject.SetActive(true);
            _ghostVisual.position = position;
            _ghostVisual.rotation = Quaternion.Euler(0f, yawDegrees, 0f);
        }

        private void OnLapCompleted(float lapTimeSeconds, bool isValid)
        {
            if (!isValid)
            {
                // One bad lap disqualifies the whole race recording — same
                // "only clean laps count" spirit as LapRecordStore, applied
                // to the full race instead of a single lap.
                _raceValid = false;
            }

            if (_timing.LapsCompleted < _targetLaps)
            {
                // Race still in progress — nothing to finalize yet, keep
                // recording (RecordIfDue continues on the next Update).
                return;
            }

            _raceComplete = true;
            var raceTimeSeconds = Time.time - _raceStartTime;

            if (!_raceValid || _recordingBuffer.Count == 0 || raceTimeSeconds >= _bestRaceTimeSeconds)
            {
                return;
            }

            _bestRaceTimeSeconds = raceTimeSeconds;
            _bestRaceSamples = new List<GhostSample>(_recordingBuffer);
            GhostRecordStore.SaveBestGhost(_scope, _targetLaps, raceTimeSeconds, _bestRaceSamples);
        }

        private void OnLapInvalidated()
        {
            // Invalid attempts do not publish a lap time, but they still
            // disqualify the continuous race recording from becoming a PB.
            _raceValid = false;
        }
    }
}
