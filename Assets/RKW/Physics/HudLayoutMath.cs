using UnityEngine;

namespace RKW.Physics
{
    /// <summary>
    /// Round 44 (2026-09-01) founder feedback, screenshots of the in-game
    /// HUD: "veja que a classificacao esta sobrepondo a outra informacao" —
    /// the CLASSIFICAÇÃO standings panel (<see cref="RaceStandingsHud"/>)
    /// was overlapping the lap-timing readout (<see cref="TimingHUD"/>),
    /// both anchored near the top-right corner.
    ///
    /// Root cause: <see cref="TimingHUD"/> draws 4 stacked 36px-tall rows
    /// starting at <c>Screen.safeArea.yMin + 10</c> using FIXED pixel
    /// sizes (it does not scale with screen resolution), so its block
    /// always ends at <c>safeArea.yMin + 154</c> regardless of device.
    /// <see cref="RaceStandingsHud"/>, on the other hand, starts its panel
    /// at <c>80 * scale</c>, where <c>scale = max(1, Screen.height/720)</c>
    /// — on a lower-resolution phone (smaller scale), 80*scale can land
    /// well above 154, landing right on top of TimingHUD's rows.
    ///
    /// This helper computes a safe top position for the standings panel:
    /// whichever is LOWER on screen (bigger Y) of (a) its own scaled
    /// default position, or (b) a fixed clearance below TimingHUD's fixed
    /// block — so the two panels never overlap, on any resolution or
    /// safe-area inset, while still letting the standings panel sit
    /// higher up on tall/high-scale screens where there's no actual
    /// collision risk. Pure math (no MonoBehaviour/OnGUI dependency) so it
    /// can be checked with plain EditMode tests instead of only trusting
    /// it by eye on a real device.
    /// </summary>
    public static class HudLayoutMath
    {
        /// <summary>
        /// TimingHUD's own fixed (non-scaled) block height in pixels: 4
        /// rows of 36px starting at its anchor, i.e. rows at +0, +36, +72,
        /// +108, each 36px tall -> bottom edge at +144. See TimingHUD.OnGUI.
        /// </summary>
        public const float TimingHudBlockHeightPixels = 144f;

        /// <summary>Extra breathing room below TimingHUD's block before the standings panel may start.</summary>
        public const float ClearanceMarginPixels = 20f;

        /// <summary>
        /// Returns the Y position the standings panel should use, guaranteed
        /// to sit at or below TimingHUD's fixed block (plus a margin), no
        /// matter the resolution-dependent <paramref name="scale"/>.
        /// </summary>
        public static float ComputeStandingsPanelTop(float scale, float safeAreaYMin, float defaultTopPixels)
        {
            var scaledDefault = defaultTopPixels * scale;
            var timingHudBottom = safeAreaYMin + 10f + TimingHudBlockHeightPixels + ClearanceMarginPixels;
            return Mathf.Max(scaledDefault, timingHudBottom);
        }
    }
}
