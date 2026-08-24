using System;
using UnityEngine;

namespace RKW.Physics
{
    /// <summary>
    /// Pre-race configuration screen (founder playtest feedback,
    /// 2026-08-19: "se for tranquilo escolher entre 1, 3 e 5 voltas e até
    /// 10 bots e o nível deles a gente já começa a ganhar"). A plain OnGUI
    /// button menu — not production UI, just enough to let a playtester
    /// pick before the countdown starts. The track's grid only has 10
    /// marked positions total (1 for the player, so bot count is capped at
    /// <see cref="MaxBotCount"/> = 9).
    ///
    /// Round 25 (2026-08-24) founder feedback: "deveria ter a opcao de bot
    /// ou sozinho ou os 2 pode ser uma flag" — before this, "solo" only
    /// existed implicitly by stepping the bot count down to 0, which was
    /// easy to miss. <see cref="RaceMode"/> makes it an explicit two-state
    /// flag (SOZINHO / COM BOTS) instead: SOZINHO forces the bot count to
    /// 0 and swaps the stepper for a plain label; COM BOTS restores
    /// whichever non-zero count was last selected (or 3 the first time)
    /// and re-enables the stepper, clamped to a minimum of 1 there since 0
    /// bots in "COM BOTS" mode would just be a confusing way to spell
    /// SOZINHO. The ghost (round 25 full-race redesign) always races
    /// regardless of this flag — it's the "you vs. your own best" mode
    /// Jonathan already confirmed is "normal e muito bom", independent of
    /// whether AI bots are also on the grid.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RaceSetupMenu : MonoBehaviour
    {
        public const int MaxBotCount = 9;

        private enum RaceMode
        {
            Solo,
            WithBots
        }

        private int _selectedLaps = 3;
        private int _selectedBotCount = 1;
        private RaceMode _selectedMode = RaceMode.WithBots;
        private int _lastNonZeroBotCount = 3;
        private BotDifficulty _selectedDifficulty = BotDifficulty.Medium;
        private Action<int, int, BotDifficulty> _onConfirm;
        private bool _confirmed;

        private GUIStyle _titleStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _selectedButtonStyle;

        // Founder playtest feedback, 2026-08-20 (round 8): "nem pra digitar
        // o meu [nome] pra ficar gravado a melhor volta nominalmente" — a
        // name typed here (via the OS on-screen keyboard) is what tags
        // recorded laps in LapRecordStore and labels the player's row in
        // the live standings HUD.
        private string _playerName;
        private TouchScreenKeyboard _keyboard;

        public void Configure(Action<int, int, BotDifficulty> onConfirm)
        {
            _onConfirm = onConfirm;
        }

        private void Awake()
        {
            _playerName = PlayerNameStore.GetName();
        }

        private void Update()
        {
            if (_keyboard == null)
            {
                return;
            }

            if (_keyboard.status == TouchScreenKeyboard.Status.Done)
            {
                if (!string.IsNullOrWhiteSpace(_keyboard.text))
                {
                    PlayerNameStore.SetName(_keyboard.text);
                    _playerName = PlayerNameStore.GetName();
                }
                _keyboard = null;
            }
            else if (_keyboard.status == TouchScreenKeyboard.Status.Canceled
                || _keyboard.status == TouchScreenKeyboard.Status.LostFocus)
            {
                _keyboard = null;
            }
        }

        private void EnsureStyles()
        {
            if (_titleStyle != null)
            {
                return;
            }

            var scale = Mathf.Max(1f, Screen.height / 720f);

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(28f * scale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            _titleStyle.normal.textColor = Color.white;

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(18f * scale),
                alignment = TextAnchor.MiddleLeft
            };
            _labelStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f);

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = Mathf.RoundToInt(20f * scale)
            };

            _selectedButtonStyle = new GUIStyle(_buttonStyle);
            _selectedButtonStyle.normal.textColor = new Color(0.3f, 0.95f, 0.3f);
            _selectedButtonStyle.fontStyle = FontStyle.Bold;
        }

        private void OnGUI()
        {
            if (_confirmed)
            {
                return;
            }

            EnsureStyles();
            var scale = Mathf.Max(1f, Screen.height / 720f);
            var panelWidth = 520f * scale;
            var panelX = (Screen.width - panelWidth) * 0.5f;
            var y = Screen.height * 0.1f;

            var previousColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.72f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previousColor;

            GUI.Label(new Rect(panelX, y, panelWidth, 50f * scale), "CONFIGURAR CORRIDA", _titleStyle);
            y += 58f * scale;

            // Player name
            GUI.Label(new Rect(panelX, y, panelWidth, 30f * scale), "SEU NOME", _labelStyle);
            y += 34f * scale;
            var nameRect = new Rect(panelX, y, panelWidth, 44f * scale);
            if (GUI.Button(nameRect, $"{_playerName}  (toque para editar)", _buttonStyle))
            {
                // Founder playtest feedback, 2026-08-20 (round 9): "quando
                // coloquei o nome do Piloto a primeira vez parecia que
                // tinha a palavra Pilot na frente... ideal ficar sem
                // conteúdo nenhum" — this used to pre-fill the keyboard
                // with the current name (the placeholder "Piloto", or
                // whatever was typed before), which read as text already
                // typed in the box. Always opens blank now; leaving it
                // blank and confirming just keeps whatever name was
                // already saved (see Update() below).
                _keyboard = TouchScreenKeyboard.Open(string.Empty, TouchScreenKeyboardType.Default,
                    false, false, false, false, string.Empty, 18);
            }
            y += 54f * scale;

            // Laps
            GUI.Label(new Rect(panelX, y, panelWidth, 30f * scale), "VOLTAS", _labelStyle);
            y += 34f * scale;
            DrawLapButton(panelX, y, panelWidth, scale, 0, 1);
            DrawLapButton(panelX, y, panelWidth, scale, 1, 3);
            DrawLapButton(panelX, y, panelWidth, scale, 2, 5);
            y += 54f * scale;

            // Round 25: explicit solo/with-bots flag, drawn above the bot
            // count stepper since it now governs whether that stepper is
            // even interactive (see class doc above).
            GUI.Label(new Rect(panelX, y, panelWidth, 30f * scale), "MODO", _labelStyle);
            y += 34f * scale;
            DrawModeButton(panelX, y, panelWidth, scale, 0, RaceMode.Solo, "SOZINHO");
            DrawModeButton(panelX, y, panelWidth, scale, 1, RaceMode.WithBots, "COM BOTS");
            y += 54f * scale;

            // Bots (stepper) — only interactive in RaceMode.WithBots; in
            // RaceMode.Solo the count is forced to 0 by SelectMode and this
            // just shows a plain label instead of +/- buttons, so it is
            // obvious there is nothing to adjust.
            GUI.Label(new Rect(panelX, y, panelWidth, 30f * scale), "BOTS", _labelStyle);
            y += 34f * scale;
            if (_selectedMode == RaceMode.Solo)
            {
                var soloRect = new Rect(panelX, y, panelWidth, 44f * scale);
                GUI.Label(soloRect, "0 — corrida sozinho, só você e o fantasma", _labelStyle);
            }
            else
            {
                var minusRect = new Rect(panelX, y, 60f * scale, 44f * scale);
                var valueRect = new Rect(panelX + 66f * scale, y, panelWidth - 132f * scale, 44f * scale);
                var plusRect = new Rect(panelX + panelWidth - 60f * scale, y, 60f * scale, 44f * scale);
                if (GUI.Button(minusRect, "-", _buttonStyle))
                {
                    _selectedBotCount = Mathf.Max(1, _selectedBotCount - 1);
                    _lastNonZeroBotCount = _selectedBotCount;
                }
                GUI.Label(valueRect, _selectedBotCount.ToString(), _titleStyle);
                if (GUI.Button(plusRect, "+", _buttonStyle))
                {
                    _selectedBotCount = Mathf.Min(MaxBotCount, _selectedBotCount + 1);
                    _lastNonZeroBotCount = _selectedBotCount;
                }
            }
            y += 54f * scale;

            // Difficulty
            GUI.Label(new Rect(panelX, y, panelWidth, 30f * scale), "NÍVEL DOS BOTS", _labelStyle);
            y += 34f * scale;
            DrawDifficultyButton(panelX, y, panelWidth, scale, 0, BotDifficulty.Easy, "FÁCIL");
            DrawDifficultyButton(panelX, y, panelWidth, scale, 1, BotDifficulty.Medium, "MÉDIO");
            DrawDifficultyButton(panelX, y, panelWidth, scale, 2, BotDifficulty.Hard, "DIFÍCIL");
            y += 54f * scale;

            y += 16f * scale;
            var confirmRect = new Rect(panelX, y, panelWidth, 56f * scale);
            if (GUI.Button(confirmRect, "COMEÇAR CORRIDA", _buttonStyle))
            {
                _confirmed = true;
                _onConfirm?.Invoke(_selectedLaps, _selectedBotCount, _selectedDifficulty);
            }
        }

        private void DrawLapButton(float panelX, float y, float panelWidth, float scale, int slot, int laps)
        {
            var buttonWidth = panelWidth / 3f;
            var rect = new Rect(panelX + slot * buttonWidth, y, buttonWidth - 6f * scale, 44f * scale);
            var style = _selectedLaps == laps ? _selectedButtonStyle : _buttonStyle;
            if (GUI.Button(rect, laps.ToString(), style))
            {
                _selectedLaps = laps;
            }
        }

        private void DrawModeButton(float panelX, float y, float panelWidth, float scale, int slot,
            RaceMode mode, string label)
        {
            var buttonWidth = panelWidth / 2f;
            var rect = new Rect(panelX + slot * buttonWidth, y, buttonWidth - 6f * scale, 44f * scale);
            var style = _selectedMode == mode ? _selectedButtonStyle : _buttonStyle;
            if (GUI.Button(rect, label, style))
            {
                SelectMode(mode);
            }
        }

        private void SelectMode(RaceMode mode)
        {
            if (_selectedMode == mode)
            {
                return;
            }

            _selectedMode = mode;
            if (mode == RaceMode.Solo)
            {
                if (_selectedBotCount > 0)
                {
                    _lastNonZeroBotCount = _selectedBotCount;
                }
                _selectedBotCount = 0;
            }
            else
            {
                _selectedBotCount = _lastNonZeroBotCount > 0 ? _lastNonZeroBotCount : 1;
            }
        }

        private void DrawDifficultyButton(float panelX, float y, float panelWidth, float scale, int slot,
            BotDifficulty difficulty, string label)
        {
            var buttonWidth = panelWidth / 3f;
            var rect = new Rect(panelX + slot * buttonWidth, y, buttonWidth - 6f * scale, 44f * scale);
            var style = _selectedDifficulty == difficulty ? _selectedButtonStyle : _buttonStyle;
            if (GUI.Button(rect, label, style))
            {
                _selectedDifficulty = difficulty;
            }
        }
    }
}
