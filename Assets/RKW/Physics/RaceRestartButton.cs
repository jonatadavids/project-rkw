using UnityEngine;
using UnityEngine.SceneManagement;

namespace RKW.Physics
{
    /// <summary>
    /// Founder playtest feedback, 2026-08-20: "poderia ter um botão para
    /// fazer o start novamente da sessão, ao finalizar ou a qualquer
    /// tempo". A small always-on-screen button that reloads the active
    /// scene — the simplest reliable "restart" for a scene that is built
    /// entirely at runtime by <see cref="KartPhysicsPrototypeBootstrap"/>:
    /// reloading re-runs every Awake() from scratch instead of hand-writing
    /// a manual reset for every system (physics, timing, bots, audio...).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RaceRestartButton : MonoBehaviour
    {
        private GUIStyle _buttonStyle;

        /// <summary>Reloads the active scene. Shared by this button and <see cref="RaceManager"/>'s finish-screen button so there is one restart implementation.</summary>
        public static void RestartRace()
        {
            // Rodada 46 (2026-09-01) founder feedback: "correr novamente
            // significa reiniciar a mesma corrida... teria que ir direto"
            // -- flag the upcoming reload so KartPhysicsPrototypeBootstrap.Awake
            // skips MainMenu/TrackSelectMenu/RaceSetupMenu and rebuilds
            // the same race directly. See that flag's own doc comment.
            KartPhysicsPrototypeBootstrap.RequestQuickRestart();
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void OnGUI()
        {
            var scale = Mathf.Max(1f, Screen.height / 720f);
            if (_buttonStyle == null)
            {
                _buttonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = Mathf.RoundToInt(14f * scale)
                };
            }

            // Round 46: shares TopCenterButtonLayout with PauseButton (which
            // sits immediately to this button's left) so the two of them
            // center as ONE block on Screen.width, matching RaceManager's
            // centered META label underneath -- see that class's own doc
            // comment. This button no longer centers itself alone.
            var width = TopCenterButtonLayout.RestartWidthRaw * scale;
            var height = TopCenterButtonLayout.HeightRaw * scale;
            var rect = new Rect(TopCenterButtonLayout.RestartButtonX(scale, Screen.width), 8f * scale, width, height);
            if (GUI.Button(rect, "REINICIAR", _buttonStyle))
            {
                RestartRace();
            }
        }
    }
}
