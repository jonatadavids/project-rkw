using UnityEngine;

namespace RKW.Physics
{
    /// <summary>
    /// Rodada 46 (2026-09-01) founder feedback: "poderia ter um botao de
    /// pause e continue ainda mas nesse modo de bot ou com o fantasma" --
    /// a small always-on-screen button (next to RaceRestartButton, same
    /// top-center row) that freezes/resumes the race via
    /// <see cref="Time.timeScale"/>. This is deliberately the simplest
    /// possible pause: setting timeScale to 0 stops every FixedUpdate
    /// (Rigidbody physics for every kart) and scales Time.deltaTime/
    /// Time.time to a standstill for free -- and this whole project
    /// already reads Time.time (not Time.unscaledTime) for every
    /// race-critical timer (RaceManager's elapsed time, TimingManagerLite's
    /// lap/split clocks, GhostController's recording clock, the countdown
    /// in RaceStartController), so all of those correctly freeze too
    /// without any extra wiring. OnGUI itself is NOT affected by
    /// timeScale, so this button (and every other HUD element) keeps
    /// drawing and responding to taps while paused.
    ///
    /// Scoped to "bot mode or with the ghost" per the founder's own
    /// wording: this prototype has no live multiplayer race yet (see
    /// KartPhysicsPrototypeBootstrap's own wiring comment where this
    /// button is created) -- everyone on track is either the player, a
    /// local bot, or a recorded ghost with no physics of its own, so
    /// nobody else's session is affected by freezing this one. If/when a
    /// real online race (Photon Fusion) exists, pausing a shared match
    /// this way would not be correct anymore and this button would need
    /// to be hidden for that mode specifically -- not attempted here since
    /// that mode does not exist in the game yet.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PauseButton : MonoBehaviour
    {
        private bool _paused;
        private GUIStyle _cornerButtonStyle;
        private GUIStyle _overlayTitleStyle;
        private GUIStyle _resumeButtonStyle;
        private Texture2D _dimTexture;

        private void OnDestroy()
        {
            // Safety net: KartPhysicsPrototypeBootstrap.Awake() already
            // resets Time.timeScale = 1f on every (re)load (see its own
            // rodada-46 comment), but resetting here too means this
            // component leaving a still-paused scene never leaves the
            // engine stuck at timeScale 0 for whatever runs next.
            Time.timeScale = 1f;
        }

        private void EnsureStyles()
        {
            if (_cornerButtonStyle != null)
            {
                return;
            }

            var scale = Mathf.Max(1f, Screen.height / 720f);

            _cornerButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = Mathf.RoundToInt(14f * scale)
            };

            _overlayTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(34f * scale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            _overlayTitleStyle.normal.textColor = new Color(1f, 0.85f, 0.3f);

            _resumeButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = Mathf.RoundToInt(20f * scale)
            };

            _dimTexture = Texture2D.whiteTexture;
        }

        private void OnGUI()
        {
            EnsureStyles();

            var scale = Mathf.Max(1f, Screen.height / 720f);

            // Round 46: shares TopCenterButtonLayout with RaceRestartButton
            // so the PAUSE+REINICIAR pair centers as ONE block on
            // Screen.width -- see that class's own doc comment for why
            // (founder feedback: the pair had drifted out of alignment
            // with RaceManager's centered META label underneath it).
            var x = TopCenterButtonLayout.PairLeftX(scale, Screen.width);
            var width = TopCenterButtonLayout.PauseWidthRaw * scale;
            var rect = new Rect(x, 8f * scale, width, TopCenterButtonLayout.HeightRaw * scale);

            var label = _paused ? "CONTINUAR" : "PAUSE";
            if (GUI.Button(rect, label, _cornerButtonStyle))
            {
                TogglePause();
            }

            if (!_paused)
            {
                return;
            }

            var previousColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), _dimTexture);
            GUI.color = previousColor;

            var titleRect = new Rect(0f, Screen.height * 0.4f, Screen.width, 60f * scale);
            GUI.Label(titleRect, "PAUSADO", _overlayTitleStyle);

            var resumeWidth = 220f * scale;
            var resumeHeight = 56f * scale;
            var resumeRect = new Rect((Screen.width - resumeWidth) * 0.5f, Screen.height * 0.4f + 70f * scale, resumeWidth, resumeHeight);
            if (GUI.Button(resumeRect, "CONTINUAR", _resumeButtonStyle))
            {
                TogglePause();
            }
        }

        private void TogglePause()
        {
            _paused = !_paused;
            Time.timeScale = _paused ? 0f : 1f;
        }
    }
}
