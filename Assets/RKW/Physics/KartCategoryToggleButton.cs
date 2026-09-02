using UnityEngine;

namespace RKW.Physics
{
    /// <summary>
    /// Round 32 (2026-08-24) founder request: he sent a second, faster kart
    /// model ("18 HP / 80 km/h" — the existing kart is now retuned to
    /// "13 HP / 60 km/h", see docs/30-founder-playtest-log.md rodada 32 for
    /// the exact numbers and why they changed). There is no proper
    /// pre-race category-selection SCREEN yet — that is its own, larger
    /// task, not attempted this round. This is the smallest safe way to
    /// let him compare both karts on a real phone build: an always-on
    /// on-screen button, active only BEFORE the race actually starts
    /// (mirrors <see cref="RaceStartController"/>'s own input-lock window
    /// — see <see cref="KartPrototypeInput.InputEnabled"/>), that tears
    /// down and rebuilds the player kart's visual + physics tuning in
    /// place via <see cref="KartPhysicsPrototypeBootstrap.RebuildKartVisual"/>.
    ///
    /// Restricted to the pre-race window on purpose: the kart is
    /// guaranteed stationary at its grid slot at that point, so there is
    /// no live physics state (speed, grip, drift) to reconcile with a
    /// suddenly different tuning asset mid-race.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class KartCategoryToggleButton : MonoBehaviour
    {
        private KartDynamics _dynamics;
        private KartPrototypeInput _playerInput;
        private Color _tintColor;
        private int _raceNumber;
        private bool _usingV2;
        private GUIStyle _buttonStyle;

        public void Configure(KartDynamics dynamics, KartPrototypeInput playerInput, Color tintColor, int raceNumber,
            bool startsUsingV2 = false)
        {
            _dynamics = dynamics;
            _playerInput = playerInput;
            _tintColor = tintColor;
            _raceNumber = raceNumber;
            // Round 45 (2026-09-01): the player's kart can now spawn
            // already using the 18 HP model (see
            // KartPhysicsPrototypeBootstrap.BeginRace and
            // RacePreferencesStore) instead of always starting on 13 HP --
            // this button must reflect whichever one is ACTUALLY spawned,
            // or its label ("toque para trocar PARA X") would show the
            // wrong current kart on the very first frame.
            _usingV2 = startsUsingV2;
        }

        private void OnGUI()
        {
            if (_dynamics == null || _playerInput == null)
            {
                return;
            }

            // Only tappable before "VAI!" — see class doc for why.
            if (_playerInput.InputEnabled)
            {
                return;
            }

            var scale = Mathf.Max(1f, Screen.height / 720f);
            if (_buttonStyle == null)
            {
                // Round 37: shrunk font / widened+heightened the button
                // (was 210x42 @15pt) so the longer "(toque p/ trocar)"
                // label fits on one line instead of overflowing/clipping.
                _buttonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = Mathf.RoundToInt(13f * scale),
                    wordWrap = true
                };
            }

            var safe = Screen.safeArea;
            var width = 260f * scale;
            var height = 46f * scale;
            // Stacked directly under CameraViewToggleButton, same top-left
            // corner — see that button's own class doc for why every other
            // screen corner is already taken by another HUD element.
            var y = Screen.height - safe.yMax + 8f * scale + height + 6f * scale;
            var rect = new Rect(safe.xMin + 8f * scale, y, width, height);

            // Round 37 founder feedback: "os botoes de selecionar parece
            // invertido" -- this used to name the kart you'd SWITCH TO
            // (matching the camera toggle button's convention), but a
            // label naming a DIFFERENT kart than the one you're currently
            // driving reads as backwards/wrong once you compare it against
            // how the kart actually feels on track. Now shows the kart
            // you're CURRENTLY driving, with an explicit "toque para
            // trocar" so it's still clear this is a toggle, not just a
            // status readout.
            var label = _usingV2 ? "KART: 18 HP · 80 km/h (toque p/ trocar)" : "KART: 13 HP · 60 km/h (toque p/ trocar)";
            if (GUI.Button(rect, label, _buttonStyle))
            {
                _usingV2 = !_usingV2;
                var modelPath = _usingV2
                    ? KartPhysicsPrototypeBootstrap.KartVisualV2ResourcePath
                    : KartPhysicsPrototypeBootstrap.KartVisualResourcePath;
                var tuningPath = _usingV2
                    ? KartPhysicsPrototypeBootstrap.TuningV2ResourcePath
                    : KartPhysicsPrototypeBootstrap.TuningResourcePath;
                KartPhysicsPrototypeBootstrap.RebuildKartVisual(_dynamics, modelPath, tuningPath, _tintColor, _raceNumber);
            }
        }
    }
}
