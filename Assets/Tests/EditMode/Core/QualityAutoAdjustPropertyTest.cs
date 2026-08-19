using NUnit.Framework;
using RKW.Physics;

namespace RKW.Tests.EditMode.Core
{
    /// <summary>
    /// Property 24: Quality Auto-Adjust with Hysteresis
    /// Downgrade when 3s avg FPS less than 28.
    /// Upgrade ONLY when: 10s avg greater than 55 AND cooldown 30s AND hysteresis 5 FPS margin.
    /// Validates: Requirements 12.5
    /// </summary>
    public sealed class QualityAutoAdjustPropertyTest
    {
        [Test]
        public void Downgrade_WhenAvgBelowThreshold_AndWindowFull()
        {
            var result = QualityManager.EvaluateAdjustment(
                downgradeAvgFps: 25f,
                downgradeWindowElapsed: 3.1f,
                downgradeWindowRequired: 3f,
                downgradeThreshold: 28f,
                upgradeAvgFps: 0f,
                upgradeWindowElapsed: 0f,
                upgradeWindowRequired: 10f,
                upgradeThreshold: 55f,
                hysteresisMargin: 5f,
                timeSinceLastChange: 31f,
                cooldown: 30f,
                currentTierInt: 1,
                maxTier: 2);

            Assert.That(result, Is.EqualTo(-1));
        }

        [Test]
        public void NoDowngrade_WhenAvgAboveThreshold()
        {
            var result = QualityManager.EvaluateAdjustment(
                downgradeAvgFps: 35f,
                downgradeWindowElapsed: 3.5f,
                downgradeWindowRequired: 3f,
                downgradeThreshold: 28f,
                upgradeAvgFps: 0f,
                upgradeWindowElapsed: 0f,
                upgradeWindowRequired: 10f,
                upgradeThreshold: 55f,
                hysteresisMargin: 5f,
                timeSinceLastChange: 5f,
                cooldown: 30f,
                currentTierInt: 1,
                maxTier: 2);

            Assert.That(result, Is.EqualTo(0));
        }

        [Test]
        public void NoDowngrade_WhenAlreadyAtLowest()
        {
            var result = QualityManager.EvaluateAdjustment(
                downgradeAvgFps: 20f,
                downgradeWindowElapsed: 4f,
                downgradeWindowRequired: 3f,
                downgradeThreshold: 28f,
                upgradeAvgFps: 0f,
                upgradeWindowElapsed: 0f,
                upgradeWindowRequired: 10f,
                upgradeThreshold: 55f,
                hysteresisMargin: 5f,
                timeSinceLastChange: 40f,
                cooldown: 30f,
                currentTierInt: 0, // already Low
                maxTier: 2);

            Assert.That(result, Is.EqualTo(0));
        }

        [Test]
        public void Upgrade_WhenAllConditionsMet()
        {
            var result = QualityManager.EvaluateAdjustment(
                downgradeAvgFps: 60f,
                downgradeWindowElapsed: 1f,
                downgradeWindowRequired: 3f,
                downgradeThreshold: 28f,
                upgradeAvgFps: 58f,
                upgradeWindowElapsed: 11f,
                upgradeWindowRequired: 10f,
                upgradeThreshold: 55f,
                hysteresisMargin: 5f,
                timeSinceLastChange: 35f,
                cooldown: 30f,
                currentTierInt: 1,
                maxTier: 2);

            Assert.That(result, Is.EqualTo(1));
        }

        [Test]
        public void NoUpgrade_WhenCooldownNotMet()
        {
            var result = QualityManager.EvaluateAdjustment(
                downgradeAvgFps: 60f,
                downgradeWindowElapsed: 1f,
                downgradeWindowRequired: 3f,
                downgradeThreshold: 28f,
                upgradeAvgFps: 58f,
                upgradeWindowElapsed: 11f,
                upgradeWindowRequired: 10f,
                upgradeThreshold: 55f,
                hysteresisMargin: 5f,
                timeSinceLastChange: 15f, // only 15s since last change
                cooldown: 30f,
                currentTierInt: 0,
                maxTier: 2);

            Assert.That(result, Is.EqualTo(0));
        }

        [Test]
        public void NoUpgrade_WhenHysteresisNotMet()
        {
            var result = QualityManager.EvaluateAdjustment(
                downgradeAvgFps: 60f,
                downgradeWindowElapsed: 1f,
                downgradeWindowRequired: 3f,
                downgradeThreshold: 28f,
                upgradeAvgFps: 30f, // above 28 but NOT above 28+5=33
                upgradeWindowElapsed: 11f,
                upgradeWindowRequired: 10f,
                upgradeThreshold: 55f,
                hysteresisMargin: 5f,
                timeSinceLastChange: 40f,
                cooldown: 30f,
                currentTierInt: 0,
                maxTier: 2);

            Assert.That(result, Is.EqualTo(0));
        }

        [Test]
        public void NoUpgrade_WhenAlreadyAtMaxTier()
        {
            var result = QualityManager.EvaluateAdjustment(
                downgradeAvgFps: 60f,
                downgradeWindowElapsed: 1f,
                downgradeWindowRequired: 3f,
                downgradeThreshold: 28f,
                upgradeAvgFps: 60f,
                upgradeWindowElapsed: 11f,
                upgradeWindowRequired: 10f,
                upgradeThreshold: 55f,
                hysteresisMargin: 5f,
                timeSinceLastChange: 40f,
                cooldown: 30f,
                currentTierInt: 2, // already High
                maxTier: 2);

            Assert.That(result, Is.EqualTo(0));
        }

        [Test]
        public void PropertyTest_200Iterations_NoOscillation()
        {
            var random = new System.Random(2401);

            for (var iteration = 0; iteration < 200; iteration++)
            {
                var currentTier = random.Next(0, 3);
                var timeSinceChange = RandomFloat(random, 0f, 60f);
                var downgradeAvg = RandomFloat(random, 15f, 40f);
                var upgradeAvg = RandomFloat(random, 30f, 70f);

                var result = QualityManager.EvaluateAdjustment(
                    downgradeAvg, 3.5f, 3f, 28f,
                    upgradeAvg, 11f, 10f, 55f,
                    5f, timeSinceChange, 30f,
                    currentTier, 2);

                // Can't both upgrade and downgrade
                Assert.That(result, Is.InRange(-1, 1));

                // If cooldown not met, can't upgrade
                if (timeSinceChange < 30f)
                {
                    Assert.That(result, Is.Not.EqualTo(1),
                        $"Upgrade not allowed during cooldown at iteration {iteration}");
                }

                // If at tier 0, can't downgrade
                if (currentTier == 0)
                {
                    Assert.That(result, Is.Not.EqualTo(-1),
                        $"Downgrade not allowed at tier 0 at iteration {iteration}");
                }

                // If at max tier, can't upgrade
                if (currentTier == 2)
                {
                    Assert.That(result, Is.Not.EqualTo(1),
                        $"Upgrade not allowed at max tier at iteration {iteration}");
                }
            }
        }

        private static float RandomFloat(System.Random random, float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }
    }
}
