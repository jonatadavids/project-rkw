using UnityEngine;

namespace RKW.Physics
{
    /// <summary>
    /// Simple on-screen display for the vertical slice timing.
    /// Shows: current lap time, last lap, best lap, laps completed.
    /// </summary>
    [RequireComponent(typeof(TimingManagerLite))]
    public sealed class TimingHUD : MonoBehaviour
    {
        private TimingManagerLite _timing;
        private GUIStyle _style;
        private string _lastLapDisplay = "";
        private string _validityDisplay = "";
        private bool _hasLapFeedback;

        private void Awake()
        {
            _timing = GetComponent<TimingManagerLite>();
            _timing.OnLapCompleted += OnLapCompleted;
            _timing.OnLapInvalidated += OnLapInvalidated;
        }

        private void OnDestroy()
        {
            if (_timing != null)
            {
                _timing.OnLapCompleted -= OnLapCompleted;
                _timing.OnLapInvalidated -= OnLapInvalidated;
            }
        }

        private void OnLapCompleted(float lapTime, bool isValid)
        {
            _lastLapDisplay = FormatTime(lapTime);
            _validityDisplay = isValid ? "VÁLIDA" : "INVÁLIDA";
            _hasLapFeedback = true;
        }

        private void OnLapInvalidated()
        {
            _lastLapDisplay = "";
            _validityDisplay = "INVÁLIDA";
            _hasLapFeedback = true;
        }

        private void OnGUI()
        {
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 28,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.UpperRight
                };
                _style.normal.textColor = Color.white;
            }

            var safeArea = Screen.safeArea;
            var x = safeArea.xMax - 320f;
            var y = safeArea.yMin + 10f;

            GUI.Label(new Rect(x, y, 310f, 36f),
                $"VOLTA: {FormatTime(_timing.CurrentLapTime)}", _style);

            if (_hasLapFeedback)
            {
                var lastColor = _validityDisplay == "VÁLIDA" ? "white" : "red";
                _style.normal.textColor = lastColor == "white" ? Color.white : Color.red;
                GUI.Label(new Rect(x, y + 36f, 310f, 36f),
                    _validityDisplay == "VÁLIDA"
                        ? $"ÚLTIMA: {_lastLapDisplay} (VÁLIDA)"
                        : "VOLTA INVÁLIDA", _style);
                _style.normal.textColor = Color.white;
            }

            if (_timing.BestLapTime < float.MaxValue)
            {
                _style.normal.textColor = Color.green;
                GUI.Label(new Rect(x, y + 72f, 310f, 36f),
                    $"MELHOR: {FormatTime(_timing.BestLapTime)}", _style);
                _style.normal.textColor = Color.white;
            }

            GUI.Label(new Rect(x, y + 108f, 310f, 36f),
                $"VOLTAS: {_timing.LapsCompleted}", _style);
        }

        private static string FormatTime(float seconds)
        {
            if (seconds <= 0f || seconds >= 600f)
            {
                return "--:--.---";
            }

            var minutes = (int)(seconds / 60f);
            var secs = seconds - minutes * 60f;
            return $"{minutes}:{secs:00.000}";
        }
    }
}
