using System;
using UnityEngine;

namespace RKW.Physics
{
    /// <summary>
    /// Lightweight timing system for the M2 vertical slice.
    /// Tracks lap times using checkpoint triggers. Validates laps by
    /// confirming all checkpoints were passed in order.
    /// Will be evolved into full TimingManager in M4 (with sectors, delta, ideal lap).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TimingManagerLite : MonoBehaviour
    {
        [SerializeField] private int totalCheckpoints = 3;

        private int _nextExpectedCheckpoint;
        private float _lapStartTime;
        private bool _lapInProgress;
        private bool _lapInvalid;

        public float LastLapTime { get; private set; }
        public float BestLapTime { get; private set; } = float.MaxValue;
        // Rodada 46 (2026-09-01), quinta passada -- founder feedback:
        // "quanto termina a corrida a tela para porém a contagem de outra
        // volta aparece no cronometro". The earlier StopTiming() fix this
        // same day set _lapInProgress false, but HandleStartFinishCrossing
        // unconditionally calls StartNewLap() right after firing
        // OnLapCompleted -- and RaceManager's finish handler (subscribed
        // to that exact event) calls StopTiming() SYNCHRONOUSLY inside
        // that same call, so StartNewLap() ran immediately afterward in
        // the same call stack and flipped _lapInProgress back to true,
        // completely undoing the fix within the same frame. _stopped adds
        // a second, sticky guard: once true, StartNewLap() below refuses
        // to start a new lap at all, AND this getter checks it directly
        // too (belt and suspenders, since the exact same "something calls
        // StartNewLap() right after" pattern is what broke the first fix).
        private bool _stopped;

        public float CurrentLapTime => (_lapInProgress && !_stopped) ? Time.time - _lapStartTime : 0f;
        public int LapsCompleted { get; private set; }
        public bool IsCurrentLapValid => _lapInProgress && !_lapInvalid;

        public event Action<float, bool> OnLapCompleted; // (lapTime, isValid)
        public event Action OnLapInvalidated;

        // Round 44 (2026-09-01) founder feedback: "vc pode colocar
        // checkpoint sei la dividir a pista em 3 colocar uma lista e
        // informar naquele ponto vc foi 1 segundo a mais ou menos da
        // ultima volta" -- this reuses the checkpoint triggers that
        // already exist for lap VALIDATION (CheckpointTrigger/
        // KartCheckpointDetector above) to also report a per-checkpoint
        // "split" time, plus how it compares to the same checkpoint on the
        // last VALID lap. (checkpointIndex, splitTimeSeconds,
        // deltaVsPreviousValidLapSecondsOrNull -- negative delta = faster
        // than last lap at this point, positive = slower, null = no
        // previous valid lap to compare against yet, e.g. lap 1).
        public event Action<int, float, float?> OnCheckpointSplit;

        private float[] _currentLapSplitSeconds;
        private float[] _previousValidLapSplitSeconds;

        public void Configure(int checkpointCount)
        {
            totalCheckpoints = Mathf.Max(1, checkpointCount);
            Reset();
        }

        /// <summary>
        /// Lazily (re)allocates the two split-time arrays whenever
        /// totalCheckpoints changes (normally only once, from Configure) —
        /// kept separate from Reset so a defensive call from
        /// RegisterCheckpointHit before Configure ever ran (should not
        /// happen in practice, but costs nothing to guard) still has valid
        /// arrays to write into instead of throwing.
        /// </summary>
        private void EnsureSplitArrays()
        {
            if (_currentLapSplitSeconds != null && _currentLapSplitSeconds.Length == totalCheckpoints)
            {
                return;
            }

            _currentLapSplitSeconds = new float[totalCheckpoints];
            _previousValidLapSplitSeconds = new float[totalCheckpoints];
            for (var i = 0; i < totalCheckpoints; i++)
            {
                _currentLapSplitSeconds[i] = -1f;
                _previousValidLapSplitSeconds[i] = -1f;
            }
        }

        public void RegisterCheckpointHit(int checkpointIndex, bool isStartFinish,
            bool isCrossingForward = true)
        {
            if (isStartFinish)
            {
                // A reverse crossing is not a lap boundary. Ignore it without
                // resetting checkpoint progress so backing across the line
                // cannot start, complete, or consume a lap.
                if (!isCrossingForward)
                {
                    return;
                }

                HandleStartFinishCrossing();
                return;
            }

            if (!_lapInProgress)
            {
                return;
            }

            if (checkpointIndex == _nextExpectedCheckpoint)
            {
                EnsureSplitArrays();
                if (checkpointIndex >= 0 && checkpointIndex < _currentLapSplitSeconds.Length)
                {
                    var splitTime = Time.time - _lapStartTime;
                    _currentLapSplitSeconds[checkpointIndex] = splitTime;
                    var previousSplit = _previousValidLapSplitSeconds[checkpointIndex];
                    float? delta = previousSplit >= 0f ? splitTime - previousSplit : (float?)null;
                    OnCheckpointSplit?.Invoke(checkpointIndex, splitTime, delta);
                }

                _nextExpectedCheckpoint++;
            }
            else if (checkpointIndex != _nextExpectedCheckpoint - 1) // allow re-trigger of same
            {
                // Missed checkpoint or wrong order
                _lapInvalid = true;
            }
        }

        private void HandleStartFinishCrossing()
        {
            if (!_lapInProgress)
            {
                // Start first lap
                StartNewLap();
                return;
            }

            // Completing a lap
            var lapTime = Time.time - _lapStartTime;
            var allCheckpointsPassed = _nextExpectedCheckpoint >= totalCheckpoints;
            var isValid = allCheckpointsPassed && !_lapInvalid;

            if (isValid && lapTime < BestLapTime)
            {
                BestLapTime = lapTime;
            }

            if (isValid)
            {
                LastLapTime = lapTime;
                LapsCompleted++;
                // Snapshot this lap's splits as the new comparison baseline
                // BEFORE StartNewLap() below resets _currentLapSplitSeconds
                // for the next lap -- only a VALID lap's splits are worth
                // comparing future laps against.
                EnsureSplitArrays();
                Array.Copy(_currentLapSplitSeconds, _previousValidLapSplitSeconds, totalCheckpoints);
            }

            if (isValid)
            {
                OnLapCompleted?.Invoke(lapTime, true);
            }
            else
            {
                OnLapInvalidated?.Invoke();
            }

            StartNewLap();
        }

        /// <summary>
        /// Rodada 46 (2026-09-01) founder feedback: "quando termina a
        /// corrida o carro parou mas o tempo la no canto superior direito
        /// ficou rodando" -- RaceManager's own finish time was already
        /// frozen, and the kart itself now stops too (see
        /// KartDynamics.StopImmediately), but TimingHUD's "VOLTA: ..."
        /// readout (top-right corner) reads CurrentLapTime, which is just
        /// `Time.time - lapStartTime` for as long as _lapInProgress is
        /// true -- and crossing the finish line on the FINAL lap still
        /// calls StartNewLap() like any other lap crossing (this class has
        /// no idea the race, as opposed to just this one lap, is over), so
        /// that clock kept counting up forever after the "race". Called by
        /// RaceManager the instant it marks the race finished.
        /// </summary>
        public void StopTiming()
        {
            _stopped = true;
            _lapInProgress = false;
        }

        private void StartNewLap()
        {
            if (_stopped)
            {
                return;
            }

            _lapStartTime = Time.time;
            _lapInProgress = true;
            _lapInvalid = false;
            _nextExpectedCheckpoint = 0;
            EnsureSplitArrays();
            for (var i = 0; i < _currentLapSplitSeconds.Length; i++)
            {
                _currentLapSplitSeconds[i] = -1f;
            }
        }

        private void Reset()
        {
            _lapInProgress = false;
            _lapInvalid = false;
            _nextExpectedCheckpoint = 0;
            _lapStartTime = 0f;
            LastLapTime = 0f;
            BestLapTime = float.MaxValue;
            LapsCompleted = 0;
            EnsureSplitArrays();
            for (var i = 0; i < totalCheckpoints; i++)
            {
                _currentLapSplitSeconds[i] = -1f;
                _previousValidLapSplitSeconds[i] = -1f;
            }
        }
    }
}
