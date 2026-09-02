using NUnit.Framework;
using RKW.Physics;

namespace RKW.Physics.Tests.EditMode
{
    /// <summary>
    /// Round 46 (2026-09-01): checks the pure math behind centering the
    /// PAUSE+REINICIAR pair as one block (see TopCenterButtonLayout's own
    /// doc comment -- founder feedback: the pair had drifted out of
    /// alignment with RaceManager's centered META label once PauseButton
    /// was added beside RaceRestartButton).
    /// </summary>
    public sealed class TopCenterButtonLayoutTests
    {
        [Test]
        public void PairCentersOnScreenWidth_AcrossScalesAndWidths()
        {
            foreach (var scale in new[] { 1f, 1.25f, 1.5f, 2f, 3.33f })
            {
                foreach (var screenWidth in new[] { 720f, 1080f, 1440f, 2400f })
                {
                    var pairLeft = TopCenterButtonLayout.PairLeftX(scale, screenWidth);
                    var pairWidth = (TopCenterButtonLayout.PauseWidthRaw + TopCenterButtonLayout.GapRaw
                        + TopCenterButtonLayout.RestartWidthRaw) * scale;
                    var pairCenter = pairLeft + pairWidth * 0.5f;

                    Assert.That(pairCenter, Is.EqualTo(screenWidth * 0.5f).Within(0.01f),
                        $"Pair not centered at scale={scale}, screenWidth={screenWidth}");
                }
            }
        }

        [Test]
        public void RestartButton_SitsImmediatelyRightOfPauseButton_WithGapBetween()
        {
            const float scale = 1.5f;
            const float screenWidth = 1080f;

            var pauseLeft = TopCenterButtonLayout.PairLeftX(scale, screenWidth);
            var pauseRight = pauseLeft + TopCenterButtonLayout.PauseWidthRaw * scale;
            var restartLeft = TopCenterButtonLayout.RestartButtonX(scale, screenWidth);

            Assert.That(restartLeft, Is.EqualTo(pauseRight + TopCenterButtonLayout.GapRaw * scale).Within(0.01f));
        }

        [Test]
        public void PairFitsWithinScreenWidth_OnASmallScreen()
        {
            // Sanity check: the combined pair should never be wider than
            // the screen itself for any resolution this game actually
            // targets (Screen.height/720f scale, so scale never drops
            // below 1).
            var pairWidth = TopCenterButtonLayout.PauseWidthRaw + TopCenterButtonLayout.GapRaw
                + TopCenterButtonLayout.RestartWidthRaw;
            Assert.That(pairWidth, Is.LessThan(480f));
        }
    }
}
