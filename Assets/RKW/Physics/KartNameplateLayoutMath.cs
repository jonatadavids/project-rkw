using UnityEngine;

namespace RKW.Physics
{
    /// <summary>
    /// Rodada 46 (2026-09-01) founder request: "nos kart seria legal ter o
    /// nome em cima do carrinho na hora da corrida, até mesmo o fantasma".
    /// This is the pure math behind <see cref="KartNameplateHud"/>'s
    /// screen-space label placement, pulled out into its own static class
    /// (same convention as HudLayoutMath/TopCenterButtonLayout) so it can
    /// be exercised by an EditMode test with plain numbers.
    /// Camera.WorldToScreenPoint itself needs a live scene Camera, which
    /// EditMode tests don't have -- but everything AFTER that call
    /// (deciding whether the resulting point is behind the camera, and
    /// turning it into an OnGUI Rect) is ordinary arithmetic that doesn't,
    /// so it lives here instead of inline inside KartNameplateHud.OnGUI().
    /// </summary>
    public static class KartNameplateLayoutMath
    {
        public const float LabelWidthRaw = 160f;
        public const float LabelHeightRaw = 22f;

        /// <summary>
        /// True when a Camera.WorldToScreenPoint result is behind the
        /// camera -- its x/y are mirrored nonsense in that case, so the
        /// caller should skip drawing that frame's label rather than
        /// trust them.
        /// </summary>
        public static bool IsBehindCamera(Vector3 screenPoint)
        {
            return screenPoint.z <= 0f;
        }

        /// <summary>
        /// Converts a Camera.WorldToScreenPoint result (bottom-left
        /// origin, Y grows up) into an OnGUI Rect (top-left origin, Y
        /// grows down) centered on that point, scaled the same way every
        /// other HUD element in this prototype scales
        /// (Mathf.Max(1f, Screen.height / 720f)).
        /// </summary>
        public static Rect ComputeLabelRect(Vector3 screenPoint, float screenHeight, float scale)
        {
            var labelWidth = LabelWidthRaw * scale;
            var labelHeight = LabelHeightRaw * scale;
            var x = screenPoint.x - labelWidth * 0.5f;
            var y = screenHeight - screenPoint.y - labelHeight * 0.5f;
            return new Rect(x, y, labelWidth, labelHeight);
        }
    }
}
