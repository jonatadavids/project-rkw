using System;

namespace RKW.Telemetry
{
    /// <summary>
    /// M3-T07: pure rolling-average FPS accumulator (Requirement R12.4 —
    /// "coletar FPS a cada frame (rolling average)"). Deliberately has no
    /// Unity API dependency so it is trivially EditMode-testable: callers
    /// feed it Time.unscaledDeltaTime (or any deterministic sequence in
    /// tests) frame by frame.
    /// </summary>
    public sealed class FpsRollingAverage
    {
        private readonly float[] _samples;
        private readonly int _capacity;
        private int _count;
        private int _writeIndex;
        private float _sum;

        public FpsRollingAverage(int windowSampleCount = 60)
        {
            if (windowSampleCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(windowSampleCount), "Window must hold at least one sample.");
            }

            _capacity = windowSampleCount;
            _samples = new float[_capacity];
        }

        /// <summary>Number of samples currently held (≤ window capacity).</summary>
        public int SampleCount => _count;

        /// <summary>
        /// Average FPS across the current window. 0 until at least one
        /// positive-deltaTime sample has been recorded.
        /// </summary>
        public float CurrentAverageFps => _count == 0 ? 0f : _count / _sum;

        /// <summary>
        /// Records one frame's delta time. Non-positive values (paused frame,
        /// first frame, editor hitches reporting 0) are ignored — they do not
        /// correspond to a meaningful FPS reading and would otherwise produce
        /// a division by zero or an infinite instantaneous FPS.
        /// </summary>
        public void Sample(float deltaTimeSeconds)
        {
            if (deltaTimeSeconds <= 0f || float.IsNaN(deltaTimeSeconds) || float.IsInfinity(deltaTimeSeconds))
            {
                return;
            }

            if (_count < _capacity)
            {
                _samples[_writeIndex] = deltaTimeSeconds;
                _sum += deltaTimeSeconds;
                _count++;
            }
            else
            {
                _sum -= _samples[_writeIndex];
                _samples[_writeIndex] = deltaTimeSeconds;
                _sum += deltaTimeSeconds;
            }

            _writeIndex = (_writeIndex + 1) % _capacity;
        }

        public void Reset()
        {
            Array.Clear(_samples, 0, _samples.Length);
            _count = 0;
            _writeIndex = 0;
            _sum = 0f;
        }
    }
}
