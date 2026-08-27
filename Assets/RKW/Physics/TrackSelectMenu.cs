using System;
using UnityEngine;

namespace RKW.Physics
{
    /// <summary>
    /// Round-36 founder feedback ("o circuito oval pode dar o nome de
    /// Circuito Oval e deixar selecionável"): the very first screen shown
    /// each session, before any track/kart geometry is built at all — lets
    /// the player pick which track to drive instead of the previous
    /// build-time-only <c>UseTechnicalCircuit2</c> toggle (see that
    /// field's own comment in <see cref="KartPhysicsPrototypeBootstrap"/>
    /// for the round-34 "pre-race toggle, not a full selection screen"
    /// decision this now replaces).
    ///
    /// Deliberately minimal, same "not production UI, just enough to
    /// work" scope as <see cref="RaceSetupMenu"/> (whose OnGUI/GUIStyle-
    /// scaling pattern is reused here) — no track thumbnail/preview image
    /// yet, just the two names and one line describing each. Can be made
    /// prettier later without touching the flow below.
    ///
    /// <see cref="KartPhysicsPrototypeBootstrap.Awake"/> shows this FIRST
    /// and only builds the track/kart/camera/timing/course (previously
    /// all done unconditionally in Awake itself, before the player ever
    /// saw a menu) once this confirms — which track to build has to be
    /// known before any of that geometry exists, so this necessarily runs
    /// before <see cref="RaceSetupMenu"/> (laps/bots/difficulty), not
    /// alongside it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TrackSelectMenu : MonoBehaviour
    {
        private Action<bool> _onConfirm;
        private bool _confirmed;

        private GUIStyle _titleStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _buttonStyle;

        public void Configure(Action<bool> onConfirm)
        {
            _onConfirm = onConfirm;
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
                fontSize = Mathf.RoundToInt(16f * scale),
                alignment = TextAnchor.MiddleCenter
            };
            _labelStyle.normal.textColor = new Color(0.75f, 0.75f, 0.75f);

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = Mathf.RoundToInt(22f * scale)
            };
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
            var y = Screen.height * 0.22f;

            var previousColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.72f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previousColor;

            GUI.Label(new Rect(panelX, y, panelWidth, 50f * scale), "ESCOLHA A PISTA", _titleStyle);
            y += 62f * scale;

            var ovalRect = new Rect(panelX, y, panelWidth, 64f * scale);
            if (GUI.Button(ovalRect, "CIRCUITO OVAL", _buttonStyle))
            {
                Confirm(false);
            }
            y += 70f * scale;
            GUI.Label(new Rect(panelX, y, panelWidth, 26f * scale), "traçado oval, mais rápido e fácil", _labelStyle);
            y += 50f * scale;

            var carreraRect = new Rect(panelX, y, panelWidth, 64f * scale);
            if (GUI.Button(carreraRect, "CARRERA KART", _buttonStyle))
            {
                Confirm(true);
            }
            y += 70f * scale;
            GUI.Label(new Rect(panelX, y, panelWidth, 26f * scale), "traçado técnico, extraído de pista real", _labelStyle);
        }

        private void Confirm(bool useTechnicalCircuit2)
        {
            _confirmed = true;
            _onConfirm?.Invoke(useTechnicalCircuit2);
        }
    }
}
