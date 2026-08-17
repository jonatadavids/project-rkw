using UnityEngine;

namespace RKW.UI
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform _rectTransform;
        private Rect _lastSafeArea;
        private Vector2Int _lastScreenSize;

        private void Awake()
        {
            _rectTransform = (RectTransform)transform;
        }

        private void OnEnable()
        {
            ApplyCurrentSafeArea();
        }

        private void OnRectTransformDimensionsChange()
        {
            ApplyCurrentSafeArea();
        }

        internal static void CalculateNormalizedAnchors(
            Rect safeArea,
            Vector2 screenSize,
            out Vector2 anchorMin,
            out Vector2 anchorMax)
        {
            if (screenSize.x <= 0f || screenSize.y <= 0f)
            {
                anchorMin = Vector2.zero;
                anchorMax = Vector2.one;
                return;
            }

            anchorMin = new Vector2(
                Mathf.Clamp01(safeArea.xMin / screenSize.x),
                Mathf.Clamp01(safeArea.yMin / screenSize.y));
            anchorMax = new Vector2(
                Mathf.Clamp01(safeArea.xMax / screenSize.x),
                Mathf.Clamp01(safeArea.yMax / screenSize.y));
        }

        private void ApplyCurrentSafeArea()
        {
            if (_rectTransform == null)
            {
                return;
            }

            var screenSize = new Vector2Int(Screen.width, Screen.height);
            var safeArea = Screen.safeArea;
            if (screenSize == _lastScreenSize && safeArea == _lastSafeArea)
            {
                return;
            }

            _lastScreenSize = screenSize;
            _lastSafeArea = safeArea;
            CalculateNormalizedAnchors(safeArea, screenSize, out var minimum, out var maximum);
            _rectTransform.anchorMin = minimum;
            _rectTransform.anchorMax = maximum;
            _rectTransform.offsetMin = Vector2.zero;
            _rectTransform.offsetMax = Vector2.zero;
        }
    }
}
