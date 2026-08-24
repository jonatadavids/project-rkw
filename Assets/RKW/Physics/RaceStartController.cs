using System.Collections.Generic;
using UnityEngine;

namespace RKW.Physics
{
    /// <summary>
    /// Simple race-start ritual (founder playtest feedback, 2026-08-19:
    /// "largada 3 2 1 já com as bandeiras"). Holds player and every bot's
    /// input during a 3-2-1 countdown with a checkered starter bar, then
    /// shows a green "VAI!" flash and releases control. Deliberately
    /// minimal — not a full flag/penalty system (that belongs to a later
    /// milestone).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RaceStartController : MonoBehaviour
    {
        private const int StartCount = 3;
        private const float SecondsPerCount = 1f;
        private const float GoDisplaySeconds = 1f;

        private KartPrototypeInput _playerInput;
        private readonly List<KartBotController> _botControllers = new List<KartBotController>();
        private float _elapsed;
        private bool _released;
        private Texture2D _checkerTexture;
        private GUIStyle _countdownStyle;

        /// <summary>Founder playtest feedback, 2026-08-19: "até 10 bots" — holds however many bots the race was configured with.</summary>
        public void Configure(KartPrototypeInput playerInput, IEnumerable<KartBotController> botControllers)
        {
            _playerInput = playerInput;
            _botControllers.Clear();
            if (botControllers != null)
            {
                _botControllers.AddRange(botControllers);
            }
            SetInputEnabled(false);
        }

        private void Update()
        {
            // Founder playtest feedback, 2026-08-20: "quando comeca 1 2 3
            // escreve VAI e fica na tela" — this early-returned as soon as
            // _released flipped true, which froze _elapsed forever, which
            // meant the "showingGo" window in OnGUI (elapsed < countdown +
            // GoDisplaySeconds) could never become false. Keep advancing
            // _elapsed after release, then remove this component entirely
            // once the "VAI!" window has had its time on screen.
            _elapsed += Time.deltaTime;

            if (!_released && _elapsed >= StartCount * SecondsPerCount)
            {
                SetInputEnabled(true);
                _released = true;
            }

            if (_released && _elapsed >= StartCount * SecondsPerCount + GoDisplaySeconds)
            {
                Destroy(gameObject);
            }
        }

        private void SetInputEnabled(bool inputEnabled)
        {
            if (_playerInput != null)
            {
                _playerInput.SetInputEnabled(inputEnabled);
            }

            foreach (var bot in _botControllers)
            {
                if (bot != null)
                {
                    bot.SetInputEnabled(inputEnabled);
                }
            }
        }

        private int CurrentCountNumber()
        {
            var remaining = StartCount * SecondsPerCount - _elapsed;
            return Mathf.Clamp(Mathf.CeilToInt(remaining), 1, StartCount);
        }

        private void OnGUI()
        {
            var totalCountdownSeconds = StartCount * SecondsPerCount;
            var showingGo = _released && _elapsed < totalCountdownSeconds + GoDisplaySeconds;
            if (!showingGo && _released)
            {
                return;
            }

            if (_checkerTexture == null)
            {
                _checkerTexture = ProceduralUITextures.CreateCheckerTexture(24, 2);
            }

            if (_countdownStyle == null)
            {
                _countdownStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold
                };
            }

            var scale = Mathf.Max(1f, Screen.height / 720f);

            var barHeight = 46f * scale;
            var previousColor = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, barHeight), _checkerTexture, ScaleMode.StretchToFill);
            GUI.color = previousColor;

            _countdownStyle.fontSize = Mathf.RoundToInt(110f * scale);
            _countdownStyle.normal.textColor = _released ? new Color(0.25f, 0.85f, 0.25f) : Color.white;

            var label = _released ? "VAI!" : CurrentCountNumber().ToString();
            var labelRect = new Rect(0f, Screen.height * 0.30f, Screen.width, 170f * scale);
            GUI.Label(labelRect, label, _countdownStyle);
        }

        private void OnDestroy()
        {
            if (_checkerTexture != null)
            {
                Destroy(_checkerTexture);
            }
        }
    }
}
