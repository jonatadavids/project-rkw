using System;
using UnityEngine;

namespace RKW.Physics
{
    /// <summary>
    /// Rodada 46 (2026-09-01), terceira passada -- founder feedback: "a
    /// tela de menu foi uma bem antiga... queria aquela tela mesmo com
    /// aquelas cores, aquele layout... eu mandei o arquivo certo, só pra
    /// vc adicionar as ações nos botões e ler o que precisar". Two earlier
    /// attempts fell short of this: round 44's version used plain
    /// GUI.skin.button chrome, and round 45's version only approximated
    /// the mockup's colors/row layout loosely. This rewrite instead reads
    /// the ACTUAL approved file directly (project doc "claude/menu-principal-v1.html",
    /// the "1c Telemetria" identity finalized from "KartGrid UI.dc.html")
    /// and reproduces its exact structure at design resolution 2340x1080
    /// landscape: same two-column layout, same header, same row heights/
    /// order/colors, same amber accent treatment on JOGAR -- scaled
    /// uniformly by Screen.width / DesignWidth instead of Screen.height,
    /// since this mockup was designed at a specific WIDTH, not the
    /// Screen.height/720 convention the in-race HUD elements use (this is
    /// a separate full-screen menu, not overlaid on gameplay, so it does
    /// not need to share that convention).
    ///
    /// IMPORTANT — what could NOT be copied as-is, and why (read this
    /// before assuming a number here matches a stat you'll actually see):
    /// the mockup shows a full player-progression economy that does not
    /// exist in the game yet -- coins/diamonds currency, "NÍVEL 14",
    /// "LICENÇA RENTAL SPORT", "LIMPEZA 86", a numeric ranking ("#0342"),
    /// and a season pass ("PASSE DE TEMPORADA 12/30"). None of that is
    /// implemented (no economy, no player level, no online ranking, no
    /// season pass -- see the project's own MVP scope). Rather than fake
    /// those numbers, this fills the same visual slots with REAL data
    /// already available in this build (best lap ever recorded, current
    /// kart category, current track choice, the player's actual saved
    /// name) and leaves the currency chips and season-pass box out
    /// entirely instead of inventing fake progress. "sala privada" in
    /// JOGAR's subtitle (a private-room multiplayer feature that does not
    /// exist) was likewise swapped for "escolha de pista" (what JOGAR
    /// actually does today). Also: this uses Unity's default built-in
    /// font, not the mockup's Chakra Petch/IBM Plex Mono webfonts --
    /// importing real custom font assets is Editor/asset-import work this
    /// remote-only session cannot safely do blind.
    ///
    /// Rodada 46 (2026-09-01), passada seguinte -- founder request: "ele
    /// falar o melhor tempo nas 3 categorias 1 3 5 e a volta mais rapida
    /// ... rank dos melhores tempos, isso tudo no menu inicial". None of
    /// this exists in the approved mockup (it only reserves the kart-
    /// render placeholder below the top stats), so
    /// <see cref="DrawRaceTimesRow"/> and <see cref="DrawRankingList"/>
    /// add two new compact panels in the LEFT column's previously-empty
    /// space between that placeholder and the footer, rather than
    /// crowding or resizing anything the mockup already specifies. Two
    /// more honesty notes: (1) "melhor tempo nas 3 categorias 1 3 5" reads
    /// as best TOTAL RACE time per lap count -- a new RaceRecordStore
    /// history, separate from the per-lap LapRecordStore already behind
    /// MELHOR VOLTA above, since a 1-lap total and a 5-lap total are not
    /// comparable; (2) both new panels intentionally do NOT filter by
    /// track/kart-category, same as the existing MELHOR VOLTA stat right
    /// above them (see that stat's own call to
    /// LapRecordMath.FindBestLapTimeSeconds) -- this is the simplest
    /// reading of "menu inicial" (a single always-visible summary, before
    /// the player has necessarily picked which track/kart they will race
    /// next) and keeps this first version consistent with what was
    /// already on screen, rather than introducing new track/category
    /// scoping logic no other part of this menu uses yet. "rank dos
    /// melhores tempos" reuses the existing per-LAP leaderboard (labeled
    /// "RANKING - MELHORES VOLTAS") instead of ranking full races, because
    /// mixing 1/3/5-lap totals in one ranked list would be dominated
    /// entirely by 1-lap races and would not read as a meaningful ranking.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MainMenu : MonoBehaviour
    {
        private const float ToastDurationSeconds = 2.2f;

        // Design resolution the approved mockup was built at (see class
        // doc) -- every layout number below is in these units, then
        // multiplied by DesignScale in OnGUI.
        private const float DesignWidth = 2340f;

        private static readonly Color BackgroundColor = new Color(0.063f, 0.055f, 0.047f, 1f); // #100e0c
        private static readonly Color PanelColor = new Color(0.09f, 0.078f, 0.06f, 1f); // #17140f
        private static readonly Color AmberColor = new Color(0.851f, 0.588f, 0.157f); // #d99628
        private static readonly Color InkColor = new Color(0.925f, 0.906f, 0.875f); // #ece7df
        private static readonly Color InkDimColor = new Color(0.561f, 0.529f, 0.486f); // #8f877c
        private static readonly Color InkFaintColor = new Color(0.361f, 0.333f, 0.298f); // #5c554c
        private static readonly Color DividerColor = new Color(1f, 1f, 1f, 0.12f);
        private static readonly Color DividerFaintColor = new Color(1f, 1f, 1f, 0.1f);
        private static readonly Color HighlightGradientColor = new Color(0.851f, 0.588f, 0.157f, 0.18f);

        private Action _onPlay;
        private Action _onSettings;
        private bool _confirmed;
        private string _toastMessage;
        private float _toastShownUntil = -1f;
        private Texture2D _solidTexture;

        private GUIStyle _wordmarkStyle;
        private GUIStyle _taglineStyle;
        private GUIStyle _nameStyle;
        private GUIStyle _subtitleStyle;
        private GUIStyle _avatarInitialsStyle;
        private GUIStyle _statLabelStyle;
        private GUIStyle _statValueStyle;
        private GUIStyle _placeholderStyle;
        private GUIStyle _rowIndexStyle;
        private GUIStyle _primaryRowLabelStyle;
        private GUIStyle _primaryRowSublabelStyle;
        private GUIStyle _rowLabelStyle;
        private GUIStyle _rowTrailingStyle;
        private GUIStyle _arrowStyle;
        private GUIStyle _footerStyle;
        private GUIStyle _toastStyle;
        // Rodada 46, passada seguinte: styles for the two new compact
        // panels described in this class's own doc comment above.
        private GUIStyle _miniSectionTitleStyle;
        private GUIStyle _miniListRowStyle;

        public void Configure(Action onPlay, Action onSettings)
        {
            _onPlay = onPlay;
            _onSettings = onSettings;
        }

        private void EnsureStyles(float scale)
        {
            if (_wordmarkStyle != null)
            {
                return;
            }

            _solidTexture = Texture2D.whiteTexture;

            _wordmarkStyle = MakeStyle(96f * scale, FontStyle.Bold, TextAnchor.UpperLeft, InkColor);
            _taglineStyle = MakeStyle(20f * scale, FontStyle.Normal, TextAnchor.UpperLeft, InkDimColor);
            _nameStyle = MakeStyle(38f * scale, FontStyle.Bold, TextAnchor.UpperLeft, InkColor);
            _subtitleStyle = MakeStyle(19f * scale, FontStyle.Normal, TextAnchor.UpperLeft, InkDimColor);
            _avatarInitialsStyle = MakeStyle(28f * scale, FontStyle.Bold, TextAnchor.MiddleCenter, AmberColor);
            _statLabelStyle = MakeStyle(17f * scale, FontStyle.Normal, TextAnchor.UpperLeft, InkDimColor);
            _statValueStyle = MakeStyle(44f * scale, FontStyle.Bold, TextAnchor.UpperLeft, InkColor);
            _placeholderStyle = MakeStyle(20f * scale, FontStyle.Normal, TextAnchor.LowerLeft, InkFaintColor);
            _rowIndexStyle = MakeStyle(18f * scale, FontStyle.Normal, TextAnchor.MiddleLeft, InkFaintColor);
            _primaryRowLabelStyle = MakeStyle(56f * scale, FontStyle.Bold, TextAnchor.UpperLeft, InkColor);
            _primaryRowSublabelStyle = MakeStyle(19f * scale, FontStyle.Normal, TextAnchor.UpperLeft, InkDimColor);
            _rowLabelStyle = MakeStyle(38f * scale, FontStyle.Bold, TextAnchor.MiddleLeft, InkColor);
            _rowTrailingStyle = MakeStyle(19f * scale, FontStyle.Normal, TextAnchor.MiddleRight, InkDimColor);
            _arrowStyle = MakeStyle(40f * scale, FontStyle.Bold, TextAnchor.MiddleCenter, AmberColor);
            _footerStyle = MakeStyle(18f * scale, FontStyle.Normal, TextAnchor.LowerLeft, InkFaintColor);
            _toastStyle = MakeStyle(22f * scale, FontStyle.Bold, TextAnchor.MiddleCenter, AmberColor);
            _miniSectionTitleStyle = MakeStyle(17f * scale, FontStyle.Bold, TextAnchor.UpperLeft, InkDimColor);
            _miniListRowStyle = MakeStyle(19f * scale, FontStyle.Normal, TextAnchor.UpperLeft, InkColor);
        }

        private static GUIStyle MakeStyle(float fontSize, FontStyle fontStyle, TextAnchor anchor, Color color)
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(fontSize),
                fontStyle = fontStyle,
                alignment = anchor,
                wordWrap = false
            };
            style.normal.textColor = color;
            return style;
        }

        private void OnGUI()
        {
            if (_confirmed)
            {
                return;
            }

            var scale = Screen.width / DesignWidth;
            EnsureStyles(scale);

            DrawRect(new Rect(0f, 0f, Screen.width, Screen.height), BackgroundColor);

            DrawHeader(scale);
            DrawLeftColumn(scale);
            DrawRightColumn(scale);
            DrawFooter(scale);
            DrawToastIfActive(scale);
        }

        private void DrawHeader(float scale)
        {
            const float PadX = 56f;
            const float AvatarY = 34f;
            const float AvatarSize = 72f;
            const float HeaderHeight = 132f;

            var avatarRect = R(PadX, AvatarY, AvatarSize, AvatarSize, scale);
            DrawRect(avatarRect, PanelColor);
            GUI.Label(avatarRect, PlayerInitials(), _avatarInitialsStyle);

            var textX = PadX + AvatarSize + 24f;
            GUI.Label(R(textX, 38f, 900f, 44f, scale), PlayerNameStore.GetName(), _nameStyle);

            // Round 46: the mockup's subtitle line here is "NÍVEL 14 ·
            // LICENÇA RENTAL SPORT · LIMPEZA 86" -- a full progression
            // system (level, license tier, driving-cleanliness score) that
            // does not exist in this game. Reused the same slot for real,
            // available data instead: the kart/track the player is
            // currently set up to race, from RacePreferencesStore.
            var kartLabel = RacePreferencesStore.PreferredUsesKartV2 ? "KART 18 HP" : "KART 13 HP";
            var trackLabel = RacePreferencesStore.PreferredUseTechnicalCircuit2 ? "CIRCUITO 2" : "CIRCUITO OVAL";
            GUI.Label(R(textX, 80f, 900f, 30f, scale), $"{kartLabel}  ·  {trackLabel}", _subtitleStyle);

            // Round 46: the mockup's top-right coin/diamond currency chips
            // are intentionally omitted -- there is no cosmetic-item
            // economy implemented yet (see project MVP scope), and showing
            // fake currency numbers would be misleading.

            DrawRect(R(0f, HeaderHeight, DesignWidth, 1f, scale), DividerColor);
        }

        private void DrawLeftColumn(float scale)
        {
            const float BodyTop = 172f;
            const float PadX = 56f;
            const float LeftWidth = 1200f;

            GUI.Label(R(PadX, BodyTop, LeftWidth, 110f, scale), "KARTGRID", _wordmarkStyle);
            GUI.Label(R(PadX, BodyTop + 108f, LeftWidth, 30f, scale), "KART RENTAL  ·  CRONOMETRAGEM REAL", _taglineStyle);

            var statsY = BodyTop + 168f;
            var statWidth = (LeftWidth - 40f) / 3f;

            var bestLapSeconds = LapRecordMath.FindBestLapTimeSeconds(
                LapRecordStore.LoadHistory(), DateTimeOffset.UtcNow.ToUnixTimeSeconds(), -1);
            DrawStat(PadX, statsY, statWidth, scale, "MELHOR VOLTA",
                bestLapSeconds.HasValue ? FormatLapTime(bestLapSeconds.Value) : "--", accent: true);

            var categoryLabel = RacePreferencesStore.PreferredUsesKartV2 ? "18 HP" : "13 HP";
            DrawStat(PadX + statWidth + 20f, statsY, statWidth, scale, "CATEGORIA", categoryLabel, accent: false);

            var trackLabel = RacePreferencesStore.PreferredUseTechnicalCircuit2 ? "CIRCUITO 2" : "CIRCUITO OVAL";
            DrawStat(PadX + (statWidth + 20f) * 2f, statsY, statWidth, scale, "PISTA", trackLabel, accent: false);

            // Round 46: this frame is a PLACEHOLDER in the approved mockup
            // itself ("Não sei desenhar o kart — marquei os slots" -- from
            // the original design-exploration doc), reserved for a future
            // 3D render of the player's own kart. Kept as a labeled
            // placeholder rather than guessed at.
            var placeholderY = statsY + 110f;
            var placeholderHeight = 300f;
            var placeholderRect = R(PadX, placeholderY, LeftWidth, placeholderHeight, scale);
            DrawRect(placeholderRect, PanelColor);
            DrawBorder(placeholderRect, DividerColor, 2f * scale);
            var placeholderTextRect = new Rect(
                placeholderRect.x + 24f * scale, placeholderRect.y,
                placeholderRect.width - 48f * scale, placeholderRect.height - 24f * scale);
            GUI.Label(placeholderTextRect, "[ PLACEHOLDER ]\nkart + piloto do jogador", _placeholderStyle);

            // Rodada 46, passada seguinte -- see this class's own doc
            // comment for why these two panels live here (previously-
            // empty space below the mockup's own placeholder) instead of
            // altering anything the approved mockup already specifies.
            var raceTimesY = placeholderY + placeholderHeight + 16f;
            DrawRaceTimesRow(PadX, raceTimesY, LeftWidth, scale);

            var rankingY = raceTimesY + 94f + 18f;
            DrawRankingList(PadX, rankingY, LeftWidth, scale);
        }

        private void DrawRaceTimesRow(float x, float y, float width, float scale)
        {
            GUI.Label(R(x, y, width, 22f, scale), "MELHOR TEMPO DE CORRIDA", _miniSectionTitleStyle);

            var rowY = y + 24f;
            var statWidth = (width - 40f) / 3f;
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var history = RaceRecordStore.LoadHistory();

            DrawRaceTimeStat(x, rowY, statWidth, scale, "1 VOLTA", history, laps: 1, now: now);
            DrawRaceTimeStat(x + statWidth + 20f, rowY, statWidth, scale, "3 VOLTAS", history, laps: 3, now: now);
            DrawRaceTimeStat(x + (statWidth + 20f) * 2f, rowY, statWidth, scale, "5 VOLTAS", history, laps: 5, now: now);
        }

        private void DrawRaceTimeStat(float x, float y, float width, float scale, string label,
            RaceRecord[] history, int laps, long now)
        {
            var best = RaceRecordMath.FindBestRaceTimeSeconds(history, laps, now, maxAgeSecondsOrNegativeForAllTime: -1);
            DrawStat(x, y, width, scale, label, best.HasValue ? FormatLapTime(best.Value) : "--", accent: false);
        }

        private void DrawRankingList(float x, float y, float width, float scale)
        {
            const int RowCount = 5;

            GUI.Label(R(x, y, width, 22f, scale), "RANKING - MELHORES VOLTAS", _miniSectionTitleStyle);

            var history = LapRecordStore.LoadHistory();
            var top = LapRecordMath.FindTopRecords(history, RowCount);

            var rowY = y + 26f;
            const float RowHeight = 24f;
            for (var i = 0; i < RowCount; i++)
            {
                var rowRect = R(x, rowY, width, RowHeight, scale);
                if (i < top.Count)
                {
                    var record = top[i];
                    var nameLabel = string.IsNullOrEmpty(record.PlayerName) ? "Piloto" : record.PlayerName;
                    GUI.Label(rowRect, $"{i + 1}. {nameLabel}", _miniListRowStyle);
                    var timeRect = new Rect(rowRect.x, rowRect.y, rowRect.width, rowRect.height);
                    var timeStyle = new GUIStyle(_miniListRowStyle) { alignment = TextAnchor.UpperRight };
                    GUI.Label(timeRect, FormatLapTime(record.LapTimeSeconds), timeStyle);
                }
                else
                {
                    GUI.Label(rowRect, $"{i + 1}. --", _miniListRowStyle);
                }

                rowY += RowHeight;
            }
        }

        private void DrawStat(float x, float y, float width, float scale, string label, string value, bool accent)
        {
            DrawRect(R(x, y, width, 2f, scale), accent ? AmberColor : DividerColor);
            GUI.Label(R(x, y + 16f, width, 24f, scale), label, _statLabelStyle);
            GUI.Label(R(x, y + 42f, width, 52f, scale), value, _statValueStyle);
        }

        private void DrawRightColumn(float scale)
        {
            const float BodyTop = 172f;
            const float PadX = 56f;
            const float LeftWidth = 1200f;
            const float Gap = 48f;
            const float RightX = PadX + LeftWidth + Gap;

            DrawRect(R(RightX - Gap * 0.5f, BodyTop, 1f, 864f, scale), DividerColor);

            var rowX = RightX;
            var rowWidth = DesignWidth - RightX - PadX;

            var y = BodyTop;
            DrawPrimaryRow(rowX, y, rowWidth, scale, "01", "JOGAR", "partida rápida · escolha de pista", Confirm);
            y += 180f;

            y = DrawPlaceholderRow(rowX, y, rowWidth, scale, "02", "ESCOLA DE PILOTAGEM");
            y = DrawPlaceholderRow(rowX, y, rowWidth, scale, "03", "GARAGEM");
            y = DrawPlaceholderRow(rowX, y, rowWidth, scale, "04", "LOJA");

            // Round 46: real, functioning screen (kart/laps/bots/
            // dificuldade, salva um padrão) -- see SettingsMenu.
            DrawMenuRow(rowX, y, rowWidth, 112f, scale, "05", "CONFIGURAÇÕES", null, drawBottomBorder: false,
                onClick: OpenSettings);

            // Round 46: the mockup's "PASSE DE TEMPORADA 12/30" box at the
            // bottom of this column is intentionally left out -- there is
            // no season pass system implemented, and a fake progress bar
            // would misrepresent actual progress that does not exist.
        }

        private void DrawPrimaryRow(float x, float y, float width, float scale, string index, string label,
            string sublabel, Action onClick)
        {
            const float Height = 168f;
            var rect = R(x, y, width, Height, scale);

            DrawRect(rect, HighlightGradientColor);
            DrawRect(new Rect(rect.x, rect.y, 6f * scale, rect.height), AmberColor);

            var contentX = rect.x + 30f * scale;
            GUI.Label(new Rect(contentX, rect.y, 40f * scale, rect.height), index, _rowIndexStyle);

            var labelX = contentX + 46f * scale;
            GUI.Label(new Rect(labelX, rect.y + rect.height * 0.5f - 40f * scale, rect.width - 200f * scale, 60f * scale),
                label, _primaryRowLabelStyle);
            GUI.Label(new Rect(labelX, rect.y + rect.height * 0.5f + 14f * scale, rect.width - 200f * scale, 30f * scale),
                sublabel, _primaryRowSublabelStyle);

            GUI.Label(new Rect(rect.x + rect.width - 80f * scale, rect.y, 60f * scale, rect.height), "›", _arrowStyle);

            if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
            {
                onClick?.Invoke();
            }
        }

        private float DrawPlaceholderRow(float x, float y, float width, float scale, string index, string label)
        {
            const float Height = 112f;
            DrawMenuRow(x, y, width, Height, scale, index, label, null, drawBottomBorder: true,
                onClick: () => ShowToast($"{label} — em construção"));
            return y + Height;
        }

        private void DrawMenuRow(float x, float y, float width, float height, float scale, string index, string label,
            string trailing, bool drawBottomBorder, Action onClick)
        {
            var rect = R(x, y, width, height, scale);
            var contentX = rect.x + 30f * scale;

            GUI.Label(new Rect(contentX, rect.y, 40f * scale, rect.height), index, _rowIndexStyle);
            GUI.Label(new Rect(contentX + 46f * scale, rect.y, rect.width - 200f * scale, rect.height), label, _rowLabelStyle);

            if (!string.IsNullOrEmpty(trailing))
            {
                GUI.Label(new Rect(rect.x + rect.width - 160f * scale, rect.y, 140f * scale, rect.height), trailing,
                    _rowTrailingStyle);
            }

            if (drawBottomBorder)
            {
                DrawRect(new Rect(rect.x, rect.y + rect.height - 1f, rect.width, 1f), DividerFaintColor);
            }

            if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
            {
                onClick?.Invoke();
            }
        }

        private void DrawFooter(float scale)
        {
            GUI.Label(R(56f, 1040f, 800f, 30f, scale), "KARTGRID • PROTÓTIPO DEV", _footerStyle);
        }

        private void DrawBorder(Rect rect, Color color, float thickness)
        {
            DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            DrawRect(new Rect(rect.x, rect.y + rect.height - thickness, rect.width, thickness), color);
            DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            DrawRect(new Rect(rect.x + rect.width - thickness, rect.y, thickness, rect.height), color);
        }

        private static Rect R(float designX, float designY, float designWidth, float designHeight, float scale)
        {
            return new Rect(designX * scale, designY * scale, designWidth * scale, designHeight * scale);
        }

        private void DrawRect(Rect rect, Color color)
        {
            var previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, _solidTexture);
            GUI.color = previousColor;
        }

        private static string PlayerInitials()
        {
            var name = PlayerNameStore.GetName();
            if (string.IsNullOrWhiteSpace(name))
            {
                return "?";
            }

            var parts = name.Trim().Split(' ');
            if (parts.Length >= 2 && parts[0].Length > 0 && parts[1].Length > 0)
            {
                return $"{char.ToUpperInvariant(parts[0][0])}{char.ToUpperInvariant(parts[1][0])}";
            }

            return parts[0].Length >= 2
                ? parts[0].Substring(0, 2).ToUpperInvariant()
                : parts[0].Substring(0, 1).ToUpperInvariant();
        }

        private static string FormatLapTime(float seconds)
        {
            var minutes = (int)(seconds / 60f);
            var secs = seconds - minutes * 60f;
            return minutes > 0 ? $"{minutes}:{secs:00.000}" : $"{secs:0.000}";
        }

        private void ShowToast(string message)
        {
            _toastMessage = message;
            _toastShownUntil = Time.unscaledTime + ToastDurationSeconds;
        }

        private void DrawToastIfActive(float scale)
        {
            if (_toastShownUntil < 0f || Time.unscaledTime > _toastShownUntil)
            {
                return;
            }

            var toastRect = new Rect(0f, Screen.height * 0.9f, Screen.width, 50f * scale);
            GUI.Label(toastRect, _toastMessage, _toastStyle);
        }

        private void OpenSettings()
        {
            _confirmed = true;
            // Round 45: MainMenu <-> SettingsMenu can be visited back and
            // forth repeatedly, so this object is destroyed rather than
            // just hidden -- otherwise every detour into CONFIGURAÇÕES
            // would leave a stale, invisible MainMenu GameObject behind.
            Destroy(gameObject);
            _onSettings?.Invoke();
        }

        private void Confirm()
        {
            _confirmed = true;
            Destroy(gameObject);
            _onPlay?.Invoke();
        }
    }
}
