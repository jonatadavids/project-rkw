using System;
using System.Collections.Generic;
using UnityEngine;

namespace RKW.Physics
{
    /// <summary>
    /// Ends the demo race once the player completes the configured lap
    /// count (founder playtest feedback, 2026-08-19: "seria legal ter fim
    /// de corrida algo de 3 voltas"). Freezes player + bot input and shows
    /// a simple finish overlay with total race time. Deliberately minimal —
    /// no positions/scoring for bots yet (that is M4 territory).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RaceManager : MonoBehaviour
    {
        private TimingManagerLite _timing;
        private int _targetLaps;
        private BotDifficulty _difficulty;
        private KartPrototypeInput _playerInput;
        private readonly List<KartBotController> _bots = new List<KartBotController>();
        private float _raceStartTime;
        private float _finishTime;
        private bool _finished;
        private GUIStyle _style;
        private GUIStyle _buttonStyle;
        private GUIStyle _infoStyle;
        private GUIStyle _leaderboardStyle;
        private GUIStyle _leaderboardTitleStyle;
        private GUIStyle _standingsStyle;
        private GUIStyle _playerStandingsStyle;
        private GUIStyle _standingsTitleStyle;
        private GUIStyle _lapCompareStyle;
        private GUIStyle _lapCompareTitleStyle;

        // Founder playtest feedback, 2026-08-20 (round 9): "no final vc
        // mostra o top melhores voltas mas nao mostra a classificacao da
        // corrida, poderia mostrar" — the finish screen only ever showed
        // the all-time best-laps leaderboard, never who actually finished
        // in what order in THIS race. Uses the same nearest-waypoint proxy
        // RaceStandingsHud uses for the live panel, snapshotted once at the
        // moment the race ends.
        private Transform _playerTransform;
        private string _playerName;
        // Founder playtest feedback, 2026-08-22: "a tela final de
        // classificação não mostra os números de corrida" — the live
        // RaceStandingsHud panel already shows each kart's race number
        // (#<n>) but RaceManager's finish screen never got the player's
        // number at all (Configure had no parameter for it) and
        // StandingEntry never carried a bot's KartBotController.RaceNumber
        // either. 0 means "not assigned", same convention as
        // KartBotController.RaceNumber, so a degraded call site (no numbers
        // configured) just omits the "#" prefix instead of showing "#0".
        private int _playerNumber;
        private IReadOnlyList<Vector3> _path;
        // Founder feedback, 2026-08-24: "depois que alteramos a pista a
        // melhor volta ficou travada na ultima deveria ser reiniciada toda
        // vez que a gente colocar uma pista nova" — see
        // LapRecordMath.CalculateClosedPathLengthMeters/FormatTrackSignature
        // for how this is derived from _path once, here, at race setup.
        private readonly List<StandingEntry> _finalStandings = new List<StandingEntry>();

        // Round 25 (2026-08-24) founder feedback: "outra coisa seria legal
        // ter o tempo do bot a comparacao dele em todas as voltas" — the
        // player's own lap times, appended in race order as
        // OnPlayerLapCompleted fires (same event RecordLap already uses),
        // so the finish screen can show a lap-by-lap "you vs. the bot"
        // table instead of only the single aggregate finish time.
        private readonly List<float> _playerLapTimes = new List<float>();

        // Round 28 (2026-08-24) founder request: "seria legal também
        // aparecer a mensagem de volta 1 volta 2 volta 3 meio que
        // notificando quando passar e o tempo que ele fez" — a brief
        // on-screen toast each time the player crosses the line, showing
        // which lap just finished and how long it took. _toastShownUntil
        // stays -1 (never "now or later") until the first lap completes,
        // so nothing draws before that.
        private const float ToastDurationSeconds = 2.6f;
        private int _toastLapNumber;
        private float _toastLapTime;
        private bool _toastInvalid;
        private float _toastShownUntil = -1f;
        private GUIStyle _toastStyle;

        private readonly struct StandingEntry
        {
            public readonly string Name;
            public readonly bool IsPlayer;
            public readonly int Number;

            public StandingEntry(string name, bool isPlayer, int number)
            {
                Name = name;
                IsPlayer = isPlayer;
                Number = number;
            }
        }

        // Founder playtest feedback, 2026-08-20 (round 8): the original
        // all-time/day/week idea never actually rendered for him (it only
        // showed on the finish screen, and he restarted before getting
        // there most of the time) and, once he saw the round-7 message
        // describing it, he asked for something more concrete instead: a
        // named top-5, "talvez pegar os 5 + o restante só descartar".
        // Computed once at finish time from LapRecordStore's persisted
        // history, tagged with PlayerNameStore's saved name.
        private const int LeaderboardSize = 5;
        private List<LapRecord> _topRecords = new List<LapRecord>();
        private PrototypeCompetitiveScope _comparisonScope;

        public bool IsFinished => _finished;

        public void Configure(TimingManagerLite timing, int targetLaps, BotDifficulty difficulty,
            KartPrototypeInput playerInput, IEnumerable<KartBotController> bots,
            PrototypeCompetitiveScope comparisonScope,
            Transform playerTransform = null, string playerName = null, IReadOnlyList<Vector3> path = null,
            int playerNumber = 0)
        {
            if (_timing != null)
            {
                _timing.OnLapCompleted -= OnPlayerLapCompleted;
                _timing.OnLapInvalidated -= OnPlayerLapInvalidated;
            }

            _timing = timing;
            _targetLaps = Mathf.Max(1, targetLaps);
            _difficulty = difficulty;
            _playerInput = playerInput;
            _bots.Clear();
            if (bots != null)
            {
                _bots.AddRange(bots);
            }

            _playerTransform = playerTransform;
            _playerName = string.IsNullOrEmpty(playerName) ? "Você" : playerName;
            _playerNumber = playerNumber;
            _path = path;
            _comparisonScope = comparisonScope;

            _raceStartTime = Time.time;
            _finished = false;
            _playerLapTimes.Clear();
            _toastShownUntil = -1f;

            if (_timing != null)
            {
                _timing.OnLapCompleted += OnPlayerLapCompleted;
                _timing.OnLapInvalidated += OnPlayerLapInvalidated;
            }
        }

        private void OnPlayerLapCompleted(float lapTime, bool isValid)
        {
            if (!isValid)
            {
                return;
            }

            _playerLapTimes.Add(lapTime);
            _toastLapNumber = _playerLapTimes.Count;
            _toastLapTime = lapTime;
            _toastInvalid = false;
            _toastShownUntil = Time.time + ToastDurationSeconds;

            // Recorded regardless of whether the race is over yet, so the
            // leaderboard reflects every clean lap ever driven, not just
            // laps in races that happened to finish.
            LapRecordStore.RecordLap(lapTime, DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                PlayerNameStore.GetName(), _comparisonScope);

            if (_finished || _timing == null || _timing.LapsCompleted < _targetLaps)
            {
                return;
            }

            // Founder playtest feedback, 2026-08-20: "quando terminar a
            // corrida o tempo de finalização não para" — the finish label
            // was recomputing Time.time - _raceStartTime every OnGUI call,
            // so it kept counting up after the race ended. Freeze it once,
            // here, instead of live-computing it in OnGUI.
            _finished = true;
            _finishTime = Time.time - _raceStartTime;
            SetAllInputEnabled(false);
            ComputeLeaderboard();
            ComputeFinalStandings();
            Debug.Log($"RaceManager: race finished in {_finishTime:0.000}s ({_targetLaps} laps).");
        }

        private void OnPlayerLapInvalidated()
        {
            _toastInvalid = true;
            _toastShownUntil = Time.time + ToastDurationSeconds;
        }

        private void ComputeLeaderboard()
        {
            var history = LapRecordStore.LoadHistory();
            var currentTrackHistory = LapRecordMath.FilterByComparisonScope(history, _comparisonScope);
            _topRecords = LapRecordMath.FindTopRecords(currentTrackHistory, LeaderboardSize);
        }

        /// <summary>
        /// Snapshots who finished ahead of whom at the moment the race
        /// ended, using the same laps-then-nearest-waypoint ranking
        /// RaceStandingsHud uses live during the race. If the player/path
        /// weren't supplied (older call sites), this just stays empty and
        /// the finish screen falls back to the best-laps leaderboard alone.
        /// </summary>
        private void ComputeFinalStandings()
        {
            _finalStandings.Clear();

            if (_playerTransform == null || _path == null || _path.Count == 0)
            {
                return;
            }

            var scratch = new List<(string Name, int Laps, int Waypoint, bool IsPlayer, int Number)>();

            var playerLaps = _timing != null ? _timing.LapsCompleted : 0;
            var playerWaypoint = RaceProgressMath.FindNearestWaypointIndex(_playerTransform.position, _path);
            scratch.Add((_playerName, playerLaps, playerWaypoint, true, _playerNumber));

            foreach (var bot in _bots)
            {
                if (bot == null)
                {
                    continue;
                }

                var waypoint = RaceProgressMath.FindNearestWaypointIndex(bot.transform.position, _path);
                scratch.Add((bot.name, bot.LapsCompleted, waypoint, false, bot.RaceNumber));
            }

            // Stable insertion sort — same tiny-N justification as
            // RaceStandingsHud's live version.
            for (var i = 1; i < scratch.Count; i++)
            {
                var current = scratch[i];
                var j = i - 1;
                while (j >= 0 && RaceProgressMath.IsAheadOf(
                    current.Laps, current.Waypoint, scratch[j].Laps, scratch[j].Waypoint))
                {
                    scratch[j + 1] = scratch[j];
                    j--;
                }
                scratch[j + 1] = current;
            }

            foreach (var entry in scratch)
            {
                _finalStandings.Add(new StandingEntry(entry.Name, entry.IsPlayer, entry.Number));
            }
        }

        /// <summary>
        /// Round 25: which bot to show in the lap-by-lap comparison table.
        /// Picks the bot that got furthest (most laps completed, tie-broken
        /// by lowest total recorded time) rather than always bot #0 — with
        /// several bots on the grid the most meaningful rival is whichever
        /// one actually raced hardest, not an arbitrary spawn-order pick.
        /// Null if there are no bots this race (e.g. RaceMode.Solo).
        /// </summary>
        private KartBotController SelectComparisonBot()
        {
            KartBotController best = null;
            var bestLaps = -1;
            var bestTotalTime = float.MaxValue;

            foreach (var bot in _bots)
            {
                if (bot == null || bot.LapTimes.Count == 0)
                {
                    continue;
                }

                var totalTime = 0f;
                for (var i = 0; i < bot.LapTimes.Count; i++)
                {
                    totalTime += bot.LapTimes[i];
                }

                var isBetter = RaceProgressMath.IsBetterComparisonCandidate(
                    bot.LapsCompleted, totalTime, bestLaps, bestTotalTime);
                if (isBetter)
                {
                    best = bot;
                    bestLaps = bot.LapsCompleted;
                    bestTotalTime = totalTime;
                }
            }

            return best;
        }

        private void SetAllInputEnabled(bool inputEnabled)
        {
            if (_playerInput != null)
            {
                _playerInput.SetInputEnabled(inputEnabled);
            }

            foreach (var bot in _bots)
            {
                if (bot != null)
                {
                    bot.SetInputEnabled(inputEnabled);
                }
            }
        }

        private void OnGUI()
        {
            var scale = Mathf.Max(1f, Screen.height / 720f);

            // Founder playtest feedback, 2026-08-20: "nem dá pra saber se é
            // fácil ou difícil" — a small always-on label so the chosen
            // lap target and bot difficulty are visible during the race,
            // not just implied by (broken) bot behavior.
            if (_infoStyle == null)
            {
                _infoStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = Mathf.RoundToInt(15f * scale),
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter
                };
                _infoStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
            }
            var infoRect = new Rect((Screen.width - 320f * scale) * 0.5f, 46f * scale, 320f * scale, 26f * scale);
            GUI.Label(infoRect, $"META: {_targetLaps} VOLTAS  •  BOTS: {DifficultyLabel(_difficulty)}", _infoStyle);

            // Round 28: "volta N — tempo" toast, shown for a few seconds
            // right after each lap completes. Drawn before the
            // !_finished early-return below so it also fires on the very
            // last lap — it just ends up covered by the finish overlay
            // that draws afterward in that case, which reads fine (no
            // point showing a small toast at the same instant as the big
            // finish screen).
            if (Time.time < _toastShownUntil)
            {
                if (_toastStyle == null)
                {
                    _toastStyle = new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontStyle = FontStyle.Bold,
                        fontSize = Mathf.RoundToInt(26f * scale)
                    };
                }

                // Fades out over the last third of the display window
                // instead of popping off abruptly.
                var remaining = _toastShownUntil - Time.time;
                var alpha = Mathf.Clamp01(remaining / (ToastDurationSeconds * 0.35f));
                _toastStyle.normal.textColor = new Color(1f, 0.85f, 0.3f, alpha);

                var toastRect = new Rect((Screen.width - 420f * scale) * 0.5f, 82f * scale, 420f * scale, 40f * scale);
                GUI.Label(toastRect, _toastInvalid
                    ? "VOLTA INVÁLIDA  •  COMPLETE O CIRCUITO"
                    : $"VOLTA {_toastLapNumber}  •  {FormatTime(_toastLapTime)}", _toastStyle);
            }

            if (!_finished)
            {
                return;
            }

            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    fontSize = Mathf.RoundToInt(42f * scale)
                };
                _style.normal.textColor = Color.white;
            }

            if (_buttonStyle == null)
            {
                _buttonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = Mathf.RoundToInt(22f * scale)
                };
            }

            if (_leaderboardStyle == null)
            {
                _leaderboardStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.UpperCenter,
                    fontSize = Mathf.RoundToInt(19f * scale)
                };
                _leaderboardStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
            }

            if (_leaderboardTitleStyle == null)
            {
                _leaderboardTitleStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    fontSize = Mathf.RoundToInt(16f * scale)
                };
                _leaderboardTitleStyle.normal.textColor = new Color(1f, 0.85f, 0.3f);
            }

            if (_standingsTitleStyle == null)
            {
                _standingsTitleStyle = new GUIStyle(_leaderboardTitleStyle);
                _standingsTitleStyle.normal.textColor = new Color(0.6f, 0.85f, 1f);
            }

            if (_standingsStyle == null)
            {
                _standingsStyle = new GUIStyle(_leaderboardStyle ?? GUI.skin.label)
                {
                    alignment = TextAnchor.UpperCenter,
                    fontSize = Mathf.RoundToInt(19f * scale)
                };
                _standingsStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);

                _playerStandingsStyle = new GUIStyle(_standingsStyle)
                {
                    fontStyle = FontStyle.Bold
                };
                _playerStandingsStyle.normal.textColor = new Color(0.35f, 0.95f, 0.4f);
            }

            if (_lapCompareTitleStyle == null)
            {
                _lapCompareTitleStyle = new GUIStyle(_leaderboardTitleStyle);
                _lapCompareTitleStyle.normal.textColor = new Color(1f, 0.6f, 0.85f);
            }

            if (_lapCompareStyle == null)
            {
                _lapCompareStyle = new GUIStyle(_leaderboardStyle ?? GUI.skin.label)
                {
                    alignment = TextAnchor.UpperCenter,
                    fontSize = Mathf.RoundToInt(17f * scale)
                };
                _lapCompareStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f);
            }

            var previousColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.65f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previousColor;

            var titleRect = new Rect(0f, Screen.height * 0.1f, Screen.width, 44f * scale);
            var timeRect = new Rect(0f, titleRect.yMax + 2f, Screen.width, 40f * scale);

            // Founder playtest feedback, 2026-08-20 (round 9): the finish
            // screen showed the all-time best-laps leaderboard but not who
            // actually finished where in THIS race — so both are shown now,
            // this race's classification first (more relevant right after
            // finishing) and the persistent best-laps list below it.
            var standingsShown = Mathf.Min(_finalStandings.Count, 6);
            var standingsTitleRect = new Rect(0f, timeRect.yMax + 8f, Screen.width, 22f * scale);
            var standingsRect = new Rect(0f, standingsTitleRect.yMax + 2f, Screen.width, 22f * scale * Mathf.Max(1, standingsShown));
            var standingsBottom = _finalStandings.Count > 0 ? standingsRect.yMax : standingsTitleRect.yMax;

            // Round 25: lap-by-lap "você x bot" comparison table, between
            // this race's classification and the all-time best-laps list.
            var comparisonBot = SelectComparisonBot();
            var lapCompareRows = comparisonBot != null
                ? Mathf.Max(_playerLapTimes.Count, comparisonBot.LapTimes.Count)
                : 0;
            var lapCompareTitleRect = new Rect(0f, standingsBottom + 10f, Screen.width, 22f * scale);
            var lapCompareRect = new Rect(0f, lapCompareTitleRect.yMax + 2f, Screen.width, 20f * scale * Mathf.Max(1, lapCompareRows));
            var lapCompareBottom = lapCompareRows > 0 ? lapCompareRect.yMax : standingsBottom;

            var leaderboardTitleRect = new Rect(0f, lapCompareBottom + 12f, Screen.width, 24f * scale);
            var leaderboardRect = new Rect(0f, leaderboardTitleRect.yMax + 4f, Screen.width, 26f * scale * LeaderboardSize);
            var restartRect = new Rect((Screen.width - 260f * scale) * 0.5f, leaderboardRect.yMax + 20f, 260f * scale, 56f * scale);

            GUI.Label(titleRect, "CORRIDA FINALIZADA!", _style);
            GUI.Label(timeRect, $"{_targetLaps} voltas em {FormatTime(_finishTime)}", _style);

            if (_finalStandings.Count > 0)
            {
                GUI.Label(standingsTitleRect, "CLASSIFICAÇÃO DA CORRIDA", _standingsTitleStyle);
                DrawFinalStandings(standingsRect, standingsShown);
            }

            if (lapCompareRows > 0)
            {
                GUI.Label(lapCompareTitleRect, $"VOCÊ x {comparisonBot.name.ToUpperInvariant()} — VOLTA A VOLTA", _lapCompareTitleStyle);
                GUI.Label(lapCompareRect, BuildLapComparisonText(comparisonBot, lapCompareRows), _lapCompareStyle);
            }

            GUI.Label(leaderboardTitleRect, $"MELHORES VOLTAS (TOP {LeaderboardSize})", _leaderboardTitleStyle);
            GUI.Label(leaderboardRect, BuildLeaderboardText(), _leaderboardStyle);

            // Founder playtest feedback, 2026-08-20: "poderia ter um botão
            // para fazer o start novamente da sessão, ao finalizar...".
            if (GUI.Button(restartRect, "CORRER DE NOVO", _buttonStyle))
            {
                RaceRestartButton.RestartRace();
            }
        }

        /// <summary>
        /// Drawn row-by-row (rather than one joined multi-line label like
        /// <see cref="BuildLeaderboardText"/>) so the player's own row can
        /// be highlighted, matching RaceStandingsHud's live panel.
        /// </summary>
        private void DrawFinalStandings(Rect area, int shown)
        {
            var rowHeight = area.height / Mathf.Max(1, shown);
            var y = area.y;
            for (var i = 0; i < shown; i++)
            {
                var entry = _finalStandings[i];
                var style = entry.IsPlayer ? _playerStandingsStyle : _standingsStyle;
                // Same "#<n> " convention as RaceStandingsHud's live panel;
                // Number 0 means "not assigned" (older/degraded call sites
                // that never configured race numbers), so just omit it
                // instead of showing a meaningless "#0".
                var numberLabel = entry.Number > 0 ? $"#{entry.Number} " : string.Empty;
                var label = $"{i + 1}. {numberLabel}{entry.Name}";
                GUI.Label(new Rect(area.x, y, area.width, rowHeight), label, style);
                y += rowHeight;
            }
        }

        /// <summary>
        /// Round 25: one line per lap, "Volta N: você Xs | bot Ys (+delta)".
        /// A lap either side didn't complete (bot fell short, or the race
        /// ended on the player's last lap before the bot finished it) is
        /// shown as "—" rather than a misleading 0:00.000, and no delta is
        /// printed when either side is missing.
        /// </summary>
        private string BuildLapComparisonText(KartBotController comparisonBot, int rows)
        {
            var lines = new string[rows];
            for (var i = 0; i < rows; i++)
            {
                var playerHasLap = i < _playerLapTimes.Count;
                var botHasLap = i < comparisonBot.LapTimes.Count;
                var playerText = playerHasLap ? FormatTime(_playerLapTimes[i]) : "—";
                var botText = botHasLap ? FormatTime(comparisonBot.LapTimes[i]) : "—";
                var deltaText = string.Empty;
                if (playerHasLap && botHasLap)
                {
                    var delta = _playerLapTimes[i] - comparisonBot.LapTimes[i];
                    deltaText = delta <= 0f ? $"  ({delta:0.000}s)" : $"  (+{delta:0.000}s)";
                }

                lines[i] = $"Volta {i + 1}: você {playerText}  |  bot {botText}{deltaText}";
            }

            return string.Join("\n", lines);
        }

        private string BuildLeaderboardText()
        {
            if (_topRecords == null || _topRecords.Count == 0)
            {
                return "(nenhuma volta válida registrada ainda)";
            }

            var lines = new string[_topRecords.Count];
            for (var i = 0; i < _topRecords.Count; i++)
            {
                var record = _topRecords[i];
                var name = string.IsNullOrEmpty(record.PlayerName) ? "?" : record.PlayerName;
                lines[i] = $"{i + 1}. {name} — {FormatTime(record.LapTimeSeconds)}";
            }

            return string.Join("\n", lines);
        }

        private static string DifficultyLabel(BotDifficulty difficulty)
        {
            switch (difficulty)
            {
                case BotDifficulty.Easy:
                    return "FÁCIL";
                case BotDifficulty.Medium:
                    return "MÉDIO";
                case BotDifficulty.Hard:
                    return "DIFÍCIL";
                default:
                    return "?";
            }
        }

        private static string FormatTime(float seconds)
        {
            var minutes = (int)(seconds / 60f);
            var secs = seconds - minutes * 60f;
            return $"{minutes}:{secs:00.000}";
        }

        private void OnDestroy()
        {
            if (_timing != null)
            {
                _timing.OnLapCompleted -= OnPlayerLapCompleted;
                _timing.OnLapInvalidated -= OnPlayerLapInvalidated;
            }
        }
    }
}
