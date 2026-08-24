using UnityEngine;

namespace RKW.Physics
{
    /// <summary>
    /// Pure layout logic for the on-screen touch controls (founder playtest
    /// feedback, 2026-08-19: pedals side-by-side rather than stacked, and a
    /// steering wheel that visibly rotates). No Unity lifecycle dependency,
    /// so it is EditMode testable.
    /// </summary>
    public static class KartInputLayoutMath
    {
        /// <summary>Visual wheel rotation for a given steering input, clamped to +/-1 before scaling.</summary>
        public static float CalculateSteeringWheelRotationDegrees(float steeringValue, float maxRotationDegrees)
        {
            return Mathf.Clamp(steeringValue, -1f, 1f) * maxRotationDegrees;
        }

        /// <summary>
        /// True when a touch at <paramref name="touchX"/> falls in the left
        /// half of the right-hand control zone (brake), false for the right
        /// half (throttle) — side-by-side pedals instead of the previous
        /// stacked top/bottom split.
        /// </summary>
        public static bool IsBrakeSide(float touchX, float rightZoneStartX, float rightZoneWidth)
        {
            if (rightZoneWidth <= 0f)
            {
                return false;
            }

            var relativeX = (touchX - rightZoneStartX) / rightZoneWidth;
            return relativeX < 0.5f;
        }
    }
}
