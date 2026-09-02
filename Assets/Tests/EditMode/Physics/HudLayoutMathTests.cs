using NUnit.Framework;
using RKW.Physics;

namespace RKW.Physics.Tests.EditMode
{
    /// <summary>
    /// Round 44 (2026-09-01): checks the pure math behind the CLASSIFICAÇÃO
    /// panel / TimingHUD overlap fix (see HudLayoutMath's own doc comment).
    /// Cannot exercise the real OnGUI/Screen.height code path in an EditMode
    /// test, so this validates the formula directly against known
    /// resolutions and safe-area insets instead.
    /// </summary>
    public sealed class HudLayoutMathTests
    {
        [Test]
        public void LowScaleDevice_PanelPushedBelowTimingHud()
        {
            // scale = 1 (e.g. a 720p-tall screen) is exactly the case that
            // produced the founder's reported overlap: the old unscaled
            // "80 * scale" = 80 sat well inside TimingHUD's 10..154 block.
            var top = HudLayoutMath.ComputeStandingsPanelTop(scale: 1f, safeAreaYMin: 0f, defaultTopPixels: 80f);

            var timingHudBottom = 0f + 10f + HudLayoutMath.TimingHudBlockHeightPixels;
            Assert.That(top, Is.GreaterThanOrEqualTo(timingHudBottom + HudLayoutMath.ClearanceMarginPixels));
        }

        [Test]
        public void HighScaleDevice_KeepsOwnScaledPositionAboveTimingHud()
        {
            // On a tall/high-res screen, the old "80 * scale" default
            // already clears TimingHUD comfortably -- the fix should not
            // needlessly push it even further down.
            var top = HudLayoutMath.ComputeStandingsPanelTop(scale: 3.33f, safeAreaYMin: 0f, defaultTopPixels: 80f);
            Assert.That(top, Is.EqualTo(80f * 3.33f).Within(0.01f));
        }

        [Test]
        public void NonZeroSafeAreaInset_StillClearsTimingHud()
        {
            // A device with a landscape notch/inset shifts TimingHUD's
            // whole block down by the same amount -- the fix must track
            // that, not just assume safeArea.yMin is always 0.
            const float safeAreaYMin = 60f;
            var top = HudLayoutMath.ComputeStandingsPanelTop(scale: 1f, safeAreaYMin: safeAreaYMin, defaultTopPixels: 80f);

            var timingHudBottom = safeAreaYMin + 10f + HudLayoutMath.TimingHudBlockHeightPixels;
            Assert.That(top, Is.GreaterThanOrEqualTo(timingHudBottom + HudLayoutMath.ClearanceMarginPixels));
        }

        [Test]
        public void ResultNeverOverlapsTimingHud_AcrossManyScalesAndInsets()
        {
            foreach (var scale in new[] { 1f, 1.25f, 1.5f, 2f, 2.5f, 3.33f })
            {
                foreach (var safeAreaYMin in new[] { 0f, 24f, 60f, 120f })
                {
                    var top = HudLayoutMath.ComputeStandingsPanelTop(scale, safeAreaYMin, defaultTopPixels: 80f);
                    var timingHudBottom = safeAreaYMin + 10f + HudLayoutMath.TimingHudBlockHeightPixels;

                    Assert.That(top, Is.GreaterThanOrEqualTo(timingHudBottom),
                        $"Overlap at scale={scale}, safeAreaYMin={safeAreaYMin}: panelTop={top}, timingHudBottom={timingHudBottom}");
                }
            }
        }
    }
}
