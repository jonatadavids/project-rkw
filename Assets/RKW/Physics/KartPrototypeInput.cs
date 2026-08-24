using UnityEngine;
using UnityEngine.InputSystem;

namespace RKW.Physics
{
    /// <summary>
    /// Touch + keyboard input for the kart prototype.
    /// M2-T14: Implements virtual joystick (left side, analog steering)
    /// and analog pedals (right side, side-by-side: brake left half,
    /// throttle right half — founder playtest feedback, 2026-08-19, asked
    /// for pedals "um do lado do outro não um acima do outro" plus a
    /// visible steering wheel that turns with input).
    /// Throttle respects the 150ms ramp in KartDynamics (not here — raw input is analog).
    ///
    /// Round 27 (2026-08-24): founder request "Aplicar o novo design
    /// enviado do volante e dos pedais... nos botões de UI do celular" —
    /// the wheel/pedal HUD icons now prefer baked PNG art (rendered from
    /// the founder's own modeled 3D steering wheel and pedal box,
    /// Assets/RKW/Physics/Resources/KartPhysics/Textures/*.png) loaded via
    /// Resources.Load, and only fall back to the original procedural
    /// silhouettes (see ProceduralUITextures) if that asset is ever
    /// missing — preserving the "nothing that can go missing on a fresh
    /// checkout" guarantee those were built for.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(KartDynamics))]
    public sealed class KartPrototypeInput : MonoBehaviour
    {
        private KartDynamics _dynamics;

        // Touch state
        private int _steeringFingerId = -1;
        private Vector2 _steeringOrigin;
        private float _steeringValue;
        private float _throttleValue;
        private float _brakeValue;
        private bool _inputEnabled = true;

        // Round 31 (2026-08-24): smoothed copies of _brakeValue/_throttleValue
        // used ONLY for the pedal-press visual (icon squash/tilt + 3D pedal
        // prop, see DrawPedal below). The physics-facing values are now
        // binary (press & hold, see HandlePedalTouch), which would make the
        // icon/pedal SNAP instantly; animating a smoothed copy instead gives
        // the "pedal being pushed down" feel the founder asked for without
        // touching the actual input the kart responds to.
        private float _brakeVisualIntensity;
        private float _throttleVisualIntensity;
        private const float PedalVisualSpeed = 12f; // units/sec, ~80ms to reach full press

        // Layout
        private const float JoystickDeadzone = 15f; // pixels
        private const float JoystickMaxRadius = 120f; // pixels
        private const float SteeringZoneWidthRatio = 0.40f; // left 40% of screen
        private const float MaxWheelVisualRotationDegrees = 90f;

        // Round 31 (2026-08-24) founder request: "Aumentar significativamente
        // o tamanho do volante e dos pedais". Both the wheel-size ratio
        // (below, in DrawSteeringWheel) and these pedal-slot numbers went up
        // noticeably from round 30's values (0.30 / 210px).
        private const float PedalSlotWidthRatio = 0.40f;
        private const float PedalSlotMaxWidthPixels = 260f;

        // Round 27: brake/throttle colors used to be applied by tinting a
        // SHARED white pedal texture via GUI.color. Baked icons need their
        // true colors preserved (alpha-only tint), so brake/throttle each
        // get their own texture now.
        //
        // Round 28 (2026-08-24) founder feedback: "pode tirar aquele verde
        // e vermelho a animação do pedal já ajuda" — was red for brake /
        // green for throttle (both the procedural-fallback icon bake and
        // the intensity bar below each icon); replaced with one neutral
        // gray used for both, since the founder felt the pedal's own
        // motion/fade already communicates which is which without
        // color-coding.
        private static readonly Color PedalNeutralColor = new(0.82f, 0.82f, 0.85f);

        // HUD textures — each loaded once (baked art preferred, procedural
        // fallback if missing) and disposed in OnDestroy, but ONLY when
        // procedural: a Resources.Load'd Texture2D is a shared project
        // asset, and calling Destroy() on it can corrupt that asset
        // reference for the rest of the session, not just this instance.
        private Texture2D _wheelTexture;
        private bool _wheelTextureIsBaked;
        private Texture2D _brakeTexture;
        private bool _brakeTextureIsBaked;
        private Texture2D _throttleTexture;
        private bool _throttleTextureIsBaked;

        // Round 28: soft dark glow drawn behind the wheel/pedal icons so
        // they read clearly against the track — always procedural (there
        // is no "baked" version of a blur), so always safe to Destroy().
        private Texture2D _hudBackingTexture;

        private void Awake()
        {
            _dynamics = GetComponent<KartDynamics>();
        }

        /// <summary>
        /// Held false during the race-start countdown (see
        /// <see cref="RaceStartController"/>) so the kart cannot move before
        /// "VAI!" — touches/keyboard are still read so the HUD stays live.
        /// </summary>
        public void SetInputEnabled(bool inputEnabled)
        {
            _inputEnabled = inputEnabled;
        }

        /// <summary>
        /// Round 32 (2026-08-24): read-only mirror of the field above, so
        /// other components (see KartCategoryToggleButton) can tell
        /// whether the race has actually started ("VAI!" fired) without
        /// needing their own separate wiring into RaceStartController.
        /// </summary>
        public bool InputEnabled => _inputEnabled;

        private void Update()
        {
            _steeringValue = 0f;
            _throttleValue = 0f;
            _brakeValue = 0f;

            ReadKeyboard();
            ReadTouches();

            _brakeVisualIntensity = Mathf.MoveTowards(_brakeVisualIntensity, _brakeValue, PedalVisualSpeed * Time.deltaTime);
            _throttleVisualIntensity = Mathf.MoveTowards(_throttleVisualIntensity, _throttleValue, PedalVisualSpeed * Time.deltaTime);

            if (_inputEnabled)
            {
                _dynamics.SetInput(_steeringValue, _throttleValue, _brakeValue);
            }
            else
            {
                _dynamics.SetInput(0f, 0f, 0f);
            }
        }

        private void ReadKeyboard()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            {
                _steeringValue -= 1f;
            }

            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            {
                _steeringValue += 1f;
            }

            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
            {
                _throttleValue = 1f;
            }

            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed || keyboard.spaceKey.isPressed)
            {
                _brakeValue = 1f;
            }
        }

        private void ReadTouches()
        {
            var touchscreen = Touchscreen.current;
            if (touchscreen == null)
            {
                return;
            }

            var safe = Screen.safeArea;
            var scale = Mathf.Max(1f, Screen.height / 720f);
            var steeringZoneRight = safe.xMin + safe.width * SteeringZoneWidthRatio;

            // Round 31 (2026-08-24) founder bug report: "a área de clique...
            // ficou no meio da tela" — the pedal ICON positions moved to hug
            // the right screen edge in round 30, but the touch-detection
            // boundary here was never updated to match, so the tappable area
            // and the visible icons drifted apart. Fixed by computing the
            // zones ONCE (ComputePedalZones) and using that exact same Rect
            // both here and in OnGUI's drawing code below — they can no
            // longer disagree because there's only one calculation left.
            ComputePedalZones(safe, scale, out var brakeZone, out var throttleZone);

            foreach (var touch in touchscreen.touches)
            {
                if (!touch.press.isPressed)
                {
                    // If this was the steering finger, release it
                    if (touch.touchId.ReadValue() == _steeringFingerId)
                    {
                        _steeringFingerId = -1;
                    }
                    continue;
                }

                var position = touch.position.ReadValue();
                var touchId = touch.touchId.ReadValue();

                if (position.x < steeringZoneRight)
                {
                    // Left side: virtual joystick (analog steering)
                    HandleSteeringTouch(touchId, position, touch.phase.ReadValue());
                }
                else
                {
                    // Input System touch positions use a bottom-left origin
                    // (Y grows upward). The pedal zones below are computed in
                    // GUI space (top-left origin, Y grows downward — same
                    // convention OnGUI uses via "Screen.height - safe.yMax").
                    // Flipping Y here is what lets touch detection and
                    // on-screen drawing share the exact same Rects.
                    var guiPosition = new Vector2(position.x, Screen.height - position.y);
                    HandlePedalTouch(guiPosition, brakeZone, throttleZone);
                }
            }
        }

        /// <summary>
        /// Computes the brake/throttle pedal zones ONCE, in GUI space, so
        /// touch hit-testing (ReadTouches/HandlePedalTouch) and on-screen
        /// drawing (OnGUI) always agree on exactly where the pedals are —
        /// see the round 31 comment in ReadTouches above for why this method
        /// exists (it replaces two independent computations that had drifted
        /// out of sync).
        /// </summary>
        private static void ComputePedalZones(Rect safe, float scale, out Rect brakeZone, out Rect throttleZone)
        {
            var rightX = safe.xMin + safe.width * SteeringZoneWidthRatio;
            var rightW = safe.width * (1f - SteeringZoneWidthRatio);
            var pedalsY = Screen.height - safe.yMax;

            var slotWidth = Mathf.Min(rightW * PedalSlotWidthRatio, PedalSlotMaxWidthPixels * scale);
            var gap = 8f * scale;
            throttleZone = new Rect(rightX + rightW - slotWidth, pedalsY, slotWidth, safe.height);
            brakeZone = new Rect(throttleZone.x - gap - slotWidth, pedalsY, slotWidth, safe.height);
        }

        private void HandleSteeringTouch(int touchId, Vector2 position,
            UnityEngine.InputSystem.TouchPhase phase)
        {
            if (phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                _steeringFingerId = touchId;
                _steeringOrigin = position;
            }

            if (touchId != _steeringFingerId && _steeringFingerId != -1)
            {
                return; // different finger, ignore
            }

            if (_steeringFingerId == -1)
            {
                _steeringFingerId = touchId;
                _steeringOrigin = position;
            }

            var delta = position.x - _steeringOrigin.x;
            var absDelta = Mathf.Abs(delta);

            if (absDelta < JoystickDeadzone)
            {
                _steeringValue = 0f;
            }
            else
            {
                var effectiveDelta = absDelta - JoystickDeadzone;
                var maxEffective = JoystickMaxRadius - JoystickDeadzone;
                _steeringValue = Mathf.Clamp(
                    Mathf.Sign(delta) * (effectiveDelta / maxEffective),
                    -1f, 1f);
            }
        }

        /// <summary>
        /// Round 31 (2026-08-24) founder request: replace the old
        /// "arrastar para cima" analog mechanic (how far up the screen the
        /// finger was determined how hard the pedal was pressed) with direct
        /// press &amp; hold — full throttle/brake the instant a finger is down
        /// inside the pedal's own zone, back to zero the instant it lifts.
        /// A mobile pedal is expected to behave like a button, and the old
        /// Y-position-based intensity was also part of what made the exact
        /// touch position hard to communicate to a first-time player (round
        /// 28 feedback). <paramref name="guiPosition"/> is already in GUI
        /// space — see ReadTouches for the coordinate flip.
        /// </summary>
        private void HandlePedalTouch(Vector2 guiPosition, Rect brakeZone, Rect throttleZone)
        {
            if (brakeZone.Contains(guiPosition))
            {
                _brakeValue = 1f;
            }
            else if (throttleZone.Contains(guiPosition))
            {
                _throttleValue = 1f;
            }
            // A touch on the right side but outside both zones (e.g. the gap
            // between them) intentionally does nothing, rather than falling
            // back to the old "whichever half of the screen" logic — with
            // press & hold there is no reason to guess.
        }

        private void OnGUI()
        {
            EnsureHudTexturesLoaded();

            var safe = Screen.safeArea;
            var scale = Mathf.Max(1f, Screen.height / 720f);

            // Steering zone
            // Founder playtest feedback, 2026-08-20: "tem cores azul vermelho
            // e verde em cada touch... acho que é coisa atoa" — the
            // full-height translucent zone backgrounds (blue here, red/green
            // on the pedals below) read as unwanted color noise across the
            // whole screen rather than a subtle touch-area hint, so they are
            // gone; only the functional wheel/pedal graphics remain.
            var steerRect = new Rect(safe.xMin, Screen.height - safe.yMax,
                safe.width * SteeringZoneWidthRatio, safe.height);
            DrawSteeringWheel(steerRect, scale);

            // Round 31 (2026-08-24): the drawn pedal rects now come from the
            // SAME ComputePedalZones call that ReadTouches uses for touch
            // hit-testing (see that method's doc comment) — they can no
            // longer drift apart the way they did in round 30.
            ComputePedalZones(safe, scale, out var brakeZone, out var throttleZone);

            DrawPedal(brakeZone, _brakeVisualIntensity, _brakeTexture, PedalNeutralColor, "FREIO", scale);
            DrawPedal(throttleZone, _throttleVisualIntensity, _throttleTexture, PedalNeutralColor, "ACELERADOR", scale);

            // Speed HUD
            var hudStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(22f * scale),
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            var motionLabel = _dynamics.SignedForwardSpeedKph < -0.5f ? "RÉ " : string.Empty;
            // Founder playtest feedback, 2026-08-20 (round 8): "não consegui
            // ver o vácuo funcionando" — now that KartDynamics actually
            // applies the drafting drag reduction, show it here too so it's
            // visibly confirmable, not just felt as a slightly higher top
            // speed.
            var slipstreamLabel = _dynamics.SlipstreamDragReduction > 0.001f
                ? $"  •  VÁCUO {_dynamics.SlipstreamDragReduction * 100f:0}%"
                : string.Empty;
            GUI.Label(new Rect(safe.x + 20f, Screen.height - safe.yMax + 14f, 460f * scale, 46f * scale),
                $"{motionLabel}{_dynamics.SpeedKph:0} km/h  •  grip {_dynamics.GripRatio:0.00}{slipstreamLabel}", hudStyle);
        }

        /// <summary>
        /// Round 27: lazily loads the baked steering-wheel/brake/throttle
        /// icon PNGs (Resources/KartPhysics/Textures/*.png), falling back
        /// to the original procedural silhouettes if a given asset is
        /// absent. Runs once per texture (each field stays non-null after
        /// its first successful load or fallback).
        /// </summary>
        private void EnsureHudTexturesLoaded()
        {
            if (_hudBackingTexture == null)
            {
                _hudBackingTexture = ProceduralUITextures.CreateHudBackingTexture(128);
            }

            if (_wheelTexture == null)
            {
                _wheelTexture = Resources.Load<Texture2D>("KartPhysics/Textures/SteeringWheelIcon");
                _wheelTextureIsBaked = _wheelTexture != null;
                if (_wheelTexture == null)
                {
                    _wheelTexture = ProceduralUITextures.CreateSteeringWheelTexture(160);
                }
            }

            if (_brakeTexture == null)
            {
                _brakeTexture = Resources.Load<Texture2D>("KartPhysics/Textures/BrakePedalIcon");
                _brakeTextureIsBaked = _brakeTexture != null;
                if (_brakeTexture == null)
                {
                    _brakeTexture = ProceduralUITextures.CreatePedalTexture(96, 128, PedalNeutralColor);
                }
            }

            if (_throttleTexture == null)
            {
                _throttleTexture = Resources.Load<Texture2D>("KartPhysics/Textures/ThrottlePedalIcon");
                _throttleTextureIsBaked = _throttleTexture != null;
                if (_throttleTexture == null)
                {
                    _throttleTexture = ProceduralUITextures.CreatePedalTexture(96, 128, PedalNeutralColor);
                }
            }
        }

        /// <summary>
        /// Round 28 (2026-08-24) founder feedback (relayed from a
        /// first-time player): "os pedais e o volante continuam no ponto
        /// mais alto... seria legal ele aparecer mais evidente, bem mais
        /// evidente talvez no canto inferior da tela". Both this and
        /// DrawPedal below now anchor to the BOTTOM of their zone instead
        /// of ~16-40% down from the top — where a thumb naturally rests
        /// holding the phone in landscape — with a soft dark glow
        /// (DrawHudBacking) behind each icon so it doesn't blend into a
        /// busy 3D track background.
        /// </summary>
        private void DrawSteeringWheel(Rect zoneRect, float scale)
        {
            // Founder playtest feedback, 2026-08-19: "o volante poderia ser
            // bem menor do que esta" — was 0.55 (over half the steering
            // zone), which crowded the touch area it sits on top of.
            // Round 28 (2026-08-24): "o volante ficou ok poderia ser um
            // pouco maior tbm" — nudged back up a bit (0.30 -> 0.36).
            // Round 31 (2026-08-24): "Aumentar significativamente o tamanho
            // do volante" — 0.36 -> 0.48.
            var size = Mathf.Min(zoneRect.width, zoneRect.height) * 0.48f;
            var bottomMargin = 34f * scale;
            var pivot = new Vector2(zoneRect.x + zoneRect.width * 0.5f, zoneRect.yMax - bottomMargin - size * 0.5f);
            var wheelRect = new Rect(pivot.x - size * 0.5f, pivot.y - size * 0.5f, size, size);

            DrawHudBacking(pivot, size * 1.3f, size * 1.3f);

            var rotation = KartInputLayoutMath.CalculateSteeringWheelRotationDegrees(
                _steeringValue, MaxWheelVisualRotationDegrees);

            var previousMatrix = GUI.matrix;
            var previousColor = GUI.color;
            GUIUtility.RotateAroundPivot(rotation, pivot);
            GUI.color = new Color(1f, 1f, 1f, 0.95f);
            GUI.DrawTexture(wheelRect, _wheelTexture);
            GUI.color = previousColor;
            GUI.matrix = previousMatrix;

            var labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.LowerCenter,
                fontSize = Mathf.RoundToInt(16f * scale),
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            GUI.Label(new Rect(zoneRect.x, wheelRect.yMin - 30f * scale, zoneRect.width, 26f * scale),
                $"DIREÇÃO {_steeringValue:+0.00;-0.00}", labelStyle);
        }

        private void DrawPedal(Rect zoneRect, float intensity, Texture2D icon, Color color, string label, float scale)
        {
            var previousColor = GUI.color;

            // Round 27: tint is alpha-only now (was color.r/g/b * intensity
            // mix). The baked icons are full-color renders of the real
            // pedal box and must not be hue-multiplied, or the true colors
            // get muddied.
            // Round 28 (2026-08-24) founder feedback: "achei eles bem
            // pequenos" — the 140px cap was conservative on wide/high-res
            // phone screens; both the zone-relative ratio and the cap went
            // up (0.55 -> 0.68, 140 -> 185).
            // Round 31 (2026-08-24): "Aumentar significativamente o tamanho
            // dos pedais" — 0.68 -> 0.84, 185 -> 240. Icon anchors to the
            // bottom of the zone (thumb-rest position, see DrawSteeringWheel
            // above).
            var iconWidth = Mathf.Min(zoneRect.width * 0.84f, 240f * scale);
            var iconHeight = iconWidth * 1.35f;
            var bottomMargin = 34f * scale;
            var baseRect = new Rect(
                zoneRect.x + (zoneRect.width - iconWidth) * 0.5f,
                zoneRect.yMax - bottomMargin - iconHeight,
                iconWidth, iconHeight);

            DrawHudBacking(baseRect.center, iconWidth * 1.4f, iconHeight * 1.15f);

            // Round 31 (2026-08-24) founder request: "Adicionar feedback
            // visual/animação do pedal inclinando/pressionando para baixo...
            // no botão da tela". GUI space has no real depth, so the "press"
            // is approximated with three cues layered on top of each other:
            // the icon sinks down a little (pressDepth), gets slightly
            // shorter (squash, as if compressing into the floor), and tilts
            // back a few degrees around its own base (hinge-like rotation) —
            // driven by the smoothed _brakeVisualIntensity/
            // _throttleVisualIntensity from Update(), not the raw binary
            // press value, so it animates instead of snapping.
            var pressDepth = 10f * scale * intensity;
            var squash = Mathf.Lerp(1f, 0.90f, intensity);
            var pressedHeight = iconHeight * squash;
            var iconRect = new Rect(
                baseRect.x,
                baseRect.yMin + pressDepth + (iconHeight - pressedHeight),
                iconWidth,
                pressedHeight);

            var previousMatrix = GUI.matrix;
            var tiltPivot = new Vector2(iconRect.center.x, iconRect.yMax);
            GUIUtility.RotateAroundPivot(Mathf.Lerp(0f, 7f, intensity), tiltPivot);

            GUI.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.55f, 1f, intensity));
            GUI.DrawTexture(iconRect, icon);
            GUI.color = previousColor;
            GUI.matrix = previousMatrix;

            var labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.LowerCenter,
                fontSize = Mathf.RoundToInt(16f * scale),
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            GUI.Label(new Rect(zoneRect.x, baseRect.yMin - 30f * scale, zoneRect.width, 26f * scale),
                label, labelStyle);
        }

        /// <summary>Soft dark radial glow centered on <paramref name="center"/> — see ProceduralUITextures.CreateHudBackingTexture.</summary>
        private void DrawHudBacking(Vector2 center, float width, float height)
        {
            if (_hudBackingTexture == null)
            {
                return;
            }

            var rect = new Rect(center.x - width * 0.5f, center.y - height * 0.5f, width, height);
            GUI.DrawTexture(rect, _hudBackingTexture);
        }

        private void OnDestroy()
        {
            // Round 27: only Destroy() textures we created procedurally.
            // Resources.Load'd textures are shared project assets — Destroy()
            // on one of those can corrupt the asset for the rest of the
            // session, not just for this kart instance.
            if (_wheelTexture != null && !_wheelTextureIsBaked)
            {
                Destroy(_wheelTexture);
            }

            if (_brakeTexture != null && !_brakeTextureIsBaked)
            {
                Destroy(_brakeTexture);
            }

            if (_throttleTexture != null && !_throttleTextureIsBaked)
            {
                Destroy(_throttleTexture);
            }

            if (_hudBackingTexture != null)
            {
                Destroy(_hudBackingTexture);
            }
        }
    }
}
