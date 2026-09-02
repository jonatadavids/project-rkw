using System.Collections.Generic;
using UnityEngine;

namespace RKW.Physics
{
    /// <summary>
    /// Founder playtest feedback, 2026-08-20 (round 8): "não mostrou a
    /// classificação... nem durante a corrida nem o nome dos bots". A
    /// small always-on panel ranking the player and every bot by laps
    /// completed (ties broken by <see cref="RaceProgressMath"/>'s nearest-
    /// waypoint proxy), so bot identities and live position are visible
    /// throughout the race, not just guessed at. Deliberately display-only —
    /// does not feed back into gameplay.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RaceStandingsHud : MonoBehaviour
    {
        private const int MaxRowsShown = 6;

        private readonly struct Entry
        {
            public readonly string Name;
            public readonly int Laps;
            public readonly int WaypointIndex;
            public readonly bool IsPlayer;
            public readonly int Number;

            public Entry(string name, int laps, int waypointIndex, bool isPlayer, int number)
            {
                Name = name;
                Laps = laps;
                WaypointIndex = waypointIndex;
                IsPlayer = isPlayer;
                Number = number;
            }
        }

        private Transform _playerTransform;
        private TimingManagerLite _timing;
        private string _playerName;
        private int _playerNumber;
        private IReadOnlyList<Vector3> _path;
        private readonly List<KartBotController> _bots = new List<KartBotController>();
        private readonly List<Entry> _scratchEntries = new List<Entry>();

        private GUIStyle _rowStyle;
        private GUIStyle _playerRowStyle;
        private GUIStyle _titleStyle;

        // Founder playtest feedback, 2026-08-20 (round 16): "talvez seja
        // legal colocar numeros nos carros de forma aleatório" — playerNumber
        // and each bot's KartBotController.RaceNumber (both drawn from the
        // same shuffled pool in KartPhysicsPrototypeBootstrap) are shown
        // alongside the name so the standings panel can tell karts apart at
        // a glance, same as real race numbers.
        public void Configure(Transform playerTransform, TimingManagerLite timing, string playerName, int playerNumber,
            IReadOnlyList<Vector3> path, IEnumerable<KartBotController> bots)
        {
            _playerTransform = playerTransform;
            _timing = timing;
            _playerName = string.IsNullOrEmpty(playerName) ? "Piloto" : playerName;
            _playerNumber = playerNumber;
            _path = path;
            _bots.Clear();
            if (bots != null)
            {
                _bots.AddRange(bots);
            }
        }

        private void OnGUI()
        {
            if (_playerTransform == null || _path == null || _path.Count == 0)
            {
                return;
            }

            EnsureStyles();
            BuildRankedEntries();

            var scale = Mathf.Max(1f, Screen.height / 720f);
            var rowHeight = 22f * scale;
            var panelWidth = 260f * scale;
            var shown = Mathf.Min(_scratchEntries.Count, MaxRowsShown);
            var panelHeight = (shown + 1) * rowHeight + 8f * scale;
            // Round 44 (2026-09-01) founder feedback: this panel was
            // overlapping TimingHUD's lap-time readout on lower-resolution
            // screens (both anchored near the top-right corner). See
            // HudLayoutMath's doc comment for the full root cause -- in
            // short, TimingHUD uses fixed pixel sizes while this panel
            // scales with screen height, so a low-scale device could see
            // this panel's old fixed "80 * scale" start land on top of
            // TimingHUD's rows.
            var panelTop = HudLayoutMath.ComputeStandingsPanelTop(scale, Screen.safeArea.yMin, defaultTopPixels: 80f);
            var panelRect = new Rect(Screen.width - panelWidth - 12f * scale, panelTop, panelWidth, panelHeight);

            var previousColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.45f);
            GUI.DrawTexture(panelRect, Texture2D.whiteTexture);
            GUI.color = previousColor;

            var y = panelRect.y + 4f * scale;
            GUI.Label(new Rect(panelRect.x, y, panelRect.width, rowHeight), "CLASSIFICAÇÃO", _titleStyle);
            y += rowHeight;

            for (var i = 0; i < shown; i++)
            {
                var entry = _scratchEntries[i];
                var style = entry.IsPlayer ? _playerRowStyle : _rowStyle;
                var numberLabel = entry.Number > 0 ? $"#{entry.Number} " : string.Empty;
                var label = $"{i + 1}. {numberLabel}{entry.Name}  (V{entry.Laps})";
                GUI.Label(new Rect(panelRect.x + 8f * scale, y, panelRect.width - 12f * scale, rowHeight), label, style);
                y += rowHeight;
            }
        }

        private void BuildRankedEntries()
        {
            _scratchEntries.Clear();

            var playerLaps = _timing != null ? _timing.LapsCompleted : 0;
            var playerWaypoint = RaceProgressMath.FindNearestWaypointIndex(_playerTransform.position, _path);
            _scratchEntries.Add(new Entry(_playerName, playerLaps, playerWaypoint, true, _playerNumber));

            for (var i = 0; i < _bots.Count; i++)
            {
                var bot = _bots[i];
                if (bot == null)
                {
                    continue;
                }

                var waypoint = RaceProgressMath.FindNearestWaypointIndex(bot.transform.position, _path);
                _scratchEntries.Add(new Entry(bot.name, bot.LapsCompleted, waypoint, false, bot.RaceNumber));
            }

            // Simple stable insertion sort — entry counts here are tiny
            // (player + up to 9 bots), so O(n^2) is not a concern.
            for (var i = 1; i < _scratchEntries.Count; i++)
            {
                var current = _scratchEntries[i];
                var j = i - 1;
                while (j >= 0 && RaceProgressMath.IsAheadOf(
                    current.Laps, current.WaypointIndex, _scratchEntries[j].Laps, _scratchEntries[j].WaypointIndex))
                {
                    _scratchEntries[j + 1] = _scratchEntries[j];
                    j--;
                }
                _scratchEntries[j + 1] = current;
            }
        }

        private void EnsureStyles()
        {
            if (_rowStyle != null)
            {
                return;
            }

            var scale = Mathf.Max(1f, Screen.height / 720f);

            _rowStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(14f * scale),
                alignment = TextAnchor.MiddleLeft
            };
            _rowStyle.normal.textColor = new Color(0.85f, 0.85f, 0.85f);

            _playerRowStyle = new GUIStyle(_rowStyle)
            {
                fontStyle = FontStyle.Bold
            };
            _playerRowStyle.normal.textColor = new Color(0.35f, 0.95f, 0.4f);

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(13f * scale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            _titleStyle.normal.textColor = new Color(1f, 0.85f, 0.3f);
        }
    }
}
