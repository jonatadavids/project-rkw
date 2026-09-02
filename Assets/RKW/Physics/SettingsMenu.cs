using System;
using UnityEngine;

namespace RKW.Physics
{
    /// <summary>
    /// Round 45 (2026-09-01) founder feedback: "imaginei que em
    /// configuracoes iriam ficar... aquela tela de escolha de bot, escolha
    /// de kart, quantidade de voltas... enfim aquela tela de configuracao
    /// antes do jogo para salvar um padrao ao meu gosto" — reachable from
    /// MainMenu's CONFIGURAÇÕES item. Same fields as the pre-race
    /// <see cref="RaceSetupMenu"/> (laps/mode/bots/difficulty), plus kart
    /// category (which RaceSetupMenu does not offer — that is chosen via
    /// <see cref="KartCategoryToggleButton"/> mid-race today). Unlike
    /// RaceSetupMenu, this does NOT start a race — it only writes to
    /// <see cref="RacePreferencesStore"/> and returns to the main menu.
    /// Once saved, BeginRace applies these as the starting kart/laps/bots/
    /// difficulty for every future race (see
    /// KartPhysicsPrototypeBootstrap.BeginRace and RaceSetupMenu.ApplyDefaults) —
    /// the player can still change anything per-race on RaceSetupMenu
    /// itself, this only changes what it starts pre-filled with.
    ///
    /// Deliberately reuses RaceSetupMenu's plain OnGUI/GUIStyle-scaling
    /// pattern (not MainMenu's custom-styled rows) — this is a
    /// configuration form, not a branded landing screen.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SettingsMenu : MonoBehaviour
    {
        private bool _usesKartV2;
        private int _selectedLaps;
        private bool _solo;
        private int _selectedBotCount;
        private int _lastNonZeroBotCount;
        private BotDifficulty _selectedDifficulty;
        private Action _onClose;
        private bool _closed;

        private GUIStyle _titleStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _selectedButtonStyle;
        private GUIStyle _hintStyle;

        public void Configure(Action onClose)
        {
            _onClose = onClose;
        }

        private void Awake()
        {
            // Starts from whatever is currently saved (or the same
            // defaults RaceSetupMenu itself uses, the first time) so
            // opening this screen shows "what will happen today", not a
            // blank slate.
            _usesKartV2 = RacePreferencesStore.PreferredUsesKartV2;
            _selectedLaps = RacePreferencesStore.PreferredLaps;
            _selectedBotCount = RacePreferencesStore.PreferredBotCount;
            _lastNonZeroBotCount = _selectedBotCount > 0 ? _selectedBotCount : 1;
            _solo = RacePreferencesStore.PreferredSolo;
            _selectedDifficulty = RacePreferencesStore.PreferredDifficulty;
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

            _hintStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(14f * scale),
                alignment = TextAnchor.MiddleCenter
            };
            _hintStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
        }

        private void OnGUI()
        {
            if (_closed)
            {
                return;
            }

            EnsureStyles();
            var scale = Mathf.Max(1f, Screen.height / 720f);
            var panelWidth = 520f * scale;
            var panelX = (Screen.width - panelWidth) * 0.5f;
            var y = Screen.height * 0.06f;

            var previousColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.78f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previousColor;

            GUI.Label(new Rect(panelX, y, panelWidth, 46f * scale), "CONFIGURAÇÕES", _titleStyle);
            y += 52f * scale;
            GUI.Label(new Rect(panelX, y, panelWidth, 24f * scale),
                "Esse padrão é salvo e usado como ponto de partida em toda corrida.", _hintStyle);
            y += 34f * scale;

            // Kart category
            GUI.Label(new Rect(panelX, y, panelWidth, 28f * scale), "KART", _labelStyle);
            y += 32f * scale;
            DrawTwoWayButton(panelX, y, panelWidth, scale, !_usesKartV2, "13 HP · 60 km/h", () => _usesKartV2 = false,
                _usesKartV2, "18 HP · 80 km/h", () => _usesKartV2 = true);
            y += 50f * scale;

            // Laps
            GUI.Label(new Rect(panelX, y, panelWidth, 28f * scale), "VOLTAS", _labelStyle);
            y += 32f * scale;
            DrawLapButton(panelX, y, panelWidth, scale, 0, 1);
            DrawLapButton(panelX, y, panelWidth, scale, 1, 3);
            DrawLapButton(panelX, y, panelWidth, scale, 2, 5);
            y += 50f * scale;

            // Mode
            GUI.Label(new Rect(panelX, y, panelWidth, 28f * scale), "MODO", _labelStyle);
            y += 32f * scale;
            DrawTwoWayButton(panelX, y, panelWidth, scale, _solo, "SOZINHO", () => SelectSolo(true),
                !_solo, "COM BOTS", () => SelectSolo(false));
            y += 50f * scale;

            // Bots
            GUI.Label(new Rect(panelX, y, panelWidth, 28f * scale), "BOTS", _labelStyle);
            y += 32f * scale;
            if (_solo)
            {
                GUI.Label(new Rect(panelX, y, panelWidth, 42f * scale), "0 — corrida sozinho, só você e o fantasma", _labelStyle);
            }
            else
            {
                var minusRect = new Rect(panelX, y, 60f * scale, 42f * scale);
                var valueRect = new Rect(panelX + 66f * scale, y, panelWidth - 132f * scale, 42f * scale);
                var plusRect = new Rect(panelX + panelWidth - 60f * scale, y, 60f * scale, 42f * scale);
                if (GUI.Button(minusRect, "-", _buttonStyle))
                {
                    _selectedBotCount = Mathf.Max(1, _selectedBotCount - 1);
                    _lastNonZeroBotCount = _selectedBotCount;
                }
                GUI.Label(valueRect, _selectedBotCount.ToString(), _titleStyle);
                if (GUI.Button(plusRect, "+", _buttonStyle))
                {
                    _selectedBotCount = Mathf.Min(RaceSetupMenu.MaxBotCount, _selectedBotCount + 1);
                    _lastNonZeroBotCount = _selectedBotCount;
                }
            }
            y += 50f * scale;

            // Difficulty
            GUI.Label(new Rect(panelX, y, panelWidth, 28f * scale), "NÍVEL DOS BOTS", _labelStyle);
            y += 32f * scale;
            DrawDifficultyButton(panelX, y, panelWidth, scale, 0, BotDifficulty.Easy, "FÁCIL");
            DrawDifficultyButton(panelX, y, panelWidth, scale, 1, BotDifficulty.Medium, "MÉDIO");
            DrawDifficultyButton(panelX, y, panelWidth, scale, 2, BotDifficulty.Hard, "DIFÍCIL");
            y += 50f * scale;

            y += 12f * scale;
            var saveWidth = panelWidth * 0.62f;
            var backWidth = panelWidth - saveWidth - 10f * scale;
            var saveRect = new Rect(panelX, y, saveWidth, 54f * scale);
            var backRect = new Rect(panelX + saveWidth + 10f * scale, y, backWidth, 54f * scale);
            if (GUI.Button(saveRect, "SALVAR PADRÃO", _buttonStyle))
            {
                RacePreferencesStore.Save(_usesKartV2, _selectedLaps, _solo, _selectedBotCount, _selectedDifficulty);
                Close();
            }
            if (GUI.Button(backRect, "VOLTAR", _buttonStyle))
            {
                Close();
            }
        }

        private void SelectSolo(bool solo)
        {
            if (_solo == solo)
            {
                return;
            }

            _solo = solo;
            if (solo)
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

        private void DrawLapButton(float panelX, float y, float panelWidth, float scale, int slot, int laps)
        {
            var buttonWidth = panelWidth / 3f;
            var rect = new Rect(panelX + slot * buttonWidth, y, buttonWidth - 6f * scale, 42f * scale);
            var style = _selectedLaps == laps ? _selectedButtonStyle : _buttonStyle;
            if (GUI.Button(rect, laps.ToString(), style))
            {
                _selectedLaps = laps;
            }
        }

        private void DrawDifficultyButton(float panelX, float y, float panelWidth, float scale, int slot,
            BotDifficulty difficulty, string label)
        {
            var buttonWidth = panelWidth / 3f;
            var rect = new Rect(panelX + slot * buttonWidth, y, buttonWidth - 6f * scale, 42f * scale);
            var style = _selectedDifficulty == difficulty ? _selectedButtonStyle : _buttonStyle;
            if (GUI.Button(rect, label, style))
            {
                _selectedDifficulty = difficulty;
            }
        }

        private void DrawTwoWayButton(float panelX, float y, float panelWidth, float scale,
            bool leftSelected, string leftLabel, Action onLeft,
            bool rightSelected, string rightLabel, Action onRight)
        {
            var buttonWidth = panelWidth / 2f;
            var leftRect = new Rect(panelX, y, buttonWidth - 6f * scale, 42f * scale);
            var rightRect = new Rect(panelX + buttonWidth, y, buttonWidth - 6f * scale, 42f * scale);
            if (GUI.Button(leftRect, leftLabel, leftSelected ? _selectedButtonStyle : _buttonStyle))
            {
                onLeft?.Invoke();
            }
            if (GUI.Button(rightRect, rightLabel, rightSelected ? _selectedButtonStyle : _buttonStyle))
            {
                onRight?.Invoke();
            }
        }

        private void Close()
        {
            _closed = true;
            // See MainMenu.OpenSettings's comment -- destroyed rather than
            // just hidden, since this screen can be opened and closed
            // repeatedly within one scene lifetime.
            Destroy(gameObject);
            _onClose?.Invoke();
        }
    }
}
