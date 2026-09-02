namespace RKW.Physics
{
    /// <summary>
    /// Rodada 46 (2026-09-01) founder feedback: "pause e reiniciar nao
    /// esta alinhado centralizado com o texto meta" -- PauseButton was
    /// added immediately to the left of RaceRestartButton's REINICIAR
    /// button (same top-center row), but REINICIAR still centered ITSELF
    /// alone on Screen.width (as it always had, from back when it was the
    /// only button in that spot). Once PauseButton sat beside it, the
    /// PAIR's combined visual center drifted left of true screen center,
    /// no longer lining up with RaceManager's META/lap-target label
    /// underneath it (which centers on the full Screen.width, unchanged).
    ///
    /// This is the shared, single source of truth for both buttons' raw
    /// (unscaled) widths/height/gap, so the two of them size themselves as
    /// ONE centered block instead of two independently-centered rects
    /// that only happened to look right back when there was just one of
    /// them. Takes screenWidth as a parameter (instead of reading
    /// Screen.width internally) so it can be unit-tested in EditMode with
    /// plain numbers -- same convention as HudLayoutMath.
    /// </summary>
    public static class TopCenterButtonLayout
    {
        public const float PauseWidthRaw = 90f;
        public const float RestartWidthRaw = 110f;
        public const float GapRaw = 8f;
        public const float HeightRaw = 34f;

        /// <summary>X position (already scaled) of the left edge of the PAUSE+REINICIAR pair, so the pair as a whole centers on screenWidth.</summary>
        public static float PairLeftX(float scale, float screenWidth)
        {
            var pairWidth = (PauseWidthRaw + GapRaw + RestartWidthRaw) * scale;
            return (screenWidth - pairWidth) * 0.5f;
        }

        /// <summary>X position (already scaled) of RaceRestartButton's own rect -- immediately to the right of the PAUSE button, with GapRaw between them.</summary>
        public static float RestartButtonX(float scale, float screenWidth)
        {
            return PairLeftX(scale, screenWidth) + (PauseWidthRaw + GapRaw) * scale;
        }
    }
}
