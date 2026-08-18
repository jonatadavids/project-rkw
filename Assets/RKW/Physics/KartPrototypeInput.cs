using UnityEngine;
using UnityEngine.InputSystem;

namespace RKW.Physics
{
    /// <summary>
    /// Touch + keyboard input for the kart prototype.
    /// M2-T14: Implements virtual joystick (left side, analog steering)
    /// and analog pedals (right side: throttle top-half, brake bottom-half).
    /// Throttle respects the 150ms ramp in KartDynamics (not here — raw input is analog).
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

        // Layout
        private const float JoystickDeadzone = 15f; // pixels
        private const float JoystickMaxRadius = 120f; // pixels
        private const float SteeringZoneWidthRatio = 0.40f; // left 40% of screen
        private const float ThrottleZoneHeightRatio = 0.55f; // top 55% of right side

        private void Awake()
        {
            _dynamics = GetComponent<KartDynamics>();
        }

        private void Update()
        {
            _steeringValue = 0f;
            _throttleValue = 0f;
            _brakeValue = 0f;

            ReadKeyboard();
            ReadTouches();

            _dynamics.SetInput(_steeringValue, _throttleValue, _brakeValue);
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
            var steeringZoneRight = safe.xMin + safe.width * SteeringZoneWidthRatio;

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
                    // Right side: pedals
                    HandlePedalTouch(position, safe);
                }
            }
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

        private void HandlePedalTouch(Vector2 position, Rect safe)
        {
            var rightZoneX = safe.xMin + safe.width * SteeringZoneWidthRatio;
            var rightZoneWidth = safe.width * (1f - SteeringZoneWidthRatio);
            var relativeY = (position.y - safe.yMin) / safe.height;

            if (relativeY > (1f - ThrottleZoneHeightRatio))
            {
                // Top portion: throttle (intensity based on how high the touch is)
                var throttleRelative = (relativeY - (1f - ThrottleZoneHeightRatio)) / ThrottleZoneHeightRatio;
                _throttleValue = Mathf.Clamp01(throttleRelative * 1.5f); // slight amplification
            }
            else
            {
                // Bottom portion: brake
                var brakeRelative = 1f - (relativeY / (1f - ThrottleZoneHeightRatio));
                _brakeValue = Mathf.Clamp01(brakeRelative * 1.5f);
            }
        }

        private void OnGUI()
        {
            var safe = Screen.safeArea;
            var scale = Mathf.Max(1f, Screen.height / 720f);
            var style = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(18f * scale),
                normal = { textColor = Color.white }
            };

            // Draw steering zone
            var steerRect = new Rect(safe.xMin, Screen.height - safe.yMax,
                safe.width * SteeringZoneWidthRatio, safe.height);
            DrawZone(steerRect, $"DIREÇÃO\n{_steeringValue:+0.00;-0.00}", style,
                new Color(0.2f, 0.4f, 0.8f, 0.15f));

            // Draw throttle zone
            var rightX = safe.xMin + safe.width * SteeringZoneWidthRatio;
            var rightW = safe.width * (1f - SteeringZoneWidthRatio);
            var throttleRect = new Rect(rightX, Screen.height - safe.yMax,
                rightW, safe.height * ThrottleZoneHeightRatio);
            DrawZone(throttleRect, $"ACELERAR\n{_throttleValue:0.00}", style,
                new Color(0.2f, 0.8f, 0.2f, 0.15f));

            // Draw brake zone
            var brakeRect = new Rect(rightX, Screen.height - safe.yMax + safe.height * ThrottleZoneHeightRatio,
                rightW, safe.height * (1f - ThrottleZoneHeightRatio));
            DrawZone(brakeRect, $"FREAR\n{_brakeValue:0.00}", style,
                new Color(0.8f, 0.2f, 0.2f, 0.15f));

            // Speed HUD
            var hudStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(22f * scale),
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            var motionLabel = _dynamics.SignedForwardSpeedKph < -0.5f ? "RÉ " : string.Empty;
            GUI.Label(new Rect(safe.x + 20f, Screen.height - safe.yMax + 14f, 420f * scale, 46f * scale),
                $"{motionLabel}{_dynamics.SpeedKph:0} km/h  •  grip {_dynamics.GripRatio:0.00}", hudStyle);
        }

        private static void DrawZone(Rect guiRect, string label, GUIStyle style, Color color)
        {
            var previous = GUI.color;
            GUI.color = color;
            GUI.Box(guiRect, label, style);
            GUI.color = previous;
        }
    }
}
