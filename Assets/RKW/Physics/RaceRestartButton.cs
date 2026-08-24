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

            var width = 110f * scale;
            var height = 34f * scale;
            var rect = new Rect((Screen.width - width) * 0.5f, 8f * scale, width, height);
            if (GUI.Button(rect, "REINICIAR", _buttonStyle))
            {
                RestartRace();
            }
        }
    }
}
