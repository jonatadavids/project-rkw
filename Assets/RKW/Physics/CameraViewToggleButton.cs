using UnityEngine;

namespace RKW.Physics
{
    /// <summary>
    /// Founder request, 2026-08-23: "queria aquela visão do piloto" — a
    /// small always-on-screen button so a mobile player (no keyboard) can
    /// switch <see cref="KartPrototypeCamera"/> between the chase view and
    /// the new cockpit/driver view. Top-left corner: the only screen
    /// corner not already used by another HUD element (RaceRestartButton
    /// and RaceManager's lap/difficulty label sit top-center,
    /// RaceStandingsHud sits top-right, KartPrototypeInput's touch
    /// steering/pedals cover the whole bottom).
    ///
    /// Round 27 (2026-08-24) founder feedback: "corrigir o controle de
    /// troca de câmera para permitir a visão em 1ª pessoa" — round 23
    /// already logged "a câmera não mudou ao apertar o botão", never
    /// diagnosed for lack of a device log at the time. Reviewed
    /// `KartPrototypeCamera`'s toggle/snap logic end to end and found it
    /// logically sound (confirmed again this round); the one real bug
    /// found here is that this button was the ONLY on-screen control in
    /// the whole prototype positioned from raw `(0, 0)` screen coordinates
    /// instead of `Screen.safeArea` — every other element
    /// (`KartPrototypeInput`'s pedals/wheel, `RaceRestartButton`,
    /// `RaceStandingsHud`) already anchors to the safe area specifically
    /// because Android's status bar / gesture-nav / camera-cutout insets
    /// can eat into raw screen space. A button sitting under that inset is
    /// visible-but-untappable on some devices, which matches "pressed and
    /// nothing happened" better than an actual code path failing (nothing
    /// else in the toggle logic was found to be broken). Also enlarged the
    /// tap target — 140x34 scaled was on the small side for a top corner a
    /// thumb reaches at an angle. Documented as a best-evidence fix, not a
    /// confirmed root cause — see docs/30-founder-playtest-log.md rodada
    /// 27 for the full writeup and what to check if it still fails.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CameraViewToggleButton : MonoBehaviour
    {
        private KartPrototypeCamera _camera;
        private GUIStyle _buttonStyle;

        public void Configure(KartPrototypeCamera camera)
        {
            _camera = camera;
        }

        private void OnGUI()
        {
            if (_camera == null)
            {
                return;
            }

            var scale = Mathf.Max(1f, Screen.height / 720f);
            if (_buttonStyle == null)
            {
                _buttonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = Mathf.RoundToInt(16f * scale)
                };
            }

            var safe = Screen.safeArea;
            var width = 160f * scale;
            var height = 42f * scale;
            var rect = new Rect(safe.xMin + 8f * scale, Screen.height - safe.yMax + 8f * scale, width, height);
            // Label names the view you'll SWITCH TO (matches the restart
            // button's imperative style), not the one currently active.
            var label = _camera.ViewMode == CameraViewMode.Chase ? "VISÃO PILOTO" : "VISÃO TRASEIRA";
            if (GUI.Button(rect, label, _buttonStyle))
            {
                _camera.ToggleViewMode();
            }
        }
    }
}
