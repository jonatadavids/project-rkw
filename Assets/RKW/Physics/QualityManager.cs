using UnityEngine;

namespace RKW.Physics
{
    /// <summary>
    /// Manages quality tiers (Low/Medium/High) with auto-adjustment based on
    /// FPS sampling window, cooldown, and hysteresis to avoid oscillation.
    /// M3-T04 + M3-T05: Validates R12.3, R12.5.
    /// </summary>
    public sealed class QualityManager : MonoBehaviour
    {
        public enum QualityTier { Low = 0, Medium = 1, High = 2 }

        [Header("Thresholds (calibration hypotheses)")]
        [SerializeField] private float downgradeWindowSeconds = 3f;
        [SerializeField] private float downgradeThresholdFps = 28f;
        [SerializeField] private float upgradeWindowSeconds = 10f;
        [SerializeField] private float upgradeThresholdFps = 55f;
        [SerializeField] private float cooldownSeconds = 30f;
        [SerializeField] private float hysteresisMarginFps = 5f;

        private QualityTier _currentTier = QualityTier.Medium;
        private float _lastChangeTime = -999f;
        private float _downgradeSampleSum;
        private int _downgradeSampleCount;
        private float _upgradeSampleSum;
        private int _upgradeSampleCount;
        private float _downgradeWindowStart;
        private float _upgradeWindowStart;

        public QualityTier CurrentTier => _currentTier;
        public int DowngradeCount { get; private set; }
        public int UpgradeCount { get; private set; }

        /// <summary>
        /// Initialize with detected tier based on hardware.
        /// </summary>
        public void Initialize(QualityTier detectedTier)
        {
            _currentTier = detectedTier;
            ApplyTier(_currentTier);
        }

        /// <summary>
        /// Call every frame with current FPS.
        /// </summary>
        public void SampleFps(float currentFps, float time)
        {
            SampleForDowngrade(currentFps, time);
            SampleForUpgrade(currentFps, time);
        }

        /// <summary>
        /// Pure logic for auto-adjust decision. Exposed for testing.
        /// Returns: -1 downgrade, 0 no change, +1 upgrade.
        /// </summary>
        public static int EvaluateAdjustment(
            float downgradeAvgFps,
            float downgradeWindowElapsed,
            float downgradeWindowRequired,
            float downgradeThreshold,
            float upgradeAvgFps,
            float upgradeWindowElapsed,
            float upgradeWindowRequired,
            float upgradeThreshold,
            float hysteresisMargin,
            float timeSinceLastChange,
            float cooldown,
            int currentTierInt,
            int maxTier)
        {
            // Downgrade: avg in window < threshold AND window full
            if (downgradeWindowElapsed >= downgradeWindowRequired
                && downgradeAvgFps < downgradeThreshold
                && currentTierInt > 0)
            {
                return -1;
            }

            // Upgrade: avg in window > threshold AND cooldown met AND hysteresis AND not at max
            if (upgradeWindowElapsed >= upgradeWindowRequired
                && upgradeAvgFps > upgradeThreshold
                && timeSinceLastChange >= cooldown
                && upgradeAvgFps > (downgradeThreshold + hysteresisMargin)
                && currentTierInt < maxTier)
            {
                return 1;
            }

            return 0;
        }

        private void SampleForDowngrade(float fps, float time)
        {
            if (_downgradeSampleCount == 0)
            {
                _downgradeWindowStart = time;
            }

            _downgradeSampleSum += fps;
            _downgradeSampleCount++;

            var elapsed = time - _downgradeWindowStart;
            if (elapsed < downgradeWindowSeconds)
            {
                return;
            }

            var avg = _downgradeSampleSum / _downgradeSampleCount;
            var decision = EvaluateAdjustment(
                avg, elapsed, downgradeWindowSeconds, downgradeThresholdFps,
                0f, 0f, upgradeWindowSeconds, upgradeThresholdFps,
                hysteresisMarginFps, time - _lastChangeTime, cooldownSeconds,
                (int)_currentTier, 2);

            if (decision == -1)
            {
                _currentTier = (QualityTier)((int)_currentTier - 1);
                _lastChangeTime = time;
                DowngradeCount++;
                ApplyTier(_currentTier);
            }

            // Reset window
            _downgradeSampleSum = 0f;
            _downgradeSampleCount = 0;
        }

        private void SampleForUpgrade(float fps, float time)
        {
            if (_upgradeSampleCount == 0)
            {
                _upgradeWindowStart = time;
            }

            _upgradeSampleSum += fps;
            _upgradeSampleCount++;

            var elapsed = time - _upgradeWindowStart;
            if (elapsed < upgradeWindowSeconds)
            {
                return;
            }

            var avg = _upgradeSampleSum / _upgradeSampleCount;
            var decision = EvaluateAdjustment(
                0f, 0f, downgradeWindowSeconds, downgradeThresholdFps,
                avg, elapsed, upgradeWindowSeconds, upgradeThresholdFps,
                hysteresisMarginFps, time - _lastChangeTime, cooldownSeconds,
                (int)_currentTier, 2);

            if (decision == 1)
            {
                _currentTier = (QualityTier)((int)_currentTier + 1);
                _lastChangeTime = time;
                UpgradeCount++;
                ApplyTier(_currentTier);
            }

            // Reset window
            _upgradeSampleSum = 0f;
            _upgradeSampleCount = 0;
        }

        private static void ApplyTier(QualityTier tier)
        {
            QualitySettings.SetQualityLevel((int)tier, true);
        }
    }
}
