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
        public float CurrentLapTime => _lapInProgress ? Time.time - _lapStartTime : 0f;
        public int LapsCompleted { get; private set; }
        public bool IsCurrentLapValid => _lapInProgress && !_lapInvalid;

        public event Action<float, bool> OnLapCompleted; // (lapTime, isValid)

        public void Configure(int checkpointCount)
        {
            totalCheckpoints = Mathf.Max(1, checkpointCount);
            Reset();
        }

        public void RegisterCheckpointHit(int checkpointIndex, bool isStartFinish)
        {
            if (isStartFinish)
            {
                HandleStartFinishCrossing();
                return;
            }

            if (!_lapInProgress)
            {
                return;
            }

            if (checkpointIndex == _nextExpectedCheckpoint)
            {
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

            LastLapTime = lapTime;
            LapsCompleted++;

            if (isValid && lapTime < BestLapTime)
            {
                BestLapTime = lapTime;
            }

            OnLapCompleted?.Invoke(lapTime, isValid);
            StartNewLap();
        }

        private void StartNewLap()
        {
            _lapStartTime = Time.time;
            _lapInProgress = true;
            _lapInvalid = false;
            _nextExpectedCheckpoint = 0;
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
        }
    }
}
