using System;
using UnityEngine;
using UnityEngine.InputSystem;

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
    /// Rodada 46 (2026-09-01) founder feedback: "poderia sei lá meio que
    /// passar os circuitos no touch hj ele só aparece 1 circuito exemplo
    /// oval". The ORIGINAL layout stacked both tracks' buttons + captions
    /// vertically in one tall column starting at 22% of screen height --
    /// on a portrait phone screen that column runs past the bottom edge,
    /// so only the first entry (CIRCUITO OVAL) was actually reachable;
    /// CARRERA KART existed in the code but was rendered off-screen. This
    /// rewrite shows ONE track at a time as a single centered card (which
    /// can never overflow, by construction — it is sized to fit the
    /// screen instead of growing with the track count) with left/right
    /// arrow buttons AND a touch/mouse swipe gesture to move between
    /// tracks, plus small page dots showing how many tracks exist and
    /// which one is selected. Still only 2 tracks today, but
    /// <see cref="Tracks"/> is a plain array specifically so a third track
    /// later is a one-line addition, not a layout change.
    ///
    /// Deliberately minimal, same "not production UI, just enough to
    /// work" scope as <see cref="RaceSetupMenu"/> (whose OnGUI/GUIStyle-
    /// scaling pattern is reused here) — no track thumbnail/preview image
    /// yet, just the name and one descriptive line per card. Can be made
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
        private readonly struct TrackOption
        {
            public readonly string Name;
            public readonly string Subtitle;
            public readonly bool UseTechnicalCircuit2;

            public TrackOption(string name, string subtitle, bool useTechnicalCircuit2)
            {
                Name = name;
                Subtitle = subtitle;
                UseTechnicalCircuit2 = useTechnicalCircuit2;
            }
        }

        private static readonly TrackOption[] Tracks =
        {
            new TrackOption("CIRCUITO OVAL", "traçado oval, mais rápido e fácil", useTechnicalCircuit2: false),
            new TrackOption("CARRERA KART", "traçado técnico, extraído de pista real", useTechnicalCircuit2: true),
        };

        // Minimum horizontal drag, in raw pixels (not scaled -- this is a
        // physical-gesture distance, same idea as a phone OS's own swipe
        // thresholds, so it should NOT shrink/grow with this menu's own
        // scale factor), before a touch/mouse drag counts as a swipe
        // instead of an accidental wobble or a tap.
        private const float SwipeThresholdPixels = 60f;

        private Action<bool> _onConfirm;
        private bool _confirmed;
        private int _trackIndex;
        private float? _dragStartX;

        private GUIStyle _titleStyle;
        private GUIStyle _cardTitleStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _arrowButtonStyle;

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

            _cardTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(34f * scale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            _cardTitleStyle.normal.textColor = Color.white;

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(16f * scale),
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            _labelStyle.normal.textColor = new Color(0.75f, 0.75f, 0.75f);

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = Mathf.RoundToInt(22f * scale)
            };

            _arrowButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = Mathf.RoundToInt(30f * scale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
        }

        private void Update()
        {
            // Founder request: "poderia sei lá meio que passar os
            // circuitos no touch" -- swipe support alongside the on-screen
            // arrow buttons (buttons alone already fix the "can't reach
            // the second track" bug; this adds the touch gesture on top).
            // Polled in Update (once per frame) rather than inside OnGUI
            // (which can run multiple times per frame) so a single
            // physical swipe is never counted twice.
            //
            // IMPORTANT: this project's Player Settings have Active Input
            // Handling set to "Input System" only (ProjectSettings.asset
            // activeInputHandler: 1) -- the legacy UnityEngine.Input class
            // throws InvalidOperationException the instant it is touched
            // under that setting. The first version of this method used
            // Input.GetTouch/Input.GetMouseButtonDown and, because Update()
            // runs every single frame, that exception fired continuously
            // and (per Unity Test Framework's default "any logged error
            // fails the currently running test" behavior) took down ~50
            // unrelated PlayMode tests that happened to be running at the
            // same time -- caught via a real build_deploy_verify.sh run,
            // not guessed at. Fixed by reading input the same way
            // KartPrototypeInput already does elsewhere in this project:
            // Touchscreen.current / Mouse.current from
            // UnityEngine.InputSystem, never UnityEngine.Input.
            if (_confirmed)
            {
                return;
            }

            var touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                var touch = touchscreen.primaryTouch;
                var phase = touch.phase.ReadValue();
                if (phase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    _dragStartX = touch.position.ReadValue().x;
                }
                else if (phase == UnityEngine.InputSystem.TouchPhase.Ended
                    || phase == UnityEngine.InputSystem.TouchPhase.Canceled)
                {
                    TryHandleSwipeEnd(touch.position.ReadValue().x);
                }

                return;
            }

            // Desktop/Editor fallback (no touchscreen device) -- lets this
            // be tested with a mouse drag in the Editor, same convenience
            // the previous (broken) version intended.
            var mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            if (mouse.leftButton.wasPressedThisFrame)
            {
                _dragStartX = mouse.position.ReadValue().x;
            }
            else if (mouse.leftButton.wasReleasedThisFrame)
            {
                TryHandleSwipeEnd(mouse.position.ReadValue().x);
            }
        }

        private void TryHandleSwipeEnd(float endX)
        {
            if (!_dragStartX.HasValue)
            {
                return;
            }

            var deltaX = endX - _dragStartX.Value;
            _dragStartX = null;

            if (Mathf.Abs(deltaX) < SwipeThresholdPixels)
            {
                // Too small to be a deliberate swipe -- most likely the
                // same tap that just pressed a GUI.Button (arrow/confirm),
                // which already handles itself; ignore it here.
                return;
            }

            if (deltaX < 0f)
            {
                ShowNextTrack();
            }
            else
            {
                ShowPreviousTrack();
            }
        }

        private void OnGUI()
        {
            if (_confirmed)
            {
                return;
            }

            EnsureStyles();
            var scale = Mathf.Max(1f, Screen.height / 720f);

            var previousColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.72f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previousColor;

            // Card is sized as a fraction of the actual screen (capped at
            // a comfortable maximum), so it always fits regardless of
            // screen size/orientation -- see this class's own doc comment
            // for why that is the actual fix for the founder's "só
            // aparece 1 circuito" report.
            var cardWidth = Mathf.Min(620f * scale, Screen.width * 0.86f);
            var cardHeight = Mathf.Min(300f * scale, Screen.height * 0.42f);
            var cardX = (Screen.width - cardWidth) * 0.5f;
            var cardY = Screen.height * 0.5f - cardHeight * 0.5f;

            GUI.Label(new Rect(cardX, cardY - 74f * scale, cardWidth, 50f * scale), "ESCOLHA A PISTA", _titleStyle);

            var cardRect = new Rect(cardX, cardY, cardWidth, cardHeight);
            GUI.color = new Color(1f, 1f, 1f, 0.08f);
            GUI.DrawTexture(cardRect, Texture2D.whiteTexture);
            GUI.color = previousColor;

            var track = Tracks[_trackIndex];
            GUI.Label(new Rect(cardRect.x, cardRect.y + 34f * scale, cardRect.width, 50f * scale), track.Name, _cardTitleStyle);
            GUI.Label(
                new Rect(cardRect.x + 60f * scale, cardRect.y + 96f * scale, cardRect.width - 120f * scale, 90f * scale),
                track.Subtitle, _labelStyle);

            // Arrow buttons sit INSIDE the card's own bounds (not
            // outside/beside it) so they are always fully visible no
            // matter how narrow the screen is -- an arrow placed outside
            // the card could itself run off a very narrow phone screen,
            // the exact class of bug this rewrite exists to fix.
            var arrowSize = 56f * scale;
            var arrowY = cardRect.y + cardRect.height * 0.5f - arrowSize * 0.5f;
            if (_trackIndex > 0)
            {
                var leftArrowRect = new Rect(cardRect.x + 10f * scale, arrowY, arrowSize, arrowSize);
                if (GUI.Button(leftArrowRect, "‹", _arrowButtonStyle))
                {
                    ShowPreviousTrack();
                }
            }

            if (_trackIndex < Tracks.Length - 1)
            {
                var rightArrowRect = new Rect(cardRect.xMax - arrowSize - 10f * scale, arrowY, arrowSize, arrowSize);
                if (GUI.Button(rightArrowRect, "›", _arrowButtonStyle))
                {
                    ShowNextTrack();
                }
            }

            DrawPageDots(cardRect, scale);

            var confirmRect = new Rect(cardX, cardRect.yMax + 30f * scale, cardWidth, 64f * scale);
            if (GUI.Button(confirmRect, "SELECIONAR", _buttonStyle))
            {
                Confirm(track.UseTechnicalCircuit2);
            }
        }

        private void DrawPageDots(Rect cardRect, float scale)
        {
            const float DotSize = 10f;
            const float DotGap = 8f;
            var totalWidth = Tracks.Length * DotSize * scale + (Tracks.Length - 1) * DotGap * scale;
            var startX = cardRect.x + cardRect.width * 0.5f - totalWidth * 0.5f;
            var dotY = cardRect.yMax - (DotSize + 14f) * scale;

            var previousColor = GUI.color;
            for (var i = 0; i < Tracks.Length; i++)
            {
                var dotX = startX + i * (DotSize + DotGap) * scale;
                GUI.color = i == _trackIndex ? Color.white : new Color(1f, 1f, 1f, 0.35f);
                GUI.DrawTexture(new Rect(dotX, dotY, DotSize * scale, DotSize * scale), Texture2D.whiteTexture);
            }

            GUI.color = previousColor;
        }

        private void ShowNextTrack()
        {
            _trackIndex = Mathf.Min(_trackIndex + 1, Tracks.Length - 1);
        }

        private void ShowPreviousTrack()
        {
            _trackIndex = Mathf.Max(_trackIndex - 1, 0);
        }

        private void Confirm(bool useTechnicalCircuit2)
        {
            _confirmed = true;
            _onConfirm?.Invoke(useTechnicalCircuit2);
        }
    }
}
